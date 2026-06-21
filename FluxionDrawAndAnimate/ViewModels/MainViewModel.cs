using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Editing;
using FluxionDrawAndAnimate.Core.Projects;
using FluxionDrawAndAnimate.Core.Settings;
using FluxionDrawAndAnimate.Presentation;
using FluxionDrawAndAnimate.Presentation.Settings;
using FluxionDrawAndAnimate.Presentation.Shell;
using FluxionDrawAndAnimate.Presentation.StatusBar;
using FluxionDrawAndAnimate.Presentation.Studio;
using FluxionDrawAndAnimate.Services;
using FluxionDrawAndAnimate.Ui.Core;
using System.ComponentModel;

namespace FluxionDrawAndAnimate.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly TimelineEditingService _timelineEditing;
    private readonly IProjectFileService _projectFileService;
    private readonly IProjectFactory _projectFactory;
    private readonly IRecentProjectStore _recentProjectStore;
    private readonly IProjectThumbnailService _projectThumbnailService;
    private readonly ProjectCardFactory _projectCardFactory;
    private readonly SettingValueStore _settingValues;
    private readonly SettingEditorResolver _settingEditorResolver;
    private readonly IUserSettingsStore _userSettingsStore;
    private Services.IFavoritesStore _favoritesStore = new Services.NullFavoritesStore();
    private Services.IPlatformShell _platformShell = new Services.NullPlatformShell();

    public void SetFavoritesStore(Services.IFavoritesStore store)
    {
        _favoritesStore = store;
    }

    /// <summary>
    /// Injects the platform shell (immersive / fullscreen controller).
    /// Called from <c>App.OnFrameworkInitializationCompleted</c> after the VM
    /// is constructed; Android passes a real <c>AndroidPlatformShell</c>, every
    /// other platform leaves the default <c>NullPlatformShell</c> in place.
    /// Mirrors <see cref="SetFavoritesStore"/> so no constructor signature
    /// changes are needed.
    /// </summary>
    public void SetPlatformShell(Services.IPlatformShell shell)
    {
        _platformShell = shell ?? new Services.NullPlatformShell();
        // Apply the current layout mode immediately in case the shell was set
        // after the first layout evaluation.
        ApplyPlatformShellForCurrentLayout();
    }

    /// <summary>
    /// Pushes the current phone/studio layout to the platform shell so it can
    /// toggle immersive mode. No-op on desktop / browser / iOS where
    /// <see cref="Services.NullPlatformShell"/> ignores the call.
    /// </summary>
    private void ApplyPlatformShellForCurrentLayout()
    {
        // Immersive mode only makes sense on phone layouts where screen real
        // estate is at a premium. Studio/tablet keep the normal chrome.
        _platformShell.ApplyImmersive(IsPhoneLayout);
    }
    private readonly LayoutProfileFactory _layoutProfileFactory = new();
    private readonly Dictionary<string, SettingItemViewModel> _settingItems = new(StringComparer.OrdinalIgnoreCase);
    private bool _isApplyingSettings;
    private double _availableWidth;
    private bool _hasManualModeOverride;

    public UndoStack UndoStack { get; }
    public AppShellRegistry ShellRegistry { get; } = new();
    public StatusBarRegistry StatusBarRegistry { get; } = new();
    public HomePageRegistry HomePageRegistry { get; } = new();
    public StudioToolbarRegistry StudioToolbarRegistry { get; } = new();
    public StudioToolPanelRegistry StudioToolPanelRegistry { get; } = new();
    public StudioInspectorRegistry StudioInspectorRegistry { get; } = new();

    // ── Brush colour hex (synced both ways with BrushBaseColor) ──────────
    private string _brushColorHex = "#262C42";
    private bool _syncingHex;

    public string BrushColorHex
    {
        get => _brushColorHex;
        set
        {
            if (_syncingHex || _brushColorHex == value) return;
            _brushColorHex = value;
            OnPropertyChanged();
            TryApplyHexColor(value);
        }
    }

    private void TryApplyHexColor(string hex)
    {
        _syncingHex = true;
        try
        {
            var clean = hex.TrimStart('#');
            if (clean.Length == 6)
            {
                var r = Convert.ToByte(clean[..2], 16);
                var g = Convert.ToByte(clean[2..4], 16);
                var b = Convert.ToByte(clean[4..6], 16);
                BrushBaseColor = RgbaColor.FromRgb(r, g, b);
            }
        }
        catch { /* ignore invalid hex */ }
        finally { _syncingHex = false; }
    }

    // ── Brush opacity as percentage ──────────────────────────────────────
    public double BrushOpacityPercent
    {
        get => Math.Round(BrushOpacity * 100, 0);
        set { BrushOpacity = Math.Clamp(value / 100.0, 0.05, 1.0); OnPropertyChanged(); }
    }

    // ── Flow (independent from opacity) ──────────────────────────────────
    [ObservableProperty]
    private double _brushFlow = 1.0;

    public double BrushFlowPercent
    {
        get => Math.Round(BrushFlow * 100, 0);
        set { BrushFlow = Math.Clamp(value / 100.0, 0.05, 1.0); OnPropertyChanged(); }
    }

    partial void OnBrushFlowChanged(double value) => OnPropertyChanged(nameof(BrushFlowPercent));

    // ── Avalonia.Media.Color bridge for ColorView binding ─────────────────
    // ColorView speaks Avalonia.Media.Color; BrushBaseColor is our custom RgbaColor.
    private bool _settingMediaColor;
    public Avalonia.Media.Color BrushMediaColor
    {
        get => Avalonia.Media.Color.FromRgb(BrushBaseColor.R, BrushBaseColor.G, BrushBaseColor.B);
        set
        {
            if (_settingMediaColor) return;
            _settingMediaColor = true;
            BrushBaseColor = RgbaColor.FromRgb(value.R, value.G, value.B);
            _settingMediaColor = false;
            OnPropertyChanged();
        }
    }

    // ── Pressure curve points (Avalonia.Point list for Polyline binding) ──
    public IReadOnlyList<Avalonia.Point> PressureCurvePoints
    {
        get
        {
            var pts = ActiveBrushSettings?.Dynamics.SizeBinding?.Curve.Points;
            if (pts is null || pts.Count < 2)
                return [new(0, 38), new(98, 0)];
            return pts.Select(p => new Avalonia.Point(p.X * 98, (1.0 - p.Y) * 38)).ToList();
        }
    }

    // ── Active brush info (from preset settings) ─────────────────────────
    public int    ActiveBrushStabilizer => ActiveBrushSettings?.StabilizerSamples ?? 0;
    public string ActiveBrushShape      => ActiveBrushSettings?.Shape.ToString() ?? "Round";

    // ── Default colour swatches palette (with pre-built brushes for binding) ──
    public IReadOnlyList<Presentation.SwatchEntry> BrushSwatchBrushes { get; } =
        new Core.Drawing.RgbaColor[]
        {
            Core.Drawing.RgbaColor.FromRgb(220,  50,  50),
            Core.Drawing.RgbaColor.FromRgb(255, 120,  30),
            Core.Drawing.RgbaColor.FromRgb(255, 200,  50),
            Core.Drawing.RgbaColor.FromRgb(100, 200,  80),
            Core.Drawing.RgbaColor.FromRgb( 50, 150, 230),
            Core.Drawing.RgbaColor.FromRgb(120,  70, 200),
            Core.Drawing.RgbaColor.FromRgb(255, 255, 255),
            Core.Drawing.RgbaColor.FromRgb(180, 180, 180),
            Core.Drawing.RgbaColor.FromRgb( 80,  80,  80),
            Core.Drawing.RgbaColor.FromRgb(  0,   0,   0),
            Core.Drawing.RgbaColor.FromRgb(200, 140, 100),
            Core.Drawing.RgbaColor.FromRgb( 60,  40,  30),
        }.Select(c => new Presentation.SwatchEntry(c)).ToList();

    private IReadOnlyList<StudioToolPanelSection> _studioToolSections = [];
    public IReadOnlyList<StudioToolPanelSection> StudioToolSections
    {
        get => _studioToolSections;
        private set { _studioToolSections = value; OnPropertyChanged(); }
    }
    public AppearanceSettingsViewModel AppearanceSettings { get; } = new();
    public IReadOnlyList<SettingsCategoryDefinition> SettingsCategories { get; }
        = SettingsCategoryRegistrar.CreateDefault();

    // Convenience passthrough for AXAML bindings inside DataTemplates
    // (avoids complex ancestor-cast binding expressions).
    public bool ShowToolLabels => AppearanceSettings.ShowToolLabels;

    private void InitUndoStack()
    {
        UndoStack.StateChanged += NotifyUndoState;
        StatusBarRegistry.Items.CollectionChanged += (_, _) => RebuildLiveStatusBarItems();
        HomePageRegistry.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(HomePageRegistry.ProjectSearchQuery))
            {
                OnPropertyChanged(nameof(FilteredRecentProjects));
            }
        };

        // Appearance: re-apply the changed setting and notify derived VM props.
        AppearanceSettings.PropertyChanged += OnAppearanceSettingChanged;
    }

    private void OnAppearanceSettingChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Single-dispatch: always re-apply the full set; each apply is O(1).
        AppearanceApplier.ApplyAll(AppearanceSettings);

        // Notify any VM-level computed properties that depend on appearance.
        switch (e.PropertyName)
        {
            case nameof(AppearanceSettingsViewModel.ShowToolLabels):
                OnPropertyChanged(nameof(ShowToolLabels));
                break;

            // Theme change also requires the ProjectCreatorThemeMode to stay in sync.
            case nameof(AppearanceSettingsViewModel.AppTheme):
                ProjectCreatorThemeMode = AppearanceSettings.AppTheme == "Light"
                    ? ProjectCreatorThemeMode.Light
                    : ProjectCreatorThemeMode.Dark;
                break;
        }
    }

    [ObservableProperty]
    private DrawingProject _project = default!;

    [ObservableProperty]
    private bool _isSettingsVisible;

    [ObservableProperty]
    private bool _isNewProjectDialogVisible;

    [ObservableProperty]
    private bool _isRenameDialogVisible;

    [ObservableProperty]
    private string _pendingRenameText = "";

    private Presentation.ProjectCard? _renameTarget;

    public bool IsProjectDetailsVisible => SelectedRecentProject is not null;

    [ObservableProperty]
    private WorkspaceMode _workspaceMode = WorkspaceMode.Studio;

    [ObservableProperty]
    private ProjectCreatorThemeMode _projectCreatorThemeMode = ProjectCreatorThemeMode.Dark;

    [ObservableProperty]
    private int _selectedFrameIndex;

    [ObservableProperty]
    private int _selectedLayerIndex = 0;

    [ObservableProperty]
    private ToolPreset _activeTool = default!;

    [ObservableProperty]
    private bool _showOnionSkin = true;

    [ObservableProperty]
    private RgbaColor _brushBaseColor = RgbaColor.FromRgb(38, 48, 66);

    [ObservableProperty]
    private double _brushSizeSetting = 10;

    [ObservableProperty]
    private double _brushOpacity = 1;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private ProjectCard _selectedProjectTemplate = default!;

    [ObservableProperty]
    private ProjectCard? _selectedRecentProject;

    [ObservableProperty]
    private string _newProjectName = "Untitled";

    [ObservableProperty]
    private int _newProjectWidth = 1920;

    [ObservableProperty]
    private int _newProjectHeight = 1080;

    [ObservableProperty]
    private int _newProjectFrameCount = 48;

    [ObservableProperty]
    private int _newProjectFramesPerSecond = 24;

    [ObservableProperty]
    private PageDefinition? _activePage;

    // ── Settings dialog ──────────────────────────────────────────────────────

    [ObservableProperty]
    private string _selectedSettingsCategoryId = "appearance";

    partial void OnSelectedSettingsCategoryIdChanged(string value)
    {
        foreach (var cat in SettingsCategories)
        {
            cat.IsActive = cat.Id == value;
        }
        OnPropertyChanged(nameof(SelectedSettingsCategoryIndex));
    }

    /// <summary>
    /// Computed integer index into <see cref="SettingsCategories"/>.
    /// Used by the horizontal pill selector on phone layouts; the desktop
    /// sidebar uses <see cref="SelectSettingsCategoryCommand"/> with the
    /// string Id instead.
    /// </summary>
    public int SelectedSettingsCategoryIndex
    {
        get
        {
            for (var i = 0; i < SettingsCategories.Count; i++)
            {
                if (SettingsCategories[i].Id == SelectedSettingsCategoryId)
                    return i;
            }
            return 0;
        }
        set
        {
            if (value >= 0 && value < SettingsCategories.Count)
                SelectSettingsCategory(SettingsCategories[value].Id);
        }
    }

    [RelayCommand]
    private void SelectSettingsCategory(string id)
    {
        SelectedSettingsCategoryId = id;
    }

    [RelayCommand]
    private void ApplySettings()
    {
        AppearanceApplier.ApplyAll(AppearanceSettings);
        ProjectCreatorThemeMode = AppearanceSettings.AppTheme == "Light"
            ? ProjectCreatorThemeMode.Light
            : ProjectCreatorThemeMode.Dark;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        ApplySettings();
        IsSettingsVisible = false;
    }

    [RelayCommand]
    private void ResetCategorySettings()
    {
        if (SelectedSettingsCategoryId == "appearance")
        {
            AppearanceSettings.ResetToDefaults();
        }
    }

    [RelayCommand]
    private void ResetAppearanceCategory()
    {
        AppearanceSettings.ResetToDefaults();
    }

    // ── Home page sidebar navigation ────────────────────────────────────────

    [ObservableProperty]
    private string _activeSidebarItemId = "home";

    public IReadOnlyList<HomeNavItem> HomeNavItems { get; } =
    [
        new("home",      "mdi-home-outline",          "Home"),
        new("projects",  "mdi-folder-outline",         "Projects",  "BROWSE"),
        new("recent",    "mdi-clock-time-four-outline","Recent"),
        new("templates", "mdi-view-grid-outline",      "Templates"),
        new("learn",     "mdi-school-outline",         "Learn"),
        new("cloud",     "mdi-cloud-outline",          "Cloud",     "LIBRARY"),
        new("trash",     "mdi-delete-outline",         "Trash"),
    ];

    partial void OnActiveSidebarItemIdChanged(string value)
    {
        foreach (var item in HomeNavItems)
        {
            item.IsActive = item.Id == value;
        }
    }

    [RelayCommand]
    private void SetActiveSidebarItem(string id)
    {
        ActiveSidebarItemId = id;
    }

    // ── Studio canvas live info (updated by DrawingCanvasControl callback) ──
    // Not [ObservableProperty] — the ScaleChangedCallback sets _canvasZoom directly to avoid loops.
    private double _canvasZoom = 1.0;
    public double CanvasZoom => _canvasZoom;

    [ObservableProperty]
    private bool _snapEnabled = true;

    [ObservableProperty]
    private bool _showGrid;

    // Not ObservableProperty — updated at high frequency; refreshed via RefreshLiveStatusBarItems.
    private double _cursorDocumentX;
    private double _cursorDocumentY;
    private double _lastPressure = 1.0;
    private int _memoryUsagePercent;

    public double CursorDocumentX => _cursorDocumentX;
    public double CursorDocumentY => _cursorDocumentY;
    public double LastPressure => _lastPressure;
    public int MemoryUsagePercent => _memoryUsagePercent;

    public MainViewModel()
        : this(
            new DefaultProjectFactory(),
            new TimelineEditingService(),
            new ToolPaletteFactory(),
            new ProjectPresetFactory(),
            new ProjectCardFactory(),
            new NullProjectFileService(),
            new NullRecentProjectStore(),
            new NullProjectThumbnailService(),
            new AppSettingsRegistryFactory().CreateDefaultRegistry(),
            new SettingEditorResolver(),
            new NullUserSettingsStore())
    {
    }

    public MainViewModel(
        IProjectFactory projectFactory,
        TimelineEditingService timelineEditing,
        ToolPaletteFactory toolPaletteFactory,
        ProjectPresetFactory projectPresetFactory,
        ProjectCardFactory projectCardFactory,
        IProjectFileService projectFileService,
        IRecentProjectStore recentProjectStore,
        IProjectThumbnailService projectThumbnailService,
        SettingRegistry settingRegistry,
        SettingEditorResolver settingEditorResolver,
        IUserSettingsStore userSettingsStore)
    {
        _projectFactory = projectFactory;
        _timelineEditing = timelineEditing;
        _projectFileService = projectFileService;
        _recentProjectStore = recentProjectStore;
        _projectThumbnailService = projectThumbnailService;
        _projectCardFactory = projectCardFactory;
        _settingValues = new SettingValueStore(settingRegistry);
        _settingEditorResolver = settingEditorResolver;
        _userSettingsStore = userSettingsStore;
        UndoStack = new UndoStack();
        InitUndoStack();

        // Shell + status bar registration — runs after UndoStack so all commands
        // are available for the registrars to wire up.
        AppShellRegistrar.RegisterDefaults(ShellRegistry, this);
        StatusBarRegistrar.RegisterDefaults(StatusBarRegistry, this);
        HomePageRegistrar.RegisterDefaults(HomePageRegistry, this);
        StudioToolbarRegistrar.RegisterDefaults(StudioToolbarRegistry, this);
        StudioToolPanelRegistrar.RegisterDefaults(StudioToolPanelRegistry);
        StudioToolPanelRegistry.SectionsChanged += () => StudioToolSections = StudioToolPanelRegistry.Sections;
        StudioInspectorRegistrar.RegisterDefaults(StudioInspectorRegistry);
        StudioToolSections = StudioToolPanelRegistry.Sections;
        ActivePage = ShellRegistry.FindPage("home");
        RebuildLiveStatusBarItems();
        // Initialise sidebar + settings active states
        OnActiveSidebarItemIdChanged(_activeSidebarItemId);
        OnSelectedSettingsCategoryIdChanged(_selectedSettingsCategoryId);
        // Apply appearance defaults so dynamic resources are set before first render
        AppearanceApplier.ApplyAll(AppearanceSettings);

        Project = projectFactory.CreateNewProject();
        Tools = new ObservableCollection<ToolPreset>(toolPaletteFactory.CreateDefaultTools());
        ProjectTemplates = new ObservableCollection<ProjectCard>(
            projectPresetFactory.CreateDefaultPresets().Select(projectCardFactory.CreateTemplateCard));
        RecentProjects = new ObservableCollection<ProjectCard>();
        RecentProjects.CollectionChanged += (_, _) => OnPropertyChanged(nameof(FilteredRecentProjects));
        ActiveTool = Tools[0];
        SelectedProjectTemplate = ProjectTemplates[0];
        AvailableFrameRates = new ObservableCollection<int> { 12, 24, 30, 60 };
        SettingsGroups = new ObservableCollection<SettingsGroupViewModel>();
        BuildSettingsGroups();
        ApplyAllSettings();

        _ = LoadRecentProjectsAsync();
        _ = LoadUserSettingsAsync();
        _ = LoadFavoritesAsync();
    }

    public ObservableCollection<ToolPreset> Tools { get; }
    public ObservableCollection<ProjectCard> ProjectTemplates { get; }
    public ObservableCollection<ProjectCard> RecentProjects { get; }
    public ObservableCollection<int> AvailableFrameRates { get; }
    public ObservableCollection<SettingsGroupViewModel> SettingsGroups { get; }

    // Derived from ActivePage so the shell title bar and legacy bindings stay in sync.
    public bool IsProjectHomeVisible => ActivePage?.Id == "home" || ActivePage is null;
    public bool IsEditorVisible => ActivePage?.Id == "studio";

    /// <summary>True when the viewport is phone-sized (< 600 px), regardless of manual mode override.</summary>
    public bool IsPhoneLayout => ActiveProfileKind == ResponsiveProfileKind.Phone;

    /// <summary>Recent projects filtered by the home page search query.</summary>
    public IReadOnlyList<ProjectCard> FilteredRecentProjects
    {
        get
        {
            var q = HomePageRegistry.ProjectSearchQuery;
            if (string.IsNullOrWhiteSpace(q))
            {
                return RecentProjects;
            }

            return RecentProjects
                .Where(p => p.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    // Live status bar items — Observable wrappers that hold the evaluated text/progress.
    // Rebuilt when the active page changes; refreshed when observed VM properties change.
    private readonly System.Collections.ObjectModel.ObservableCollection<LiveStatusBarItem>
        _statusBarLeftLive = [];
    private readonly System.Collections.ObjectModel.ObservableCollection<LiveStatusBarItem>
        _statusBarRightLive = [];

    public System.Collections.ObjectModel.ObservableCollection<LiveStatusBarItem>
        StatusBarLeftLiveItems => _statusBarLeftLive;
    public System.Collections.ObjectModel.ObservableCollection<LiveStatusBarItem>
        StatusBarRightLiveItems => _statusBarRightLive;

    /// <summary>Callback bound to DrawingCanvasControl; called on every pointer move.</summary>
    private Action<double, double, double>? _cursorInfoCallback;
    public Action<double, double, double> CursorInfoCallback =>
        _cursorInfoCallback ??= UpdateCursorInfo;

    public void UpdateCursorInfo(double x, double y, double pressure)
    {
        _cursorDocumentX = x;
        _cursorDocumentY = y;
        _lastPressure = pressure;
        RefreshLiveStatusBarItems(nameof(CursorDocumentX), nameof(LastPressure));
    }

    private void RebuildLiveStatusBarItems()
    {
        var pageId = ActivePage?.Id;
        _statusBarLeftLive.Clear();
        foreach (var item in StatusBarRegistry.GetForPage(pageId, StatusBarSection.Left))
            _statusBarLeftLive.Add(new LiveStatusBarItem(item, this));

        _statusBarRightLive.Clear();
        foreach (var item in StatusBarRegistry.GetForPage(pageId, StatusBarSection.Right))
            _statusBarRightLive.Add(new LiveStatusBarItem(item, this));
    }

    private void RefreshLiveStatusBarItems(params string[] changedProperties)
    {
        var changed = new HashSet<string>(changedProperties);
        foreach (var item in _statusBarLeftLive)
        {
            if (item.ObservedProperties?.Any(changed.Contains) == true) item.Refresh(this);
        }
        foreach (var item in _statusBarRightLive)
        {
            if (item.ObservedProperties?.Any(changed.Contains) == true) item.Refresh(this);
        }
    }
    public bool CanUndo => UndoStack.CanUndo;
    public bool CanRedo => UndoStack.CanRedo;
    public string ModeButtonText => WorkspaceMode == WorkspaceMode.Studio ? "Telefon UI" : "Studio UI";
    public bool IsStudioMode => WorkspaceMode == WorkspaceMode.Studio;
    public bool IsPhoneMode => WorkspaceMode == WorkspaceMode.Phone;
    public bool HasRecentProjects => RecentProjects.Count > 0;
    public bool IsRecentProjectsEmpty => !HasRecentProjects;

    // Effective responsive tier. When the user has not overridden the mode it is
    // derived from the available width (Phone / Tablet / Studio); a manual toggle
    // pins it to Phone or Studio until the user resets to automatic.
    public ResponsiveProfileKind ActiveProfileKind => _hasManualModeOverride
        ? (WorkspaceMode == WorkspaceMode.Phone ? ResponsiveProfileKind.Phone : ResponsiveProfileKind.Studio)
        : ResponsiveBreakpoints.FromWidth(_availableWidth);

    public LayoutProfile WorkspaceLayoutProfile => _layoutProfileFactory.For(ActiveProfileKind);
    public bool IsLayoutAutomatic => !_hasManualModeOverride;

    // Project-home (start screen) responsive arrangement, driven by the same
    // width-based tiers as the editor.
    public bool IsHomePhoneLayout => ActiveProfileKind == ResponsiveProfileKind.Phone;
    public bool IsHomeWideLayout => ActiveProfileKind != ResponsiveProfileKind.Phone;
    public bool IsHomeTabletLayout => ActiveProfileKind == ResponsiveProfileKind.Tablet;
    public bool IsHomeStudioLayout => ActiveProfileKind == ResponsiveProfileKind.Studio;
    public bool ShowSidePanel => WorkspaceLayoutProfile.ShowsSidePanels;
    public double TimelineHeight => WorkspaceLayoutProfile.TimelineHeight;
    public double PropertiesPanelWidth => WorkspaceLayoutProfile.SidePanelWidth;
    public RgbaColor BrushColor => RgbaColor.FromArgb(
        (byte)Math.Clamp(Math.Round(BrushOpacity * 255), 0, 255),
        BrushBaseColor.R,
        BrushBaseColor.G,
        BrushBaseColor.B);
    public double BrushSize => BrushSizeSetting;
    // ActiveToolKind is now set independently by the tool panel;
    // it no longer derives from the active brush preset.
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private FluxionDrawAndAnimate.Core.Drawing.ToolKind _activeToolKind
        = FluxionDrawAndAnimate.Core.Drawing.ToolKind.Pencil;

    /// <summary>Human-readable name from the registered ToolDefinition (falls back to brush preset name).</summary>
    public string ActiveToolName =>
        StudioToolPanelRegistry.Items.FirstOrDefault(t => t.ToolKind == ActiveToolKind)?.Name
        ?? ActiveTool?.Name
        ?? "Brush";

    /// <summary>B/5: the active preset's brush settings, bound to the canvas.</summary>
    public BrushSettings? ActiveBrushSettings => ActiveTool?.BrushPreset?.Settings;
    public int DisplayFrame => SelectedFrameIndex + 1;
    public int TotalFrames => Project.FrameCount;

    partial void OnWorkspaceModeChanged(WorkspaceMode value)
    {
        OnPropertyChanged(nameof(ModeButtonText));
        OnPropertyChanged(nameof(IsStudioMode));
        OnPropertyChanged(nameof(IsPhoneMode));
        NotifyLayoutChanged();
        ApplyPlatformShellForCurrentLayout();
    }

    private void NotifyLayoutChanged()
    {
        OnPropertyChanged(nameof(ActiveProfileKind));
        OnPropertyChanged(nameof(WorkspaceLayoutProfile));
        OnPropertyChanged(nameof(ShowSidePanel));
        OnPropertyChanged(nameof(TimelineHeight));
        OnPropertyChanged(nameof(PropertiesPanelWidth));
        OnPropertyChanged(nameof(IsLayoutAutomatic));
        OnPropertyChanged(nameof(IsPhoneLayout));
        OnPropertyChanged(nameof(IsHomePhoneLayout));
        OnPropertyChanged(nameof(IsHomeWideLayout));
        // Width-driven (automatic) transitions between phone and non-phone
        // layouts also need to update immersive mode — not just the explicit
        // WorkspaceMode toggle handled in OnWorkspaceModeChanged.
        ApplyPlatformShellForCurrentLayout();
        OnPropertyChanged(nameof(IsHomeTabletLayout));
        OnPropertyChanged(nameof(IsHomeStudioLayout));
    }

    /// <summary>
    /// Called by the view whenever the available width changes. Drives the
    /// automatic Phone/Tablet/Studio selection unless the user pinned a mode.
    /// </summary>
    public void ApplyAvailableWidth(double width)
    {
        if (width <= 0)
        {
            return;
        }

        _availableWidth = width;

        if (!_hasManualModeOverride)
        {
            var kind = ResponsiveBreakpoints.FromWidth(width);
            WorkspaceMode = kind == ResponsiveProfileKind.Phone
                ? WorkspaceMode.Phone
                : WorkspaceMode.Studio;
        }

        NotifyLayoutChanged();
    }

    partial void OnProjectCreatorThemeModeChanged(ProjectCreatorThemeMode value)
    {
        UpdateSettingFromProperty(AppSettingKeys.CreatorTheme, value);
    }


    partial void OnActivePageChanged(PageDefinition? value)
    {
        OnPropertyChanged(nameof(IsProjectHomeVisible));
        OnPropertyChanged(nameof(IsEditorVisible));
        RebuildLiveStatusBarItems();
    }


    partial void OnSnapEnabledChanged(bool value) =>
        RefreshLiveStatusBarItems(nameof(SnapEnabled));

    partial void OnShowGridChanged(bool value) =>
        RefreshLiveStatusBarItems(nameof(ShowGrid));

    [RelayCommand]
    private void NavigateToPage(string pageId)
    {
        var page = ShellRegistry.FindPage(pageId);
        if (page is not null)
        {
            ActivePage = page;
        }
    }

    partial void OnActiveToolKindChanged(Core.Drawing.ToolKind value)
    {
        foreach (var tool in StudioToolPanelRegistry.Items)
            tool.IsActive = tool.ToolKind == value;
        OnPropertyChanged(nameof(ActiveToolName));
        OnPropertyChanged(nameof(PressureCurvePoints));
        RefreshLiveStatusBarItems(nameof(ActiveToolName));
    }

    partial void OnActiveToolChanged(ToolPreset value)
    {
        OnPropertyChanged(nameof(ActiveBrushSettings));
        OnPropertyChanged(nameof(ActiveBrushStabilizer));
        OnPropertyChanged(nameof(ActiveBrushShape));
    }

    partial void OnBrushBaseColorChanged(RgbaColor value)
    {
        OnPropertyChanged(nameof(BrushColor));
        OnPropertyChanged(nameof(BrushBaseColorBrush));
        if (!_syncingHex)
        {
            _brushColorHex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
            OnPropertyChanged(nameof(BrushColorHex));
        }
        if (!_settingMediaColor)
            OnPropertyChanged(nameof(BrushMediaColor));
    }

    partial void OnBrushOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(BrushColor));
        OnPropertyChanged(nameof(BrushOpacityPercent));
    }

    [RelayCommand]
    private void SelectSwatch(object? param)
    {
        if (param is Core.Drawing.RgbaColor color)
            BrushBaseColor = color;
    }

    partial void OnBrushSizeSettingChanged(double value)
    {
        OnPropertyChanged(nameof(BrushSize));
        RefreshLiveStatusBarItems(nameof(BrushSize));
    }


    [RelayCommand]
    private void ActivateDrawingTool(Core.Drawing.ToolKind kind) => ActiveToolKind = kind;

    // ── Canvas viewport ──────────────────────────────────────────────────

    /// <summary>Increment this to trigger FitToView on the bound canvas.</summary>
    [ObservableProperty]
    private int _fitToViewTrigger;

    [RelayCommand]
    private void FitCanvas() => FitToViewTrigger++;

    [RelayCommand]
    private void ResetCanvasView() => FitToViewTrigger++;   // FitToView covers reset too for now

    private Action<double>? _scaleChangedCallback;
    /// <summary>Canvas calls this when the user zooms; VM updates CanvasZoom for the status bar.</summary>
    public Action<double> ScaleChangedCallback =>
        _scaleChangedCallback ??= scale =>
        {
            _canvasZoom = scale;
            OnPropertyChanged(nameof(CanvasZoom));
            RefreshLiveStatusBarItems(nameof(CanvasZoom));
        };

    /// <summary>Foreground colour swatch for the tool panel bottom area.</summary>
    public Avalonia.Media.IBrush BrushBaseColorBrush =>
        new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.FromRgb(BrushBaseColor.R, BrushBaseColor.G, BrushBaseColor.B));

    [RelayCommand]
    private void ToggleOnionSkin() => ShowOnionSkin = !ShowOnionSkin;

    [RelayCommand]
    private void SetGridView() => HomePageRegistry.ProjectViewMode = HomeProjectViewMode.Grid;

    [RelayCommand]
    private void SetListView() => HomePageRegistry.ProjectViewMode = HomeProjectViewMode.List;

    [RelayCommand]
    private void ToggleSnap() => SnapEnabled = !SnapEnabled;

    [RelayCommand]
    private void ToggleGrid() => ShowGrid = !ShowGrid;

    partial void OnSelectedFrameIndexChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayFrame));
    }

    partial void OnProjectChanged(DrawingProject value)
    {
        OnPropertyChanged(nameof(TotalFrames));
        OnPropertyChanged(nameof(DisplayFrame));
        ApplySetting(AppSettingKeys.TileMemoryBudget, _settingValues.GetValue<int>(AppSettingKeys.TileMemoryBudget));
        UndoStack.Clear();
        NotifyUndoState();
        UpdateMemoryEstimate();
    }

    partial void OnShowOnionSkinChanged(bool value)
    {
        UpdateSettingFromProperty(AppSettingKeys.ShowOnionSkin, value);
    }

    private void UpdateMemoryEstimate()
    {
        var p = Project;
        if (p is null) { _memoryUsagePercent = 0; return; }
        var tileSize = p.TileSettings.TileSize;
        var tilesWide = (p.Width + tileSize - 1) / tileSize;
        var tilesHigh = (p.Height + tileSize - 1) / tileSize;
        var estimated = (long)tilesWide * tilesHigh * p.Layers.Count * p.FrameCount * tileSize * tileSize * 4;
        var budget = (long)p.TileSettings.MemoryBudgetMegabytes * 1024 * 1024;
        _memoryUsagePercent = (int)(Math.Min(1.0, estimated / (double)budget) * 100);
        RefreshLiveStatusBarItems(nameof(MemoryUsagePercent));
    }

    partial void OnSelectedProjectTemplateChanged(ProjectCard value)
    {
        if (value.CreationOptions is null)
        {
            return;
        }

        NewProjectName = value.CreationOptions.Name;
        NewProjectWidth = value.CreationOptions.Width;
        NewProjectHeight = value.CreationOptions.Height;
        NewProjectFramesPerSecond = value.CreationOptions.FramesPerSecond;
        NewProjectFrameCount = value.CreationOptions.FrameCount;
    }

    [RelayCommand]
    private void ToggleWorkspaceMode()
    {
        _hasManualModeOverride = true;
        WorkspaceMode = WorkspaceMode == WorkspaceMode.Studio ? WorkspaceMode.Phone : WorkspaceMode.Studio;
    }

    [RelayCommand]
    private void UseAutomaticLayout()
    {
        _hasManualModeOverride = false;
        ApplyAvailableWidth(_availableWidth);
    }


    [RelayCommand]
    private void Undo()
    {
        UndoStack.Undo();
        NotifyUndoState();
    }

    [RelayCommand]
    private void Redo()
    {
        UndoStack.Redo();
        NotifyUndoState();
    }

    public void NotifyUndoState()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    [RelayCommand]
    private void ShowProjectHome()
    {
        ActivePage = ShellRegistry.FindPage("home");
        StatusMessage = "Ready";
    }

    [RelayCommand]
    private void ShowSettings() => OpenSettingsOnPage(null);

    /// <summary>
    /// Opens the Settings dialog and navigates to <paramref name="categoryId"/> if registered.
    /// Falls back to the current selection when the id is null/empty or not found,
    /// so callers can never crash by passing an unknown page.
    /// </summary>
    [RelayCommand]
    private void OpenSettingsOnPage(string? categoryId)
    {
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            var exists = SettingsCategories.Any(c =>
                c.Id.Equals(categoryId, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                SelectedSettingsCategoryId = categoryId;
            }
            // If the page isn't registered we silently fall back — no exception, no crash.
        }
        IsSettingsVisible = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsVisible = false;
    }

    // ── New Project dialog ───────────────────────────────────────────────

    [RelayCommand]
    private void ShowNewProjectDialog()
    {
        IsNewProjectDialogVisible = true;
    }

    [RelayCommand]
    private void CloseNewProjectDialog()
    {
        IsNewProjectDialogVisible = false;
    }

    [RelayCommand]
    private void CreateProject()
    {
        var options = new ProjectCreationOptions
        {
            Name = NewProjectName,
            Width = NewProjectWidth,
            Height = NewProjectHeight,
            FrameCount = NewProjectFrameCount,
            FramesPerSecond = NewProjectFramesPerSecond
        };

        Project = _projectFactory.CreateNewProject(options);
        SelectedFrameIndex = 0;
        SelectedLayerIndex = 0;
        IsNewProjectDialogVisible = false;
        ActivePage = ShellRegistry.FindPage("studio");
        StatusMessage = "New project created";
    }

    // ── Project details ──────────────────────────────────────────────────

    partial void OnSelectedRecentProjectChanged(ProjectCard? value)
    {
        OnPropertyChanged(nameof(IsProjectDetailsVisible));
    }

    [RelayCommand]
    private void CloseProjectDetails()
    {
        SelectedRecentProject = null;
    }

    // ── Rename ───────────────────────────────────────────────────────────

    [RelayCommand]
    private void ShowRenameDialog(Presentation.ProjectCard? card)
    {
        if (card is null || string.IsNullOrEmpty(card.ProjectPath)) return;
        _renameTarget = card;
        PendingRenameText = System.IO.Path.GetFileNameWithoutExtension(card.ProjectPath);
        IsRenameDialogVisible = true;
    }

    [RelayCommand]
    private void CloseRenameDialog()
    {
        IsRenameDialogVisible = false;
        _renameTarget = null;
    }

    [RelayCommand]
    private async Task ConfirmRenameAsync()
    {
        var card = _renameTarget;
        var newName = PendingRenameText.Trim();

        if (card is null || string.IsNullOrEmpty(card.ProjectPath) || string.IsNullOrWhiteSpace(newName))
            return;

        var oldPath = card.ProjectPath;
        var dir = System.IO.Path.GetDirectoryName(oldPath)!;
        var ext = System.IO.Path.GetExtension(oldPath);
        var newPath = System.IO.Path.Combine(dir, newName + ext);

        if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            IsRenameDialogVisible = false;
            return;
        }

        if (System.IO.File.Exists(newPath))
        {
            StatusMessage = "A file with that name already exists.";
            return;
        }

        try
        {
            System.IO.File.Move(oldPath, newPath);
            await _recentProjectStore.RemoveAsync(oldPath);
            await _recentProjectStore.AddOrUpdateAsync(new RecentProjectInfo(
                newPath, newName, card.SizeLabel.Contains("×") ? 0 : 0, 0, 0, 0,
                card.ThumbnailPath, DateTimeOffset.UtcNow));
            await LoadRecentProjectsAsync();
            IsRenameDialogVisible = false;
            SelectedRecentProject = RecentProjects.FirstOrDefault(p => p.ProjectPath == newPath);
            StatusMessage = $"Renamed to \"{newName}\"";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Rename failed: {ex.Message}";
        }

        _renameTarget = null;
    }

    // ── Duplicate ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DuplicateProjectAsync(Presentation.ProjectCard? card)
    {
        if (card is null || string.IsNullOrEmpty(card.ProjectPath)) return;
        if (!System.IO.File.Exists(card.ProjectPath))
        {
            StatusMessage = "Source file not found.";
            return;
        }

        var dir = System.IO.Path.GetDirectoryName(card.ProjectPath)!;
        var stem = System.IO.Path.GetFileNameWithoutExtension(card.ProjectPath);
        var ext  = System.IO.Path.GetExtension(card.ProjectPath);

        var copyName = stem + " (Copy)";
        var copyPath = System.IO.Path.Combine(dir, copyName + ext);
        var counter = 2;
        while (System.IO.File.Exists(copyPath))
            copyPath = System.IO.Path.Combine(dir, $"{stem} (Copy {counter++}){ext}");

        copyName = System.IO.Path.GetFileNameWithoutExtension(copyPath);

        try
        {
            System.IO.File.Copy(card.ProjectPath, copyPath);

            // Open the copy so we get full project metadata for the recent entry
            var result = await _projectFileService.OpenPathAsync(copyPath);
            if (result is not null)
            {
                result.Project.Name = copyName;
                var thumbnailPath = await _projectThumbnailService.SaveThumbnailAsync(
                    result.Project, copyPath, 0);
                await _recentProjectStore.AddOrUpdateAsync(
                    _projectCardFactory.CreateRecentInfo(result.Project, copyPath, thumbnailPath));
            }
            else
            {
                await _recentProjectStore.AddOrUpdateAsync(new RecentProjectInfo(
                    copyPath, copyName, 0, 0, 0, 0, null, DateTimeOffset.UtcNow));
            }

            await LoadRecentProjectsAsync();
            SelectedRecentProject = RecentProjects.FirstOrDefault(p => p.ProjectPath == copyPath);
            StatusMessage = $"Duplicated as \"{copyName}\"";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Duplicate failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ToggleFavorite(Presentation.ProjectCard? card)
    {
        if (card is null) return;
        card.IsFavorite = !card.IsFavorite;
        await SaveFavoritesAsync();
    }

    private async System.Threading.Tasks.Task SaveFavoritesAsync()
    {
        var paths = RecentProjects
            .Where(p => p.IsFavorite && !string.IsNullOrEmpty(p.ProjectPath))
            .Select(p => p.ProjectPath!);
        await _favoritesStore.SaveAsync(paths);
    }

    private async System.Threading.Tasks.Task LoadFavoritesAsync()
    {
        var favs = await _favoritesStore.LoadAsync();
        foreach (var card in RecentProjects)
        {
            if (!string.IsNullOrEmpty(card.ProjectPath))
                card.IsFavorite = favs.Contains(card.ProjectPath);
        }
    }

    [RelayCommand]
    private async Task OpenSelectedRecentProjectAsync()
    {
        if (SelectedRecentProject is null)
        {
            StatusMessage = "No recent project selected";
            return;
        }

        await OpenRecentProjectAsync(SelectedRecentProject);
    }

    [RelayCommand]
    private async Task OpenRecentProjectAsync(ProjectCard? projectCard)
    {
        if (projectCard?.ProjectPath is null)
        {
            StatusMessage = "Recent project is missing a file path";
            return;
        }

        try
        {
            var result = await _projectFileService.OpenPathAsync(projectCard.ProjectPath);
            if (result is null)
            {
                StatusMessage = "Recent project could not be opened";
                return;
            }

            ApplyLoadedProject(result);
            await TrackRecentProjectAsync(Project, result.Path ?? projectCard.ProjectPath);
            StatusMessage = $"Opened: {result.DisplayName ?? Project.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Open failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PreviousFrame()
    {
        SelectedFrameIndex = Math.Max(0, SelectedFrameIndex - 1);
    }

    [RelayCommand]
    private void NextFrame()
    {
        SelectedFrameIndex = Math.Min(Project.FrameCount - 1, SelectedFrameIndex + 1);
    }

    [RelayCommand]
    private void AddFrame()
    {
        _timelineEditing.AddFrame(Project);
        OnPropertyChanged(nameof(TotalFrames));
    }

    [RelayCommand]
    private void DuplicateFrame()
    {
        SelectedFrameIndex = _timelineEditing.DuplicateFrame(Project, SelectedLayerIndex, SelectedFrameIndex);
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        try
        {
            var result = await _projectFileService.SaveAsync(Project);
            StatusMessage = result is null ? "Save canceled" : $"Saved: {result.DisplayName}";
            if (result?.Path is not null)
            {
                await TrackRecentProjectAsync(Project, result.Path);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveProjectAsAsync()
    {
        try
        {
            var result = await _projectFileService.SaveAsAsync(Project);
            StatusMessage = result is null ? "Save canceled" : $"Saved: {result.DisplayName}";
            if (result?.Path is not null)
            {
                await TrackRecentProjectAsync(Project, result.Path);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        try
        {
            var result = await _projectFileService.OpenAsync();
            if (result is null)
            {
                StatusMessage = "Open canceled";
                return;
            }

            ApplyLoadedProject(result);
            if (result.Path is not null)
            {
                await TrackRecentProjectAsync(Project, result.Path);
            }
            StatusMessage = $"Opened: {result.DisplayName ?? Project.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Open failed: {ex.Message}";
        }
    }

    private void ApplyLoadedProject(ProjectFileLoadResult result)
    {
        Project = result.Project;
        SelectedFrameIndex = 0;
        SelectedLayerIndex = 0;
        ActivePage = ShellRegistry.FindPage("studio");
    }

    private async Task LoadRecentProjectsAsync()
    {
        var recentProjects = await _recentProjectStore.LoadAsync();
        RecentProjects.Clear();

        foreach (var recentProject in recentProjects)
        {
            RecentProjects.Add(_projectCardFactory.CreateRecentCard(recentProject));
        }

        SelectedRecentProject = RecentProjects.FirstOrDefault();
        OnPropertyChanged(nameof(HasRecentProjects));
        OnPropertyChanged(nameof(IsRecentProjectsEmpty));
        // Re-apply favorites after cards are (re)loaded
        await LoadFavoritesAsync();
    }

    private async Task TrackRecentProjectAsync(DrawingProject project, string path)
    {
        var thumbnailPath = await _projectThumbnailService.SaveThumbnailAsync(project, path, SelectedFrameIndex);
        await _recentProjectStore.AddOrUpdateAsync(_projectCardFactory.CreateRecentInfo(project, path, thumbnailPath));
        await LoadRecentProjectsAsync();
    }

    private void BuildSettingsGroups()
    {
        _settingItems.Clear();
        SettingsGroups.Clear();

        var items = _settingValues.Registry.Definitions
            .Select(definition => new SettingItemViewModel(
                definition,
                _settingValues,
                _settingEditorResolver,
                HandleSettingChanged))
            .ToArray();

        foreach (var item in items)
        {
            _settingItems[item.Key] = item;
        }

        foreach (var group in items.GroupBy(item => item.Group).OrderBy(group => group.Key))
        {
            SettingsGroups.Add(new SettingsGroupViewModel(group.Key, group.OrderBy(item => item.Title)));
        }
    }

    private async Task LoadUserSettingsAsync()
    {
        try
        {
            await _userSettingsStore.LoadAsync(_settingValues);
            ReloadSettingItems();
            ApplyAllSettings();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Settings load failed: {ex.Message}";
        }
    }

    private async Task SaveUserSettingsAsync()
    {
        try
        {
            await _userSettingsStore.SaveAsync(_settingValues);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Settings save failed: {ex.Message}";
        }
    }

    private void HandleSettingChanged(SettingItemViewModel setting)
    {
        ApplySetting(setting.Key, setting.Value);
        _ = SaveUserSettingsAsync();
    }

    private void ReloadSettingItems()
    {
        foreach (var setting in _settingItems.Values)
        {
            setting.ReloadFromStore();
        }
    }

    private void ApplyAllSettings()
    {
        foreach (var definition in _settingValues.Registry.Definitions)
        {
            ApplySetting(definition.Key, _settingValues.GetValue(definition));
        }
    }

    private void ApplySetting(string key, object? value)
    {
        _isApplyingSettings = true;
        try
        {
            switch (key)
            {
                case AppSettingKeys.CreatorTheme when value is ProjectCreatorThemeMode themeMode:
                    ProjectCreatorThemeMode = themeMode;
                    break;
                case AppSettingKeys.ShowOnionSkin when value is bool showOnionSkin:
                    ShowOnionSkin = showOnionSkin;
                    break;
                case AppSettingKeys.DefaultFrameRate when value is int framesPerSecond:
                    EnsureFrameRateAvailable(framesPerSecond);
                    NewProjectFramesPerSecond = framesPerSecond;
                    break;
                case AppSettingKeys.TileMemoryBudget when value is int memoryBudget:
                    Project.TileSettings.MemoryBudgetMegabytes = memoryBudget;
                    OnPropertyChanged(nameof(Project));
                    break;
            }
        }
        finally
        {
            _isApplyingSettings = false;
        }
    }

    private void UpdateSettingFromProperty(string key, object value)
    {
        if (_isApplyingSettings)
        {
            return;
        }

        if (!_settingValues.SetValue(key, value, out _))
        {
            return;
        }

        if (_settingItems.TryGetValue(key, out var setting))
        {
            setting.ReloadFromStore();
        }

        _ = SaveUserSettingsAsync();
    }

    private void EnsureFrameRateAvailable(int framesPerSecond)
    {
        if (AvailableFrameRates.Contains(framesPerSecond))
        {
            return;
        }

        AvailableFrameRates.Add(framesPerSecond);
        var orderedRates = AvailableFrameRates.OrderBy(rate => rate).ToArray();
        AvailableFrameRates.Clear();
        foreach (var rate in orderedRates)
        {
            AvailableFrameRates.Add(rate);
        }
    }
}

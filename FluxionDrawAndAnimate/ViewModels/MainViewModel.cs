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
using FluxionDrawAndAnimate.Presentation.Shell;
using FluxionDrawAndAnimate.Presentation.StatusBar;
using FluxionDrawAndAnimate.Services;
using FluxionDrawAndAnimate.Ui.Core;

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
    private readonly LayoutProfileFactory _layoutProfileFactory = new();
    private readonly Dictionary<string, SettingItemViewModel> _settingItems = new(StringComparer.OrdinalIgnoreCase);
    private bool _isApplyingSettings;
    private double _availableWidth;
    private bool _hasManualModeOverride;

    public UndoStack UndoStack { get; }
    public AppShellRegistry ShellRegistry { get; } = new();
    public StatusBarRegistry StatusBarRegistry { get; } = new();
    public HomePageRegistry HomePageRegistry { get; } = new();

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
    }

    [ObservableProperty]
    private DrawingProject _project = default!;

    [ObservableProperty]
    private bool _isSettingsVisible;

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

    // ── Home page sidebar navigation ────────────────────────────────────────

    [ObservableProperty]
    private string _activeSidebarItemId = "home";

    public IReadOnlyList<HomeNavItem> HomeNavItems { get; } =
    [
        new("home",      "⌂",  "Home"),
        new("projects",  "⊡",  "Projects",  "BROWSE"),
        new("recent",    "◷",  "Recent"),
        new("templates", "⊞",  "Templates"),
        new("learn",     "✦",  "Learn"),
        new("cloud",     "☁",  "Cloud",     "LIBRARY"),
        new("trash",     "⊘",  "Trash"),
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
    [ObservableProperty]
    private double _canvasZoom = 1.0;

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
        ActivePage = ShellRegistry.FindPage("home");
        RebuildLiveStatusBarItems();
        // Initialise sidebar active state
        OnActiveSidebarItemIdChanged(_activeSidebarItemId);

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
    }

    public ObservableCollection<ToolPreset> Tools { get; }
    public ObservableCollection<ProjectCard> ProjectTemplates { get; }
    public ObservableCollection<ProjectCard> RecentProjects { get; }
    public ObservableCollection<int> AvailableFrameRates { get; }
    public ObservableCollection<SettingsGroupViewModel> SettingsGroups { get; }

    // Derived from ActivePage so the shell title bar and legacy bindings stay in sync.
    public bool IsProjectHomeVisible => ActivePage?.Id == "home" || ActivePage is null;
    public bool IsEditorVisible => ActivePage?.Id == "studio";

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
    public ToolKind ActiveToolKind => ActiveTool.Kind;
    public string ActiveToolName => ActiveTool.Name;
    /// <summary>B//5: the active preset's brush settings, bound to the canvas.</summary>
    public BrushSettings? ActiveBrushSettings => ActiveTool?.BrushPreset?.Settings;
    public int DisplayFrame => SelectedFrameIndex + 1;
    public int TotalFrames => Project.FrameCount;

    partial void OnWorkspaceModeChanged(WorkspaceMode value)
    {
        OnPropertyChanged(nameof(ModeButtonText));
        OnPropertyChanged(nameof(IsStudioMode));
        OnPropertyChanged(nameof(IsPhoneMode));
        NotifyLayoutChanged();
    }

    private void NotifyLayoutChanged()
    {
        OnPropertyChanged(nameof(ActiveProfileKind));
        OnPropertyChanged(nameof(WorkspaceLayoutProfile));
        OnPropertyChanged(nameof(ShowSidePanel));
        OnPropertyChanged(nameof(TimelineHeight));
        OnPropertyChanged(nameof(PropertiesPanelWidth));
        OnPropertyChanged(nameof(IsLayoutAutomatic));
        OnPropertyChanged(nameof(IsHomePhoneLayout));
        OnPropertyChanged(nameof(IsHomeWideLayout));
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

    partial void OnCanvasZoomChanged(double value) =>
        RefreshLiveStatusBarItems(nameof(CanvasZoom));

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

    partial void OnActiveToolChanged(ToolPreset value)
    {
        OnPropertyChanged(nameof(ActiveToolKind));
        OnPropertyChanged(nameof(ActiveToolName));
        OnPropertyChanged(nameof(ActiveBrushSettings));
        RefreshLiveStatusBarItems(nameof(ActiveToolName));
    }

    partial void OnBrushBaseColorChanged(RgbaColor value)
    {
        OnPropertyChanged(nameof(BrushColor));
    }

    partial void OnBrushSizeSettingChanged(double value)
    {
        OnPropertyChanged(nameof(BrushSize));
        RefreshLiveStatusBarItems(nameof(BrushSize));
    }

    partial void OnBrushOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(BrushColor));
    }

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
    private void ShowSettings()
    {
        IsSettingsVisible = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsVisible = false;
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
        ActivePage = ShellRegistry.FindPage("studio");
        StatusMessage = "New project created";
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

using System.IO.Compression;
using System.Text.Json;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Color;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Tiling;

namespace FluxionDrawAndAnimate.Core.Persistence;

public sealed class ProjectArchiveStore : IProjectArchiveStore
{
    private const int CurrentVersion = 2;
    private const string ProjectEntryName = "project.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task SaveAsync(DrawingProject project, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(destination);

        project.ModifiedAt = DateTimeOffset.UtcNow;

        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        var document = CreateDocument(project);

        foreach (var layer in project.Layers)
        {
            var layerDocument = new LayerDocument
            {
                Id = layer.Id,
                Name = layer.Name,
                Type = layer.Type,
                IsVisible = layer.IsVisible,
                IsLocked = layer.IsLocked,
                Opacity = layer.Opacity,
                BlendMode = layer.BlendMode
            };

            document.Layers.Add(layerDocument);
            await WriteLayerFramesAsync(archive, layer, layerDocument, cancellationToken);
        }

        foreach (var audioClip in project.AudioClips)
        {
            document.AudioClips.Add(new AudioClipDocument
            {
                Name = audioClip.Name,
                StartFrame = audioClip.StartFrame,
                DurationFrames = audioClip.DurationFrames
            });
        }

        var projectEntry = archive.CreateEntry(ProjectEntryName, CompressionLevel.Optimal);
        await using var projectStream = projectEntry.Open();
        await JsonSerializer.SerializeAsync(projectStream, document, JsonOptions, cancellationToken);
    }

    public async Task<DrawingProject> LoadAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        var projectEntry = archive.GetEntry(ProjectEntryName)
            ?? throw new InvalidDataException("The project archive does not contain project.json.");

        await using var projectStream = projectEntry.Open();
        var document = await JsonSerializer.DeserializeAsync<ProjectDocument>(projectStream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("The project metadata is empty or invalid.");

        if (document.Format != ProjectDocument.FormatName || document.Version > CurrentVersion)
        {
            throw new InvalidDataException("The project archive format is not supported.");
        }

        return await BuildProjectAsync(archive, document, cancellationToken);
    }

    private static ProjectDocument CreateDocument(DrawingProject project)
    {
        return new ProjectDocument
        {
            Version = CurrentVersion,
            Id = project.Id,
            CreatedAt = project.CreatedAt,
            ModifiedAt = project.ModifiedAt,
            Name = project.Name,
            Width = project.Width,
            Height = project.Height,
            FrameCount = project.FrameCount,
            FramesPerSecond = project.FramesPerSecond,
            ColorProfile = project.ColorProfile,
            TileSettings = project.TileSettings.Normalize()
        };
    }

    private static async Task WriteLayerFramesAsync(
        ZipArchive archive,
        AnimationLayer layer,
        LayerDocument layerDocument,
        CancellationToken cancellationToken)
    {
        for (var frameIndex = 0; frameIndex < layer.Frames.Count; frameIndex++)
        {
            var frame = layer.Frames[frameIndex];
            if (frame.Strokes.Count == 0)
            {
                continue;
            }

            var path = $"frames/{SafePathSegment(layer.Id)}/{frame.Number:D6}.strokes.json";
            var frameEntry = archive.CreateEntry(path, CompressionLevel.Optimal);
            await using var frameStream = frameEntry.Open();

            var strokes = frame.Strokes.Select(CreateStrokeDocument).ToList();
            await JsonSerializer.SerializeAsync(frameStream, strokes, JsonOptions, cancellationToken);

            layerDocument.Frames.Add(new FrameDocument
            {
                Number = frame.Number,
                StrokesPath = path
            });
        }
    }

    private static async Task<DrawingProject> BuildProjectAsync(
        ZipArchive archive,
        ProjectDocument document,
        CancellationToken cancellationToken)
    {
        var project = new DrawingProject
        {
            Id = string.IsNullOrWhiteSpace(document.Id) ? Guid.NewGuid().ToString("N") : document.Id,
            CreatedAt = document.CreatedAt == default ? DateTimeOffset.UtcNow : document.CreatedAt,
            ModifiedAt = document.ModifiedAt == default ? DateTimeOffset.UtcNow : document.ModifiedAt,
            Name = document.Name,
            Width = document.Width,
            Height = document.Height,
            FrameCount = document.FrameCount,
            FramesPerSecond = document.FramesPerSecond,
            ColorProfile = document.ColorProfile ?? new ColorProfile(),
            TileSettings = (document.TileSettings ?? new TileSettings()).Normalize()
        };

        project.Layers.Clear();
        foreach (var layerDocument in document.Layers)
        {
            var layer = new AnimationLayer(layerDocument.Id, layerDocument.Name, project.FrameCount, layerDocument.Type)
            {
                IsVisible = layerDocument.IsVisible,
                IsLocked = layerDocument.IsLocked,
                Opacity = Math.Clamp(layerDocument.Opacity, 0, 1),
                BlendMode = layerDocument.BlendMode
            };

            foreach (var frameDocument in layerDocument.Frames)
            {
                var frameEntry = archive.GetEntry(frameDocument.StrokesPath)
                    ?? throw new InvalidDataException($"Missing frame strokes entry: {frameDocument.StrokesPath}");

                await using var frameStream = frameEntry.Open();
                var strokeDocuments = await JsonSerializer.DeserializeAsync<List<StrokeDocument>>(
                    frameStream,
                    JsonOptions,
                    cancellationToken) ?? [];

                var frame = layer.EnsureFrame(Math.Max(0, frameDocument.Number - 1));
                foreach (var strokeDocument in strokeDocuments)
                {
                    frame.Strokes.Add(CreateStroke(strokeDocument));
                }
            }

            project.Layers.Add(layer);
        }

        foreach (var audioDocument in document.AudioClips)
        {
            project.AudioClips.Add(new AudioClip(
                audioDocument.Name,
                audioDocument.StartFrame,
                audioDocument.DurationFrames));
        }

        if (project.Layers.Count == 0)
        {
            project.Layers.Add(new AnimationLayer("Layer 1", project.FrameCount));
        }

        return project;
    }

    private static StrokeDocument CreateStrokeDocument(StrokePath stroke)
    {
        return new StrokeDocument
        {
            Color = stroke.Color,
            Size = stroke.Size,
            ToolKind = stroke.ToolKind,
            IsVector = stroke.IsVector,
            BrushShape = stroke.BrushSettings.Shape,
            BrushHardness = stroke.BrushSettings.Hardness,
            SizeBinding = ToBindingDocument(stroke.BrushSettings.Dynamics.SizeBinding),
            OpacityBinding = ToBindingDocument(stroke.BrushSettings.Dynamics.OpacityBinding),
            Points = stroke.Points.ToList()
        };
    }

    private static StrokePath CreateStroke(StrokeDocument document)
    {
        var dynamics = new BrushDynamics
        {
            SizeBinding = FromBindingDocument(document.SizeBinding),
            OpacityBinding = FromBindingDocument(document.OpacityBinding)
        };
        var settings = new BrushSettings
        {
            Shape = document.BrushShape,
            Hardness = document.BrushHardness,
            Dynamics = dynamics
        };
        var stroke = new StrokePath(document.Color, document.Size, document.ToolKind, document.IsVector, settings);
        stroke.Points.AddRange(document.Points);
        return stroke;
    }

    private static SensorBindingDocument? ToBindingDocument(SensorBinding? binding)
    {
        if (binding is null) return null;
        return new SensorBindingDocument
        {
            Sensor = binding.Sensor,
            CurvePoints = binding.Curve.Points.Select(p => new[] { p.X, p.Y }).ToList()
        };
    }

    private static SensorBinding? FromBindingDocument(SensorBindingDocument? doc)
    {
        if (doc is null) return null;
        var curve = new ResponseCurve
        {
            Points = doc.CurvePoints
                .Where(p => p.Length >= 2)
                .Select(p => new CurvePoint(p[0], p[1]))
                .ToList()
        };
        if (curve.Points.Count == 0) curve.Points.AddRange(ResponseCurve.Linear().Points);
        return new SensorBinding { Sensor = doc.Sensor, Curve = curve };
    }

    private static string SafePathSegment(string value)
    {
        return string.Join("_", value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed class ProjectDocument
    {
        public const string FormatName = "fluxion.project";

        public string Format { get; set; } = FormatName;
        public int Version { get; set; }
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public string Name { get; set; } = "Untitled";
        public int Width { get; set; }
        public int Height { get; set; }
        public int FrameCount { get; set; }
        public int FramesPerSecond { get; set; }
        public ColorProfile? ColorProfile { get; set; }
        public TileSettings? TileSettings { get; set; }
        public List<LayerDocument> Layers { get; set; } = [];
        public List<AudioClipDocument> AudioClips { get; set; } = [];
    }

    private sealed class LayerDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Layer";
        public LayerType Type { get; set; } = LayerType.Raster;
        public bool IsVisible { get; set; } = true;
        public bool IsLocked { get; set; }
        public double Opacity { get; set; } = 1.0;
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;
        public List<FrameDocument> Frames { get; set; } = [];
    }

    private sealed class FrameDocument
    {
        public int Number { get; set; }
        public string StrokesPath { get; set; } = string.Empty;
    }

    private sealed class StrokeDocument
    {
        public RgbaColor Color { get; set; }
        public double Size { get; set; }
        public ToolKind ToolKind { get; set; }
        public bool IsVector { get; set; }
        public BrushShape BrushShape { get; set; } = BrushShape.Round;
        public double BrushHardness { get; set; } = 0.8;
        public SensorBindingDocument? SizeBinding { get; set; }
        public SensorBindingDocument? OpacityBinding { get; set; }
        public List<PaintInformation> Points { get; set; } = [];
    }

    private sealed class SensorBindingDocument
    {
        public SensorKind Sensor { get; set; }
        public List<double[]> CurvePoints { get; set; } = [];
    }

    private sealed class AudioClipDocument
    {
        public string Name { get; set; } = string.Empty;
        public int StartFrame { get; set; }
        public int DurationFrames { get; set; }
    }
}

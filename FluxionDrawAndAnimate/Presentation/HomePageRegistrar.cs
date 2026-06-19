using System;
using System.Linq;
using FluxionDrawAndAnimate.ViewModels;

namespace FluxionDrawAndAnimate.Presentation;

/// <summary>
/// Registers the default quick actions and welcome text on the home page.
/// Welcome name comes from the OS user; subtitle rotates randomly each session.
/// </summary>
public static class HomePageRegistrar
{
    private static readonly string[] Subtitles =
    [
        "Ready to bring your ideas to life?",
        "What will you create today?",
        "Your canvas is waiting.",
        "Time to make something amazing.",
        "Let's animate the world.",
        "Every great film starts with a single frame.",
        "Creativity is calling.",
        "Pick up the brush and begin.",
        "New day, new creation.",
        "The blank canvas is full of possibilities.",
        "Draw, animate, inspire.",
        "Your next masterpiece starts here.",
    ];

    public static void RegisterDefaults(HomePageRegistry registry, MainViewModel vm)
    {
        // OS username, capitalised (e.g. "kisstp" → "Kisstp")
        var raw = Environment.UserName ?? "Artist";
        var name = char.ToUpperInvariant(raw[0]) + raw[1..];
        registry.WelcomeName = name;

        // Random subtitle — different every session
        registry.WelcomeSubtitle = Subtitles[new Random().Next(Subtitles.Length)];

        registry.RegisterQuickAction(new QuickActionDefinition
        {
            Id = "new-studio",
            Title = "New Studio Project",
            Subtitle = "Blank canvas  •  Studio workspace",
            Icon = "+",
            Command = vm.CreateProjectCommand,
            IsPrimary = true,
            SortOrder = 0
        });

        registry.RegisterQuickAction(new QuickActionDefinition
        {
            Id = "new-drawing",
            Title = "New Drawing Project",
            Subtitle = "Canvas  •  Layers",
            Icon = "✎",
            Command = vm.CreateProjectCommand,
            SortOrder = 1
        });

        registry.RegisterQuickAction(new QuickActionDefinition
        {
            Id = "open-existing",
            Title = "Open Existing",
            Subtitle = "Browse your files",
            Icon = "📁",
            Command = vm.OpenProjectCommand,
            SortOrder = 2
        });

        registry.RegisterQuickAction(new QuickActionDefinition
        {
            Id = "recover-autosave",
            Title = "Recover Autosave",
            Subtitle = "No autosaves found",
            Icon = "↺",
            SortOrder = 3
        });
    }
}

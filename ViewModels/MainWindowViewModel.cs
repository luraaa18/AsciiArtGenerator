using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Figgle.Fonts;

namespace AsciiArtApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string inputText = "Hello!";

    [ObservableProperty]
    private string textAsciiOutput = string.Empty;

    [ObservableProperty]
    private string selectedFont = "Standard";

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public List<string> AvailableFonts { get; } = new()
    {
        "Standard",
        "Big",
        "Slant",
        "Small",
        "Block",
        "Banner",
        "Doom",
        "Shadow",
        "Script",
        "ThreePoint",
        "Mini"
    };

    [RelayCommand]
    public void GenerateTextAscii()
    {
        try
        {
            StatusMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(InputText))
            {
                TextAsciiOutput = "(Type something above to see the magic)";
                return;
            }

            TextAsciiOutput = RenderWithFont(SelectedFont, InputText);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private string RenderWithFont(string fontName, string text)
    {
        return fontName switch
        {
            "Standard" => FiggleFonts.Standard.Render(text),
            "Big" => FiggleFonts.Big.Render(text),
            "Slant" => FiggleFonts.Slant.Render(text),
            "Small" => FiggleFonts.Small.Render(text),
            "Block" => FiggleFonts.Block.Render(text),
            "Banner" => FiggleFonts.Banner.Render(text),
            "Doom" => FiggleFonts.Doom.Render(text),
            "Shadow" => FiggleFonts.Shadow.Render(text),
            "Script" => FiggleFonts.Script.Render(text),
            "ThreePoint" => FiggleFonts.ThreePoint.Render(text),
            "Mini" => FiggleFonts.Mini.Render(text),
            _ => FiggleFonts.Standard.Render(text)
        };
    }

    [RelayCommand]
    public async Task CopyTextAscii()
    {
        if (string.IsNullOrEmpty(TextAsciiOutput))
        {
            StatusMessage = "Nothing to copy yet!";
            return;
        }

        try
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(TextAsciiOutput);
                StatusMessage = "Copied to clipboard ✓";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Copy failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SaveTextAscii()
    {
        if (string.IsNullOrEmpty(TextAsciiOutput))
        {
            StatusMessage = "Nothing to save yet!";
            return;
        }

        try
        {
            var storage = GetStorageProvider();
            if (storage == null) return;

            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save ASCII Art",
                SuggestedFileName = "ascii_art.txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text File") { Patterns = new[] { "*.txt" } }
                }
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(TextAsciiOutput);
                StatusMessage = $"Saved to {file.Name} ✓";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    private IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }
        return null;
    }

    private IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.StorageProvider;
        }
        return null;
    }
}
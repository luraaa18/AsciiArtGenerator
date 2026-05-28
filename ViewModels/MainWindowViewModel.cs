using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Figgle.Fonts;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using IsImage = SixLabors.ImageSharp.Image;
using System.Linq;

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
        await SaveAsciiToFile(TextAsciiOutput, "text_ascii.txt", msg => StatusMessage = msg);
    }

    [ObservableProperty]
    private string loadedImageName = "(no image loaded)";

    [ObservableProperty]
    private string imageAsciiOutput = string.Empty;

    [ObservableProperty]
    private string imageStatusMessage = string.Empty;

    [ObservableProperty]
    private int outputWidth = 100;

    [ObservableProperty]
    private string selectedCharSet = "Classic";

    [ObservableProperty]
    private bool useColor = false;

    [ObservableProperty]
    private bool invertColors = false;

    [ObservableProperty]
    private Bitmap? coloredImageBitmap;

    private string? _loadedImagePath;

    public List<string> AvailableCharSets { get; } = new()
    {
        "Classic",
        "Detailed",
        "Blocks",
        "Simple",
        "Binary"
    };

    private string GetCharSet(string name)
    {
        return name switch
        {
            "Classic" => "@%#*+=-:. ",
            "Detailed" => "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'. ",
            "Blocks" => "█▓▒░ ",
            "Simple" => "#*+- ",
            "Binary" => "10 ",
            _ => "@%#*+=-:. "
        };
    }

    [RelayCommand]
    public async Task LoadImage()
    {
        try
        {
            var storage = GetStorageProvider();
            if (storage == null) return;

            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select an image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp" }
                    }
                }
            });

            if (files != null && files.Count > 0)
            {
                _loadedImagePath = files[0].Path.LocalPath;
                LoadedImageName = $"📷 {files[0].Name}";
                ImageStatusMessage = "Image loaded. Click Generate to convert!";
            }
        }
        catch (Exception ex)
        {
            ImageStatusMessage = $"Error loading image: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task GenerateImageAscii()
    {
        if (string.IsNullOrEmpty(_loadedImagePath))
        {
            ImageStatusMessage = "Load an image first!";
            return;
        }

        try
        {
            ImageStatusMessage = "Processing...";

            if (UseColor)
            {
                var bitmap = await Task.Run(() =>
                    ConvertImageToColoredBitmap(_loadedImagePath, OutputWidth, GetCharSet(SelectedCharSet), InvertColors));

                ColoredImageBitmap = bitmap;
                ImageAsciiOutput = string.Empty;
            }
            else
            {
                await Task.Run(() =>
                {
                    var ascii = ConvertImageToAscii(_loadedImagePath, OutputWidth, GetCharSet(SelectedCharSet), InvertColors);
                    ImageAsciiOutput = ascii;
                });

                ColoredImageBitmap = null;
            }

            ImageStatusMessage = "Done! ✓";
        }
        catch (Exception ex)
        {
            ImageStatusMessage = $"Error: {ex.Message}";
        }
    }

    private string ConvertImageToAscii(string imagePath, int width, string charSet, bool invert)
    {
        using var image = IsImage.Load<Rgba32>(imagePath);

        double aspectRatio = (double)image.Height / image.Width;
        int height = (int)(width * aspectRatio * 0.5);

        image.Mutate(x => x.Resize(width, height));

        var sb = new StringBuilder();

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];

                double brightness = (0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);

                if (invert)
                    brightness = 255 - brightness;

                int charIndex = (int)(brightness / 255.0 * (charSet.Length - 1));
                charIndex = Math.Clamp(charIndex, 0, charSet.Length - 1);

                sb.Append(charSet[charIndex]);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private Bitmap ConvertImageToColoredBitmap(string imagePath, int width, string charSet, bool invert)
    {
        using var sourceImage = IsImage.Load<Rgba32>(imagePath);

        double aspectRatio = (double)sourceImage.Height / sourceImage.Width;
        int height = (int)(width * aspectRatio * 0.5);

        sourceImage.Mutate(x => x.Resize(width, height));

        int cellWidth = 8;
        int cellHeight = 14;
        int outputW = width * cellWidth;
        int outputH = height * cellHeight;

        using var output = new Image<Rgba32>(outputW, outputH, new Rgba32(30, 30, 46));

        Font font;
        try
        {
            font = SystemFonts.CreateFont("Consolas", 12, FontStyle.Regular);
        }
        catch
        {
            font = SystemFonts.CreateFont(SystemFonts.Families.First().Name, 12, FontStyle.Regular);
        }

        for (int y = 0; y < sourceImage.Height; y++)
        {
            for (int x = 0; x < sourceImage.Width; x++)
            {
                var pixel = sourceImage[x, y];

                double brightness = (0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                if (invert) brightness = 255 - brightness;

                int charIndex = (int)(brightness / 255.0 * (charSet.Length - 1));
                charIndex = Math.Clamp(charIndex, 0, charSet.Length - 1);

                char c = charSet[charIndex];
                var color = SixLabors.ImageSharp.Color.FromRgb(pixel.R, pixel.G, pixel.B);

                int px = x * cellWidth;
                int py = y * cellHeight;

                output.Mutate(ctx => ctx.DrawText(c.ToString(), font, color, new PointF(px, py)));
            }
        }

        using var ms = new MemoryStream();
        output.SaveAsPng(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    [RelayCommand]
    public async Task CopyImageAscii()
    {
        if (string.IsNullOrEmpty(ImageAsciiOutput))
        {
            ImageStatusMessage = "Nothing to copy yet!";
            return;
        }

        try
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(ImageAsciiOutput);
                ImageStatusMessage = "Copied to clipboard ✓";
            }
        }
        catch (Exception ex)
        {
            ImageStatusMessage = $"Copy failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SaveImageAscii()
    {
        await SaveAsciiToFile(ImageAsciiOutput, "image_ascii.txt", msg => ImageStatusMessage = msg);
    }

    private async Task SaveAsciiToFile(string content, string suggestedName, Action<string> setStatus)
    {
        if (string.IsNullOrEmpty(content))
        {
            setStatus("Nothing to save yet!");
            return;
        }

        try
        {
            var storage = GetStorageProvider();
            if (storage == null) return;

            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save ASCII Art",
                SuggestedFileName = suggestedName,
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
                await writer.WriteAsync(content);
                setStatus($"Saved to {file.Name} ✓");
            }
        }
        catch (Exception ex)
        {
            setStatus($"Save failed: {ex.Message}");
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
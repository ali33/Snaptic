using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SkiaSharp;
using Snaptic.App.ViewModels;
using Snaptic.Core.Barcodes;

namespace Snaptic.App.Views;

public partial class GeneratorWindow : Window
{
    private readonly GeneratorViewModel _vm;

    /// <summary>Ảnh mã vừa tạo. App mở PreviewWindow với ảnh này.</summary>
    public event Action<SKBitmap>? CodeGenerated;

    /// <summary>Ctor rỗng cho XAML designer — không dùng lúc chạy.</summary>
    public GeneratorWindow() : this(SnapticFormat.Qr) { }

    public GeneratorWindow(SnapticFormat initialFormat)
    {
        InitializeComponent();
        _vm = new GeneratorViewModel(initialFormat);
        DataContext = _vm;

        FormatCombo.ItemsSource = GeneratorViewModel.AllFormats
            .Select(BarcodeFormatSpec.DisplayName)
            .ToList();
        FormatCombo.SelectedIndex = GeneratorViewModel.AllFormats.ToList().IndexOf(initialFormat);

        FormatCombo.SelectionChanged += (_, _) =>
        {
            if (FormatCombo.SelectedIndex >= 0)
                _vm.Format = GeneratorViewModel.AllFormats[FormatCombo.SelectedIndex];
        };

        ContentInput.TextChanged += (_, _) => _vm.Text = ContentInput.Text ?? string.Empty;

        CreateButton.Click += (_, _) =>
        {
            // Generate() trả ảnh MỚI, không phải _vm.Preview — preview bị huỷ mỗi lần gõ.
            if (_vm.Generate() is { } bitmap)
            {
                CodeGenerated?.Invoke(bitmap);
                Close();
            }
        };

        CloseButton.Click += (_, _) => Close();

        _vm.PropertyChanged += OnVmPropertyChanged;
        Closed += (_, _) =>
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.Dispose();
        };

        Sync();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => Dispatcher.UIThread.Post(Sync);

    private void Sync()
    {
        CreateButton.IsEnabled = _vm.CanGenerate;

        HintText.Text = _vm.Hint;
        HintText.IsVisible = !string.IsNullOrEmpty(_vm.Hint);

        PreviewImage.Source = _vm.Preview is { } p ? ToAvaloniaBitmap(p) : null;
    }

    private static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }
}

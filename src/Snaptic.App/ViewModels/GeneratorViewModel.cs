using System.ComponentModel;
using System.Runtime.CompilerServices;
using SkiaSharp;
using Snaptic.Core.Barcodes;

namespace Snaptic.App.ViewModels;

/// <summary>
/// Logic cửa sổ tạo mã. Ảnh mã cập nhật ngay khi gõ; nút Tạo khoá khi nội dung không
/// hợp lệ kèm lời nhắc cụ thể thay vì báo lỗi khô khan.
/// </summary>
public sealed class GeneratorViewModel : INotifyPropertyChanged, IDisposable
{
    public static IReadOnlyList<SnapticFormat> AllFormats { get; } = Enum.GetValues<SnapticFormat>();

    private string _text = string.Empty;
    private SnapticFormat _format;
    private SKBitmap? _preview;

    public GeneratorViewModel(SnapticFormat initialFormat) => _format = initialFormat;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            Raise();
            Revalidate();
        }
    }

    public SnapticFormat Format
    {
        get => _format;
        set
        {
            if (_format == value) return;
            _format = value;
            Raise();
            Revalidate();   // đổi định dạng phải validate lại text hiện có
        }
    }

    public string FormatDisplayName => BarcodeFormatSpec.DisplayName(_format);

    public bool CanGenerate => BarcodeFormatSpec.Validate(_format, _text).IsValid;

    public string? Hint => BarcodeFormatSpec.Validate(_format, _text).Hint;

    /// <summary>
    /// Ảnh xem trước. VIEWMODEL SỞ HỮU nó và huỷ mỗi lần gõ — nơi gọi không được giữ
    /// tham chiếu. Cần ảnh để dùng lâu dài thì gọi <see cref="Generate"/>.
    /// </summary>
    public SKBitmap? Preview => _preview;

    /// <summary>
    /// Ảnh mã để giao cho cửa sổ preview. Trả về ảnh MỚI chứ không phải <see cref="Preview"/>:
    /// ảnh preview bị huỷ ngay khi người dùng gõ thêm một ký tự, nên trả nó đi là giao
    /// một cái xác. Trả null khi nội dung không hợp lệ.
    /// </summary>
    public SKBitmap? Generate()
        => CanGenerate ? BarcodeGenerator.Generate(_format, _text) : null;

    private void Revalidate()
    {
        _preview?.Dispose();
        _preview = CanGenerate ? BarcodeGenerator.Generate(_format, _text) : null;

        Raise(nameof(CanGenerate));
        Raise(nameof(Hint));
        Raise(nameof(Preview));
        Raise(nameof(FormatDisplayName));
    }

    public void Dispose()
    {
        _preview?.Dispose();
        _preview = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

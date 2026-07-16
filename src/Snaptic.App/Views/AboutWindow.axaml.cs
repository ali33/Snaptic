using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Snaptic.App.ViewModels;

namespace Snaptic.App.Views;

public partial class AboutWindow : Window
{
    /// <summary>Ctor rỗng cho XAML designer — không dùng lúc chạy.</summary>
    public AboutWindow() : this(null) { }

    public AboutWindow(AboutViewModel? vm)
    {
        InitializeComponent();
        if (vm is null)
            return;

        DataContext = vm;

        AppIcon.Source = Load("avares://Snaptic.App/Assets/snaptic.ico");

        // Ảnh QR được đóng gói vào app chứ không đọc từ docs/ — bản publish không có
        // thư mục đó, đọc từ đĩa là cửa sổ này trống trơn khi cài thật.
        DonateQr.Source = Load("avares://Snaptic.App/Assets/donate-qr.png");

        VersionText.Text = $"Phiên bản {vm.Version}";
        DedicationText.Text = vm.Dedication;
        PurposeText.Text = vm.Purpose;
        DonateText.Text = vm.DonateCall;
        BankText.Text = vm.BankName;
        HolderText.Text = vm.AccountHolder;
        AccountText.Text = vm.AccountNumberDisplay;

        CopyrightText.Text = vm.Copyright;
        LicenseText.Text = $"{vm.License}  {vm.LicenseUrl}";
        DisclaimerText.Text = vm.Disclaimer;
        LimitsText.Text = vm.KnownLimits;
        LegalNoteText.Text =
            "Đây là bản tóm tắt tiếng Việt cho dễ đọc. Văn bản có hiệu lực pháp lý là " +
            "mục 15, 16 và 17 trong file LICENSE (tiếng Anh).";

        CopyAccountButton.Click += (_, _) =>
        {
            try
            {
                vm.CopyAccountNumber();
                ToastWindow.Show(this, $"Đã copy số tài khoản {vm.AccountNumber}");
            }
            catch (InvalidOperationException ex)
            {
                // Clipboard bị app khác giữ — lỗi có thật, không được để sập cửa sổ.
                ToastWindow.Show(this, ex.Message);
            }
        };

        DownloadButton.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(vm.DownloadUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Không có trình duyệt mặc định, hoặc shell từ chối. Hiếm, nhưng để nó
                // ném ra là sập cả cửa sổ Giới thiệu chỉ vì một cái link.
                ToastWindow.Show(this, $"Không mở được trình duyệt: {ex.Message}");
            }
        };

        CloseButton.Click += (_, _) => Close();
    }

    private static Bitmap Load(string uri) => new(AssetLoader.Open(new Uri(uri)));
}

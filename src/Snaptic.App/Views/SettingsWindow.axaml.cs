using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Snaptic.App.ViewModels;
using Snaptic.Core.Settings;

namespace Snaptic.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel? _vm;
    private bool _capturingHotkey;

    /// <summary>Ctor rỗng cho XAML designer — không dùng lúc chạy.</summary>
    public SettingsWindow() : this(null) { }

    public SettingsWindow(SettingsViewModel? vm)
    {
        InitializeComponent();
        _vm = vm;
        if (vm is null)
            return;

        DataContext = vm;

        LanguageCombo.ItemsSource = vm.AvailableLanguages;
        LanguageCombo.SelectedItem = vm.SelectedLanguage;
        LanguageCombo.IsEnabled = !vm.HasNoOcrLanguage;
        NoLanguageText.IsVisible = vm.HasNoOcrLanguage;

        FolderInput.Text = vm.SaveFolder;
        StartupCheck.IsChecked = vm.StartWithWindows;
        StartupCheck.IsCheckedChanged += (_, _) => vm.StartWithWindows = StartupCheck.IsChecked == true;

        HotkeyButton.Click += (_, _) =>
        {
            _capturingHotkey = true;
            Sync();
        };
        KeyDown += OnKeyDown;

        BrowseButton.Click += async (_, _) => await BrowseFolderAsync();

        SaveButton.Click += (_, _) =>
        {
            vm.SelectedLanguage = LanguageCombo.SelectedItem as string;
            vm.SaveFolder = FolderInput.Text ?? vm.SaveFolder;
            vm.Save();

            // Save() có thể trả ô tick về đúng thực tế nếu đổi registry hỏng.
            // Không đóng cửa sổ khi đó — người dùng cần thấy lỗi.
            if (vm.StartupError is null)
                Close();
            else
                Sync();
        };

        CancelButton.Click += (_, _) =>
        {
            vm.RestoreHotkey();   // hoàn tác phím tắt đã gán mà chưa lưu
            Close();
        };

        vm.PropertyChanged += OnVmPropertyChanged;
        Closed += (_, _) => vm.PropertyChanged -= OnVmPropertyChanged;

        Sync();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => Dispatcher.UIThread.Post(Sync);

    private void Sync()
    {
        if (_vm is null)
            return;

        HotkeyButton.Content = _capturingHotkey ? "Đang chờ... gõ tổ hợp phím" : _vm.Hotkey.ToString();

        HotkeyErrorText.Text = _vm.HotkeyError;
        HotkeyErrorText.IsVisible = !string.IsNullOrEmpty(_vm.HotkeyError);

        StartupCheck.IsChecked = _vm.StartWithWindows;
        StartupErrorText.Text = _vm.StartupError;
        StartupErrorText.IsVisible = !string.IsNullOrEmpty(_vm.StartupError);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_capturingHotkey || _vm is null)
            return;

        // Bỏ qua khi mới chỉ bấm phím bổ trợ — chờ phím chính.
        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                  or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        e.Handled = true;
        _capturingHotkey = false;

        var mods = HotkeyModifiers.None;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) mods |= HotkeyModifiers.Control;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) mods |= HotkeyModifiers.Alt;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) mods |= HotkeyModifiers.Shift;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Meta)) mods |= HotkeyModifiers.Win;

        // Chuẩn hoá tên phím của Avalonia sang tên chuẩn của Snaptic. Avalonia gọi phím
        // số là "D1" — đưa thẳng vào là không bao giờ gán được Ctrl+Alt+1, và tên enum
        // thô sẽ lòi ra trong thông báo lỗi cho người dùng đọc.
        var key = HotkeyKey.Normalize(e.Key.ToString()) ?? e.Key.ToString();
        _vm.TryChangeHotkey(new HotkeyCombo(mods, key));
        Sync();
    }

    private async Task BrowseFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Chọn thư mục lưu mặc định",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            FolderInput.Text = folders[0].Path.LocalPath;
    }
}

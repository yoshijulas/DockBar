using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DockBar.Models;
using DockBar.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using WinForms = System.Windows.Forms;

namespace DockBar;

public partial class TrayMenuWindow : Window, INotifyPropertyChanged
{
    private const double ScreenMargin = 8;
    private const double PopupGap = 6;
    private readonly DockConfig _config;
    private System.Drawing.Rectangle _anchorBounds;
    private bool _closeRequested;
    private Action? _afterCloseAction;

    public string VersionTag { get; }
    public bool CanUpdate => !PackageHelper.IsPackaged;
    public double UpdateButtonOpacity => CanUpdate ? 1.0 : 0.45;
    public string UpdateTooltip => CanUpdate
        ? LocalizationService.Get("Update_Menu")
        : LocalizationService.Get("Update_PackagedManaged");

    private Brush _menuBackgroundBrush = Brushes.Transparent;
    public Brush MenuBackgroundBrush
    {
        get => _menuBackgroundBrush;
        private set => SetBrush(ref _menuBackgroundBrush, value);
    }

    private Brush _menuBorderBrush = Brushes.Transparent;
    public Brush MenuBorderBrush
    {
        get => _menuBorderBrush;
        private set => SetBrush(ref _menuBorderBrush, value);
    }

    private Brush _menuTextBrush = Brushes.White;
    public Brush MenuTextBrush
    {
        get => _menuTextBrush;
        private set => SetBrush(ref _menuTextBrush, value);
    }

    private Brush _menuTextMutedBrush = Brushes.LightGray;
    public Brush MenuTextMutedBrush
    {
        get => _menuTextMutedBrush;
        private set => SetBrush(ref _menuTextMutedBrush, value);
    }

    private Brush _headerPanelBrush = Brushes.Transparent;
    public Brush HeaderPanelBrush
    {
        get => _headerPanelBrush;
        private set => SetBrush(ref _headerPanelBrush, value);
    }

    private Brush _headerBorderBrush = Brushes.Transparent;
    public Brush HeaderBorderBrush
    {
        get => _headerBorderBrush;
        private set => SetBrush(ref _headerBorderBrush, value);
    }

    private Brush _buttonHoverBackgroundBrush = Brushes.Transparent;
    public Brush ButtonHoverBackgroundBrush
    {
        get => _buttonHoverBackgroundBrush;
        private set => SetBrush(ref _buttonHoverBackgroundBrush, value);
    }

    private Brush _buttonHoverBorderBrush = Brushes.Transparent;
    public Brush ButtonHoverBorderBrush
    {
        get => _buttonHoverBorderBrush;
        private set => SetBrush(ref _buttonHoverBorderBrush, value);
    }

    private Brush _glyphHostBrush = Brushes.Transparent;
    public Brush GlyphHostBrush
    {
        get => _glyphHostBrush;
        private set => SetBrush(ref _glyphHostBrush, value);
    }

    private Brush _glyphBorderBrush = Brushes.Transparent;
    public Brush GlyphBorderBrush
    {
        get => _glyphBorderBrush;
        private set => SetBrush(ref _glyphBorderBrush, value);
    }

    private Brush _dividerBrush = Brushes.Transparent;
    public Brush DividerBrush
    {
        get => _dividerBrush;
        private set => SetBrush(ref _dividerBrush, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? PauseRequested;
    public event EventHandler? ToggleSideRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? UpdateRequested;
    public event EventHandler? ConfigFolderRequested;
    public event EventHandler? ExitRequested;

    public TrayMenuWindow(DockConfig? config = null)
    {
        _config = config ?? ConfigService.LoadConfig();
        VersionTag = $"v{UpdateService.GetCurrentVersion()}";
        ApplyTheme();

        InitializeComponent();
        DataContext = this;
        SourceInitialized += (_, _) =>
        {
            WindowSwitcherHelper.HideFromWindowSwitchers(this);
            GlassEffectHelper.Apply(this, _config.UseTransparency, _config.UseLightText);
        };
        Loaded += TrayMenuWindow_Loaded;
    }

    private bool SetBrush(ref Brush f, Brush v, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(f, v))
        {
            return false;
        }

        f = v;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void ApplyTheme()
    {
        var color = Color.FromRgb(_config.BackgroundR, _config.BackgroundG, _config.BackgroundB);
        var opacity = _config.UseTransparency
            ? Math.Clamp(_config.BackgroundOpacity, 0.0, 1.0)
            : 1.0;
        var alpha = (byte)Math.Clamp((int)Math.Round(opacity * 255), 0, 255);

        MenuBackgroundBrush = ThemeService.CreateFrozenBrush(Color.FromArgb(alpha, color.R, color.G, color.B));

        Color borderColor;
        if (_config.UseTransparency)
        {
            borderColor = _config.UseLightText
                ? Color.FromArgb(55, 255, 255, 255)
                : Color.FromArgb(45, 0, 0, 0);
        }
        else
        {
            borderColor = _config.UseLightText
                ? Color.FromArgb(60, 255, 255, 255)
                : Color.FromArgb(50, 0, 0, 0);
        }
        MenuBorderBrush = ThemeService.CreateFrozenBrush(borderColor);

        var textColor = _config.UseLightText
            ? Color.FromRgb(242, 242, 242)
            : Color.FromRgb(15, 15, 15);
        MenuTextBrush = ThemeService.CreateFrozenBrush(textColor);

        var textMutedColor = _config.UseLightText
            ? Color.FromArgb(170, 242, 242, 242)
            : Color.FromArgb(170, 15, 15, 15);
        MenuTextMutedBrush = ThemeService.CreateFrozenBrush(textMutedColor);

        var headerPanelColor = _config.UseLightText
            ? Color.FromArgb(30, 255, 255, 255)
            : Color.FromArgb(25, 0, 0, 0);
        HeaderPanelBrush = ThemeService.CreateFrozenBrush(headerPanelColor);

        var headerBorderColor = _config.UseLightText
            ? Color.FromArgb(40, 255, 255, 255)
            : Color.FromArgb(35, 0, 0, 0);
        HeaderBorderBrush = ThemeService.CreateFrozenBrush(headerBorderColor);

        var hoverBgColor = _config.UseLightText
          ? Color.FromArgb(40, 255, 255, 255)
          : Color.FromArgb(35, 0, 0, 0);
        ButtonHoverBackgroundBrush = ThemeService.CreateFrozenBrush(hoverBgColor);

        var hoverBorderColor = _config.UseLightText
          ? Color.FromArgb(75, 255, 255, 255)
          : Color.FromArgb(65, 0, 0, 0);
        ButtonHoverBorderBrush = ThemeService.CreateFrozenBrush(hoverBorderColor);

        var glyphHostColor = _config.UseLightText
          ? Color.FromArgb(25, 255, 255, 255)
          : Color.FromArgb(20, 0, 0, 0);
        GlyphHostBrush = ThemeService.CreateFrozenBrush(glyphHostColor);

        var glyphBorderColor = _config.UseLightText
          ? Color.FromArgb(45, 255, 255, 255)
          : Color.FromArgb(35, 0, 0, 0);
        GlyphBorderBrush = ThemeService.CreateFrozenBrush(glyphBorderColor);

        var divColor = _config.UseLightText
          ? Color.FromArgb(40, 255, 255, 255)
          : Color.FromArgb(35, 0, 0, 0);
        DividerBrush = ThemeService.CreateFrozenBrush(divColor);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void ShowAt(System.Drawing.Rectangle anchorBounds)
    {
        _anchorBounds = anchorBounds;
        Show();
    }

    public void RequestClose(Action? afterClose = null)
    {
        if (_closeRequested)
        {
            return;
        }

        _closeRequested = true;
        _afterCloseAction = afterClose;
        Close();
    }

    private void TrayMenuWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        Opacity = 1;
    }

    private void PositionWindow()
    {
        UpdateLayout();

        var screen = WinForms.Screen.FromRectangle(_anchorBounds);
        var area = screen.WorkingArea;
        var dpi = VisualTreeHelper.GetDpi(this);
        var scaleX = dpi.DpiScaleX <= 0 ? 1.0 : dpi.DpiScaleX;
        var scaleY = dpi.DpiScaleY <= 0 ? 1.0 : dpi.DpiScaleY;
        var widthPx = Math.Max(1, (int)Math.Ceiling(ActualWidth * scaleX));
        var heightPx = Math.Max(1, (int)Math.Ceiling(ActualHeight * scaleY));

        var anchorLeft = _anchorBounds.Left;
        var anchorTop = _anchorBounds.Top;
        var anchorRight = _anchorBounds.Right;
        var anchorBottom = _anchorBounds.Bottom;
        var anchorCenterX = anchorLeft + (_anchorBounds.Width / 2.0);
        var anchorCenterY = anchorTop + (_anchorBounds.Height / 2.0);

        var workLeft = area.Left;
        var workTop = area.Top;
        var workRight = area.Right;
        var workBottom = area.Bottom;

        var minLeft = workLeft + (int)Math.Round(ScreenMargin * scaleX);
        var maxLeft = workRight - widthPx - (int)Math.Round(ScreenMargin * scaleX);
        var minTop = workTop + (int)Math.Round(ScreenMargin * scaleY);
        var maxTop = workBottom - heightPx - (int)Math.Round(ScreenMargin * scaleY);
        var gapX = (int)Math.Round(PopupGap * scaleX);
        var gapY = (int)Math.Round(PopupGap * scaleY);

        var preferLeft = (workRight - anchorCenterX) < (anchorCenterX - workLeft);
        var preferUp = (workBottom - anchorCenterY) < (anchorCenterY - workTop);

        var leftCandidate = preferLeft
            ? anchorRight - widthPx
            : anchorLeft;
        var rightCandidate = preferLeft
            ? anchorLeft
            : anchorRight - widthPx;

        var topCandidate = preferUp
            ? anchorTop - heightPx - gapY
            : anchorBottom + gapY;
        var bottomCandidate = preferUp
            ? anchorBottom + gapY
            : anchorTop - heightPx - gapY;

        var leftPx = SelectAxisPosition(leftCandidate, rightCandidate, minLeft, maxLeft);
        var topPx = SelectAxisPosition(topCandidate, bottomCandidate, minTop, maxTop);
        ApplyPixelPosition(leftPx, topPx);
    }

    private void ApplyPixelPosition(int leftPx, int topPx)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(hwnd, IntPtr.Zero, leftPx, topPx, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private static int SelectAxisPosition(int primary, int secondary, int min, int max)
    {
        if (max < min)
        {
            return min;
        }

        if (primary >= min && primary <= max)
        {
            return primary;
        }

        if (secondary >= min && secondary <= max)
        {
            return secondary;
        }

        return Math.Max(min, Math.Min(primary, max));
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        RequestClose();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RequestClose();
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        CloseAndRaise(PauseRequested);
    }

    private void ToggleSide_Click(object sender, RoutedEventArgs e)
    {
        CloseAndRaise(ToggleSideRequested);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        CloseAndRaise(SettingsRequested);
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        if (!CanUpdate)
        {
            return;
        }
        CloseAndRaise(UpdateRequested);
    }

    private void ConfigFolder_Click(object sender, RoutedEventArgs e)
    {
        CloseAndRaise(ConfigFolderRequested);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        CloseAndRaise(ExitRequested);
    }

    private void CloseAndRaise(EventHandler? handler)
    {
        RequestClose(handler == null ? null : () => handler.Invoke(this, EventArgs.Empty));
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        var action = _afterCloseAction;
        _afterCloseAction = null;

        if (action != null)
        {
            Dispatcher.BeginInvoke(action);
        }
    }

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}

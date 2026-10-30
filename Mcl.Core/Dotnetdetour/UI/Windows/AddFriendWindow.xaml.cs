using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mcl.Core.Dotnetdetour.UI.Themes;
using WPFLauncher.Common;
using WPFLauncher.ViewModel.Chat;

namespace Mcl.Core.Dotnetdetour.UI.Windows;

public partial class AddFriendWindow : BaseWindow
{
    private static AddFriendWindow _instance;

    public AddFriendWindow()
    {
        InitializeComponent();
        FluentTheme.Apply(this);
        eb = azc.d;
        var viewModel = new ne(this);
        DataContext = viewModel;
        FriendPanel.Attach(viewModel);
        Closed += OnClosed;
    }

    public static void ShowFor(Window owner)
    {
        if (_instance != null &&
            Application.Current.Windows.OfType<AddFriendWindow>().Any(window => ReferenceEquals(window, _instance)))
        {
            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.d();
            _instance.Activate();
            return;
        }

        var window = new AddFriendWindow();
        if (owner != null && owner.IsVisible)
        {
            window.Owner = owner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        _instance = window;
        try
        {
            window.d();
            window.Activate();
        }
        catch
        {
            if (ReferenceEquals(_instance, window)) _instance = null;
            try
            {
                window.c();
            }
            catch
            {
                // Preserve the original startup exception.
            }

            throw;
        }
    }

    private void OnClosed(object sender, EventArgs e)
    {
        if (ReferenceEquals(_instance, this)) _instance = null;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        c();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        c();
    }
}

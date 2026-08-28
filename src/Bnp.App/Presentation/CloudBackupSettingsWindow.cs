using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bnp.Localization;
using Bnp.Services.CloudBackup;

namespace Bnp.Presentation;

internal sealed class CloudBackupSettingsWindow : Window
{
    private readonly CloudBackupService _backupService;
    private readonly CloudBackupCoordinator _backupCoordinator;
    private readonly EditorCopy _copy;
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _connectButton;
    private readonly Button _disconnectButton;

    public CloudBackupSettingsWindow(
        CloudBackupService backupService,
        CloudBackupCoordinator backupCoordinator,
        EditorCopy copy)
    {
        _backupService = backupService;
        _backupCoordinator = backupCoordinator;
        _copy = copy;

        var state = backupService.GetConnectionState();
        _connectButton = new Button { Content = copy.ConnectAndBackup };
        _connectButton.Click += OnConnectClick;
        _disconnectButton = new Button
        {
            Content = copy.Disconnect,
            IsVisible = state.IsConnected
        };
        _disconnectButton.Click += OnDisconnectClick;
        _status.Text = state.IsConnected ? copy.CloudConnected : copy.CloudDisconnected;

        Title = copy.CloudBackups;
        Width = 420;
        Height = 240;
        MinWidth = 360;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = BuildContent();
    }

    private Grid BuildContent()
    {
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { _disconnectButton, _connectButton }
        };
        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*"),
            RowSpacing = 8,
            Margin = new Thickness(20),
            Children =
            {
                new TextBlock
                {
                    Text = _copy.CloudBackups,
                    FontSize = 18,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock { Text = _copy.CloudService, FontSize = 12 },
                new TextBlock { Text = _copy.Dropbox },
                _status,
                actions
            }
        };
        Grid.SetRow(content.Children[1], 1);
        Grid.SetRow(content.Children[2], 2);
        Grid.SetRow(_status, 3);
        Grid.SetRow(actions, 4);
        return content;
    }

    private async void OnConnectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        SetBusy(true);
        _status.Text = _copy.CloudConnecting;
        try
        {
            await _backupCoordinator.ConnectAsync();
            _disconnectButton.IsVisible = true;
            _status.Text = _copy.CloudConnected;
        }
        catch (Exception exception)
        {
            _status.Text = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                _copy.CloudBackupFailed,
                exception.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnDisconnectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        _backupService.Disconnect();
        _disconnectButton.IsVisible = false;
        _status.Text = _copy.CloudDisconnected;
    }

    private void SetBusy(bool isBusy)
    {
        _connectButton.IsEnabled = !isBusy;
        _disconnectButton.IsEnabled = !isBusy;
    }
}
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TransAnyWhereApp.Helpers;
using TransAnyWhereApp.Models;
using TransAnyWhereApp.Services.Network;
using TransAnyWhereApp.Services.QRCode;

namespace TransAnyWhereApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ITransferService _transferService;
    private readonly IQRCodeService _qrCodeService;

    public MainViewModel(ITransferService transferService, IQRCodeService qrCodeService)
    {
        _transferService = transferService;
        _qrCodeService = qrCodeService;
        _transferService.DownloadPath = DownloadPath;
        InitializeEvents();
    }

    #region --- 响应式属性 ---

    [ObservableProperty] private ObservableCollection<TransferTask> _files = new();
    [ObservableProperty] private ObservableCollection<TransferTask> _receivedFiles = new();
    [ObservableProperty] private ObservableCollection<DeviceItem> _connectedClients = new();

    [ObservableProperty] private string _downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    [ObservableProperty] private string _connectionString = "127.0.0.1";
    [ObservableProperty] private Bitmap? _qrCodeImage;
    [ObservableProperty] private bool _isQrVisible;
    [ObservableProperty] private bool _isConfirmVisible;
    [ObservableProperty] private string _pendingDeviceName = "";
    [ObservableProperty] private bool _isShowingSending = true;
    [ObservableProperty] private string _alertMessage = "";
    [ObservableProperty] private bool _isAlertVisible;
    [ObservableProperty] private ObservableCollection<string> _ipAddresses = new();
    [ObservableProperty] private string? _selectedIp;

    public string ClientCountText => string.Format("{{M_ClientCount}}".Culture(), ConnectedClients.Count);
    public string ConnectionRequestText => string.Format("{{U_Msg_DeviceRequest}}".Culture(), PendingDeviceName);
    public bool ShowEmptyHint => Files.Count == 0;
    public bool ShowReceivedEmptyHint => ReceivedFiles.Count == 0;
    public double SendingOpacity => IsShowingSending ? 1.0 : 0.4;
    public double ReceivedOpacity => IsShowingSending ? 0.4 : 1.0;

    #endregion

    #region --- events ---

    private void InitializeEvents()
    {
        _transferService.OnConnectionRequested += name => RunOnUI(() =>
        {
            PendingDeviceName = name;
            IsConfirmVisible = true;
            OnPropertyChanged(nameof(ConnectionRequestText));
        });
        _transferService.OnDeviceConnected += name => RunOnUI(() =>
        {
            if (ConnectedClients.All(d => d.DeviceName != name))
            {
                ConnectedClients.Add(new DeviceItem { DeviceName = name });
                OnPropertyChanged(nameof(ClientCountText));
            }
        });
        _transferService.OnFileReceived += task => RunOnUI(() =>
        {
            ReceivedFiles.Insert(0, task);
            OnPropertyChanged(nameof(ShowReceivedEmptyHint));
        });
        _transferService.OnFileStatusChanged += (name, status) => RunOnUI(() =>
        {
            var task = ReceivedFiles.FirstOrDefault(t => t.FileName == name);
            if (task != null) task.Status = status;
        });
        _transferService.OnDownloadProgressChanged += (deviceName, currentSize) => RunOnUI(() =>
        {
            var task = ReceivedFiles.FirstOrDefault(t => t.DeviceName == deviceName && t.Status == TransferStatus.Transferring);
            if (task != null) task.TransferredSize = currentSize;
        });
        _transferService.OnDeviceDisconnected += name => RunOnUI(() =>
        {
            var device = ConnectedClients.FirstOrDefault(d => d.DeviceName == name);
            if (device != null) { ConnectedClients.Remove(device); OnPropertyChanged(nameof(ClientCountText)); }
            ShowMessage(string.Format("{{M_DeviceOffline}}".Culture(), name));
        });
        _transferService.OnMessageLogged += ShowMessage;
    }
    private void RunOnUI(Action action) => Avalonia.Threading.Dispatcher.UIThread.Post(action);
    partial void OnDownloadPathChanged(string value) => _transferService.DownloadPath = value;
    partial void OnIsShowingSendingChanged(bool value) { OnPropertyChanged(nameof(SendingOpacity)); OnPropertyChanged(nameof(ReceivedOpacity)); }

    partial void OnSelectedIpChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            ConnectionString = $"http://{value}";

            QrCodeImage = _qrCodeService.GenerateQrCode(ConnectionString);
        }
    }

    #endregion

    #region --- RelayCommands ---
    [RelayCommand]
    private async Task CopyConnectionString()
    {
        if (string.IsNullOrEmpty(ConnectionString)) return;

        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(
            Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null);

        if (topLevel?.Clipboard != null)
        {
            await topLevel.Clipboard.SetTextAsync(ConnectionString);
            ShowMessage("{{M_Notify_Copied}}".Culture());
        }
    }

    [RelayCommand] private void ShowSendingList() => IsShowingSending = true;
    [RelayCommand] private void ShowReceivedList() => IsShowingSending = false;

    [RelayCommand]
    private void AcceptConnection()
    {
        if (string.IsNullOrEmpty(PendingDeviceName)) return;
        _transferService.SetConnectionResult(PendingDeviceName, true);
        IsConfirmVisible = false;
        ShowMessage(string.Format("{{M_EstablishingConn}}".Culture(), PendingDeviceName));
    }

    [RelayCommand]
    private void RejectConnection()
    {
        if (string.IsNullOrEmpty(PendingDeviceName)) return;
        _transferService.SetConnectionResult(PendingDeviceName, false);
        IsConfirmVisible = false;
    }

    [RelayCommand]
    private void ShowQr()
    {
        try
        {

            var ipInfos = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up &&
                            i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(i => i.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => new
                    {
                        Ip = a.Address.ToString(),
                        Type = i.NetworkInterfaceType,
                        Desc = i.Description.ToLower()
                    }))
                .ToList();

            var sortedIps = ipInfos
                //.Where(x => !x.Desc.Contains("virtual") && !x.Desc.Contains("tun") && !x.Desc.Contains("tap"))
                .OrderByDescending(x => x.Type == NetworkInterfaceType.Wireless80211) 
                .ThenByDescending(x => x.Type == NetworkInterfaceType.Ethernet)      
                .Select(x => x.Ip)
                .ToList();

            IpAddresses = new ObservableCollection<string>(sortedIps);

            SelectedIp = sortedIps.FirstOrDefault();

            if (SelectedIp != null)
            {
                int port = 80;
                ConnectionString = $"http://{SelectedIp}";

                QrCodeImage = _qrCodeService.GenerateQrCode(ConnectionString);

                _transferService.StartServer(port);

                IsQrVisible = true;
                _transferService.IsJoinAllowed = true;
            }
            else { ShowMessage("{{M_Err_NoIP}}".Culture()); }
        }
        catch (Exception ex) { ShowMessage(string.Format("{{M_Err_GetIPFail}}".Culture(), ex.Message)); }
    }

    [RelayCommand] private void CloseQr() { IsQrVisible = false; _transferService.IsJoinAllowed = false; }

    [RelayCommand]
    private void DisconnectClient(DeviceItem client)
    {
        if (client != null) _transferService.DisconnectDevice(client.DeviceName);
    }

    [RelayCommand]
    private void SelectAllDevices()
    {
        bool targetState = ConnectedClients.Any(d => !d.IsSelected);
        foreach (var client in ConnectedClients) client.IsSelected = targetState;
    }

    [RelayCommand]
    private void StartSync()
    {
        var selectedDevices = ConnectedClients.Where(d => d.IsSelected).ToList();
        var pendingTasks = Files.ToList(); //.Where(f => f.Status == TransferStatus.Pending)
        if (!selectedDevices.Any() || !pendingTasks.Any()) return;
        foreach (var device in selectedDevices)
        {
            var fileLinks = pendingTasks.Select(GenerateFileLink).ToList();
            _transferService.SendWsMessage(device.DeviceName, new WsProtocolModel { type = "NEW_FILES", data = fileLinks });
        }
        pendingTasks.ForEach(t => t.Status = TransferStatus.Completed);
        ShowMessage(string.Format("{{M_SyncDone}}".Culture(), pendingTasks.Count));
    }

    [RelayCommand]
    private async Task ToggleFileStatus(TransferTask file)
    {
        if (file == null) return;
        var selectedDevices = ConnectedClients.Where(d => d.IsSelected).ToList();
        if (!selectedDevices.Any()) { ShowMessage("{{M_Warn_SelectDevice}}".Culture()); return; }
        file.Status = TransferStatus.Transferring;
        foreach (var device in selectedDevices)
        {
            _transferService.SendWsMessage(device.DeviceName, new WsProtocolModel
            {
                type = "NEW_FILES",
                data = new[] { GenerateFileLink(file) }
            });
        }
        file.Status = TransferStatus.Completed;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SelectFiles()
    {
        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
        if (topLevel == null) return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "{{M_Title_SelectFiles}}".Culture(), AllowMultiple = true });
        if (files?.Count > 0)
        {
            foreach (var file in files) AddFile(file.Path.LocalPath);
            ShowMessage(string.Format("{{M_FileAdded}}".Culture(), files.Count));
        }
    }

    [RelayCommand]
    private void RemoveFile(TransferTask file)
    {
        if (file == null) return;
        Files.Remove(file);
        OnPropertyChanged(nameof(ShowEmptyHint));
    }

    [RelayCommand]
    private void RemoveReceivedRecord(TransferTask task)
    {
        if (task == null) return;
        ReceivedFiles.Remove(task);
        OnPropertyChanged(nameof(ShowReceivedEmptyHint));
    }

    [RelayCommand]
    private void ClearDone()
    {
        var toRemoveSend = Files.Where(f => f.Status == TransferStatus.Completed).ToList();
        foreach (var t in toRemoveSend) Files.Remove(t);
        var toRemoveRec = ReceivedFiles.Where(f => f.Status == TransferStatus.Completed).ToList();
        foreach (var t in toRemoveRec) ReceivedFiles.Remove(t);
        OnPropertyChanged(nameof(ShowEmptyHint));
        OnPropertyChanged(nameof(ShowReceivedEmptyHint));
    }

    [RelayCommand]
    private async Task SelectDownloadFolder()
    {
        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(
            Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null);

        if (topLevel == null) return;

        var startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(DownloadPath);

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "{{M_Title_SelectFolder}}".Culture(),
            AllowMultiple = false,
            SuggestedStartLocation = startLocation
        });

        if (folders.Count > 0)
        {
            DownloadPath = folders[0].Path.LocalPath;
            ShowMessage("{{M_Notify_PathUpdated}}".Culture());
        }
    }

    [RelayCommand]
    private void OpenDownloadFolder()
    {
        try
        {
            if (!Directory.Exists(DownloadPath)) Directory.CreateDirectory(DownloadPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", DownloadPath);
            }
            else
            {
                var cmd = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "open" : "xdg-open";
                Process.Start(new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = $"\"{DownloadPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
        }
        catch (Exception ex)
        {
            ShowMessage(string.Format("{{M_Err_OpenFolderFail}}".Culture(), ex.Message));
        }
    }

    #endregion

    #region --- Utilities ---

    private FileLinkModel GenerateFileLink(TransferTask task)
    {
        string base64Path = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(task.FilePath));

        string safeUrlParam = Uri.EscapeDataString(base64Path);

        return new FileLinkModel
        {
            name = task.FileName,
            size = task.TotalSize,
            sizeText = FormatFileSize(task.TotalSize),
            url = $"{ConnectionString.TrimEnd('/')}/download?file={safeUrlParam}"
        };
    }

    public void AddFile(string path)
    {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            Files.Add(new TransferTask { FileName = fileInfo.Name, FilePath = fileInfo.FullName, TotalSize = fileInfo.Length, Status = TransferStatus.Pending });
            OnPropertyChanged(nameof(ShowEmptyHint));
        }
    }

    public async void ShowMessage(string msg)
    {
        IsAlertVisible = false;
        AlertMessage = msg;
        IsAlertVisible = true;
        await Task.Delay(3000);
        IsAlertVisible = false;
    }

    private string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int unitIndex = 0;
        while (len >= 1024 && unitIndex < units.Length - 1) { unitIndex++; len /= 1024; }
        return $"{len:F2} {units[unitIndex]}";
    }

    public void OnClosing() => _transferService.StopServer();

    #endregion
}
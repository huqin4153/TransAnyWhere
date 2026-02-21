using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TransAnyWhereApp.Helpers;

namespace TransAnyWhereApp.Models;

public enum TransferStatus { Pending, Transferring, Completed, Error, Paused, Cancelled, Failed }

public partial class TransferTask : ObservableObject
{
    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private string _fileName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private string _filePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private long _totalSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private long _transferredSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusIcon))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private TransferStatus _status = TransferStatus.Pending;

    public string StatusText => $"{{{{S_Status_{Status}}}}}".Culture();

    public double Progress => TotalSize == 0 ? 0 : (double)TransferredSize / TotalSize * 100;

    [JsonSerializable(typeof(TransferTask))]
    [JsonSerializable(typeof(List<TransferTask>))]
    internal partial class TransferTaskContext : JsonSerializerContext { }

    public string StatusIcon => Status switch
    {
        TransferStatus.Transferring => "⏸",
        _ => "▶"
    };
}
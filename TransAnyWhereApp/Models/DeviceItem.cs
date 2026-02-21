using CommunityToolkit.Mvvm.ComponentModel;

public partial class DeviceItem : ObservableObject
{
    [ObservableProperty] private string _deviceName = string.Empty;
    [ObservableProperty] private bool _isSelected = true; 
}

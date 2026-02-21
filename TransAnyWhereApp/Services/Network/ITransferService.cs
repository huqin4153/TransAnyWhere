using System;
using TransAnyWhereApp.Models;

namespace TransAnyWhereApp.Services.Network
{
    public interface ITransferService
    {
        bool IsServerRunning { get; }
        string DownloadPath { get; set; }
        bool IsJoinAllowed { get; set; }

        void StartServer(int port);
        void StopServer();

        void SetConnectionResult(string deviceName, bool accepted);
        void DisconnectDevice(string deviceName);
        void SendWsMessage(string deviceName, WsProtocolModel message);

        event Action<string>? OnMessageLogged;
        event Action<string>? OnDeviceConnected;
        event Action<string>? OnDeviceDisconnected;
        event Action<TransferTask>? OnFileReceived;
        event Action<string, TransferStatus>? OnFileStatusChanged;
        event Action<string>? OnConnectionRequested;
        event Action? OnAllDevicesDisconnected;
        event Action<string, long>? OnDownloadProgressChanged;

    }
}

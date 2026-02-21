using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TransAnyWhereApp.Services.Device
{
    public interface IDeviceManager
    {
        bool IsAuthenticated(string deviceName);
        bool IsClientConnected(string deviceName);
        IEnumerable<string> GetActiveDeviceNames();
        void RevokeAuthentication(string deviceName);
        void RegisterPendingRequest(string deviceName, TaskCompletionSource<bool> tcs);
        bool TryReleasePendingRequest(string deviceName, bool result);

        void AddOrUpdateClient(string deviceName, TcpClient client);
        TcpClient? RemoveClient(string deviceName);
        void ClearAll();

        TcpClient? GetClient(string deviceName); 
    }
}

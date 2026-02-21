using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TransAnyWhereApp.Services.Device
{
    public class DeviceManager : IDeviceManager
    {
        private readonly ConcurrentDictionary<string, TcpClient> _activeClients = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingRequests = new();
        private readonly HashSet<string> _authenticatedDevices = new();

        public bool IsAuthenticated(string deviceName) => _authenticatedDevices.Contains(deviceName);

        public bool IsClientConnected(string deviceName) => _activeClients.ContainsKey(deviceName);

        public void RevokeAuthentication(string deviceName)
        {
            _authenticatedDevices.Remove(deviceName);
        }

        public void RegisterPendingRequest(string deviceName, TaskCompletionSource<bool> tcs)
        {
            _pendingRequests.TryAdd(deviceName, tcs);
        }

        public bool TryReleasePendingRequest(string deviceName, bool result)
        {
            if (_pendingRequests.TryRemove(deviceName, out var tcs))
            {
                tcs.TrySetResult(result);
                if (result) _authenticatedDevices.Add(deviceName);
                return true;
            }
            return false;
        }

        public void AddOrUpdateClient(string deviceName, TcpClient client)
        {
            _activeClients.AddOrUpdate(deviceName, client, (key, oldClient) =>
            {
                try { oldClient.Close(); } catch { }
                return client;
            });
        }

        public TcpClient? RemoveClient(string deviceName)
        {
            if (_activeClients.TryRemove(deviceName, out var client))
            {
                return client;
            }
            return null;
        }

        public void ClearAll()
        {
            foreach (var tcs in _pendingRequests.Values)
            {
                tcs.TrySetCanceled();
            }
            _pendingRequests.Clear();

            foreach (var client in _activeClients.Values)
            {
                try
                {
                    var stream = client.GetStream();
                    byte[] closeFrame = new byte[] { 0x88, 0x00 };
                    stream.Write(closeFrame, 0, closeFrame.Length);
                    client.GetStream().Close();
                    client.Close();
                    client.Dispose();
                }
                catch { }
            }
            _activeClients.Clear();

            _authenticatedDevices.Clear();
        }

        public IEnumerable<string> GetActiveDeviceNames() => _activeClients.Keys;

        public TcpClient? GetClient(string deviceName)
        {
            if (_activeClients.TryGetValue(deviceName, out var client))
            {
                return client;
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TransAnyWhereApp.Helpers;
using TransAnyWhereApp.Models;
using TransAnyWhereApp.Services.Device;
using TransAnyWhereApp.Services.Storage;

namespace TransAnyWhereApp.Services.Network
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class TransferService : ITransferService
    {
        public event Action<string>? OnMessageLogged;
        public event Action<string>? OnDeviceConnected;
        public event Action<string>? OnDeviceDisconnected;
        public event Action<TransferTask>? OnFileReceived;
        public event Action<string, TransferStatus>? OnFileStatusChanged;
        public event Action? OnAllDevicesDisconnected;
        public event Action<string>? OnConnectionRequested;
        public event Action<string, long>? OnDownloadProgressChanged;

        private readonly IHtmlProvider _htmlProvider;
        private readonly IDeviceManager _deviceManager;
        private readonly Func<IFileReceiver> _fileReceiverFactory;
        private TcpListener? _listener;
        private bool _isServerRunning;
        public bool IsJoinAllowed { get; set; } = false;
        private CancellationTokenSource? _cts;
        public bool IsServerRunning => _isServerRunning;

        public string DownloadPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TransAnyWhere");

        public TransferService(IHtmlProvider htmlProvider, IDeviceManager deviceManager, Func<IFileReceiver> fileReceiverFactory)
        {
            _htmlProvider = htmlProvider;
            _deviceManager = deviceManager;
            _fileReceiverFactory = fileReceiverFactory;
        }

        public void SetConnectionResult(string deviceName, bool result)
        {
            _deviceManager.TryReleasePendingRequest(deviceName, result);
        }

        public void DisconnectDevice(string deviceName)
        {
            var client = _deviceManager.RemoveClient(deviceName);
            _deviceManager.RevokeAuthentication(deviceName);

            if (client != null)
            {
                try
                {
                    if (client.Connected)
                    {
                        var stream = client.GetStream();
                        byte[] closeFrame = new byte[] { 0x88, 0x00 };
                        stream.Write(closeFrame, 0, closeFrame.Length);

                        client.GetStream().Close();
                        client.Close();
                    }
                }
                catch
                {
                }
                finally
                {
                    client.Dispose();
                    OnDeviceDisconnected?.Invoke(deviceName);
                }
            }
        }

        public void StartServer(int port)
        {
            if (_isServerRunning) return;

            _listener = new TcpListener(IPAddress.Any, port);
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    _listener.Start();
                    _isServerRunning = true;
                    OnMessageLogged?.Invoke(string.Format("{{L_Log_Start}}".Culture(), port));

                    while (_isServerRunning)
                    {
                        var tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = HandleRawRequest(tcpClient, port, _cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _isServerRunning = false;
                    OnMessageLogged?.Invoke(string.Format("{{L_Log_Error}}".Culture(), ex.Message));
                }
                finally
                {
                    _isServerRunning = false;
                }
            }, _cts.Token);
        }

        public void StopServer()
        {
            if (!_isServerRunning) return;

            _isServerRunning = false;
            _cts?.Cancel();
            _listener?.Stop();

            _deviceManager.ClearAll();

            OnAllDevicesDisconnected?.Invoke();
            OnMessageLogged?.Invoke("{{L_Log_Stop}}".Culture());

            _cts?.Dispose();
            _cts = null;
        }

        private async Task HandleRawRequest(TcpClient client, int port, CancellationToken token)
        {
            var stream = client.GetStream();
            string? clientNameForCleanup = null;
            var ip = ((IPEndPoint)client.Client.LocalEndPoint!).Address.ToString();
            try
            {
                byte[] buffer = new byte[8192];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (read <= 0) return;

                string request = Encoding.UTF8.GetString(buffer, 0, read);

                if (request.Contains("Upgrade: websocket"))
                {
                    string clientName = WebSocketHelper.ParseDeviceName(request);
                    clientNameForCleanup = clientName;

                    bool accepted = await ValidateDeviceAccess(clientName, token);
                    if (!accepted) return;

                    string? clientKey = WebSocketHelper.ParseWebSocketKey(request);
                    if (clientKey == null) return;
                    string acceptKey = WebSocketHelper.GetAcceptKey(clientKey);

                    string response = $"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {acceptKey}\r\n\r\n";
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(response), token);

                    _deviceManager.AddOrUpdateClient(clientName, client);
                    OnDeviceConnected?.Invoke(clientName);

                    await HandleWebSocketData(stream, clientName, token);
                }
                else if (request.Contains("GET /download"))
                {
                    int queryIndex = request.IndexOf('?');
                    if (queryIndex == -1) return;

                    string firstLine = request.Split('\r', '\n')[0];

                    string rawUrl = firstLine.Replace("GET ", "").Replace(" HTTP/1.1", "").Trim();

                    string base64Name = "";
                    try
                    {
                        int startIndex = request.IndexOf("file=");
                        if (startIndex != -1)
                        {
                            startIndex += 5;
                            int endIndex = request.IndexOf(" ", startIndex);
                            if (endIndex != -1)
                            {
                                base64Name = request.Substring(startIndex, endIndex - startIndex);
                                base64Name = Uri.UnescapeDataString(base64Name);
                            }
                        }
                    }
                    catch { return; }

                    if (string.IsNullOrEmpty(base64Name)) return;

                    string filePath = "";
                    try
                    {
                        byte[] data = Convert.FromBase64String(base64Name);
                        filePath = Encoding.UTF8.GetString(data);
                    }

                    catch { return; }

                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);

                        string fileNameOnly = Path.GetFileName(filePath);

                        string responseHeader =
                            "HTTP/1.1 200 OK\r\n" +
                            "Server: TransAnyWhere\r\n" +
                            "Content-Type: application/octet-stream\r\n" +
                            $"Content-Length: {fileInfo.Length}\r\n" +
                            $"Content-Disposition: attachment; filename=\"{Uri.EscapeDataString(fileNameOnly)}\"\r\n" +
                            "Access-Control-Allow-Origin: *\r\n" +
                            "Connection: close\r\n\r\n";

                        byte[] headerBytes = Encoding.UTF8.GetBytes(responseHeader);
                        await stream.WriteAsync(headerBytes, 0, headerBytes.Length, token);

                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            await fs.CopyToAsync(stream, 1024 * 64, token);
                        }

                        await stream.FlushAsync(token);
                        await Task.Delay(500, token);
                        return;
                    }
                    else
                    {
                        byte[] nf = Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n");
                        await stream.WriteAsync(nf, 0, nf.Length, token);
                    }
                }
                else if (request.Contains("GET /"))
                {
                    if (request.Contains("favicon.ico")) return;

                    string html = _htmlProvider.GetMobilePage(ip, port.ToString());
                    string fullResponse = _htmlProvider.BuildHttpResponse(html);
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(fullResponse), token);
                }
            }
            finally
            {
                CleanupConnection(clientNameForCleanup, client, stream);
            }
        }

        private void CleanupConnection(string? clientName, TcpClient client, NetworkStream? stream)
        {
            try
            {
                if (clientName != null)
                {
                    _deviceManager.TryReleasePendingRequest(clientName, false);
                    var removedClient = _deviceManager.RemoveClient(clientName);
                    if (removedClient != null)
                    {
                        OnDeviceDisconnected?.Invoke(clientName);
                    }
                }

                stream?.Close();
                client.Close();
            }
            catch (ObjectDisposedException) { }
            finally
            {
                client.Dispose();
            }
        }

        private async Task<bool> ValidateDeviceAccess(string clientName, CancellationToken token)
        {
            if (_deviceManager.IsAuthenticated(clientName)) return true;

            if (!IsJoinAllowed) return false;

            var tcs = new TaskCompletionSource<bool>();
            _deviceManager.RegisterPendingRequest(clientName, tcs);
            OnConnectionRequested?.Invoke(clientName);

            try
            {
                return await tcs.Task.WaitAsync(token);
            }
            catch
            {
                return false;
            }
        }

        private async Task HandleWebSocketData(NetworkStream stream, string deviceName, CancellationToken token)
        {
            bool isCurrentlyTransferring = false;
            using var fileReceiver = _fileReceiverFactory();
            string? lastKnownFileName = null;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var (opcode, isFinal, payload) = await WebSocketHelper.ReadFrameAsync(stream, token);

                    if (opcode == 8) break;

                    if (opcode == 1)
                    {
                        string msg = Encoding.UTF8.GetString(payload);

                        if (msg.StartsWith("NAME:"))
                        {
                            string content = msg.Substring(5);
                            string[] parts = content.Split('|');
                            string fileName = parts[0];
                            long totalSize = parts.Length > 1 ? long.Parse(parts[1]) : 0;

                            if (isCurrentlyTransferring)
                            {
                                await fileReceiver.FinishAsync();
                            }

                            lastKnownFileName = fileName;
                            fileReceiver.PrepareFile(fileName, DownloadPath);
                            isCurrentlyTransferring = true;

                            OnFileReceived?.Invoke(new TransferTask
                            {
                                FileName = fileName,
                                DeviceName = deviceName,
                                TotalSize = totalSize,
                                Status = TransferStatus.Transferring
                            });
                            OnMessageLogged?.Invoke(string.Format("{{L_Log_FilePrep}}".Culture(), fileName));

                            await WebSocketHelper.SendTextAsync(stream, "ACK", token);
                        }
                        else if (msg == "DONE")
                        {
                            if (isCurrentlyTransferring && !string.IsNullOrEmpty(fileReceiver.CurrentFileName))
                            {
                                string finishedFile = fileReceiver.CurrentFileName;

                                await fileReceiver.FinishAsync();
                                isCurrentlyTransferring = false;

                                OnFileStatusChanged?.Invoke(finishedFile, TransferStatus.Completed);
                                OnMessageLogged?.Invoke(string.Format("{{L_Log_FileDone}}".Culture(), finishedFile));

                                await WebSocketHelper.SendTextAsync(stream, "TRANSFER_COMPLETE", token);
                            }
                        }
                        else if (msg == "CANCEL")
                        {
                            string canceledFile = fileReceiver.CurrentFileName ?? "{{L_UnknownFile}}".Culture();
                            fileReceiver.Abort();
                            OnFileStatusChanged?.Invoke(canceledFile, TransferStatus.Cancelled);
                            lastKnownFileName = null;
                        }
                    }
                    else if (opcode == 2)
                    {
                        var combinedPayload = new List<byte>();
                        combinedPayload.AddRange(payload);

                        bool currentIsFinal = isFinal;
                        while (!currentIsFinal)
                        {
                            var nextFrame = await WebSocketHelper.ReadFrameAsync(stream, token);
                            combinedPayload.AddRange(nextFrame.Payload);
                            currentIsFinal = nextFrame.IsFinal;
                        }

                        byte[] finalData = combinedPayload.ToArray();

                        if (isCurrentlyTransferring && finalData.Length > 0)
                        {
                            await fileReceiver.WriteChunkAsync(finalData, token);

                            OnDownloadProgressChanged?.Invoke(deviceName, fileReceiver.CurrentSize);

                            await WebSocketHelper.SendTextAsync(stream, "ACK", token);
                        }
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                isCurrentlyTransferring = false;
                string errorFileName = fileReceiver.CurrentFileName ?? lastKnownFileName ?? "{{L_UnknownFile}}".Culture();
                fileReceiver.Abort();
                OnFileStatusChanged?.Invoke(errorFileName, TransferStatus.Failed);
                OnMessageLogged?.Invoke(string.Format("{{L_Log_TransInt}}".Culture(), deviceName, ex.Message));

                if (ex is IOException || ex is SocketException)
                {
                    OnMessageLogged?.Invoke("{{L_Log_NetLost}}".Culture());
                }
            }
        }

        public void SendWsMessage(string deviceName, WsProtocolModel message)
        {
            var client = _deviceManager.GetClient(deviceName);
            if (client == null || !client.Connected)
            {
                OnMessageLogged?.Invoke(string.Format("{{L_Log_NotOnline}}".Culture(), deviceName));
                return;
            }

            try
            {
                IEnumerable<FileLinkModel>? dataList = message.data as IEnumerable<FileLinkModel>;
                var itemsJson = dataList != null
                    ? string.Join(",", dataList.Select(d => $"{{\"name\":\"{d.name}\",\"size\":{d.size},\"sizeText\":\"{d.sizeText}\",\"url\":\"{d.url}\"}}"))
                    : "[]";

                string jsonPayload = $"{{\"type\":\"{message.type}\",\"data\":[{itemsJson}]}}";

                Task.Run(async () =>
                {
                    try
                    {
                        var stream = client.GetStream();

                        await WebSocketHelper.SendTextAsync(stream, jsonPayload, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        OnMessageLogged?.Invoke(string.Format("{{L_Log_PushFail}}".Culture(), deviceName, ex.Message));
                    }
                });
            }
            catch
            {
            }
        }
    }
}
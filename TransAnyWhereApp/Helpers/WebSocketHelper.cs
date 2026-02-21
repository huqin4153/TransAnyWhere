using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TransAnyWhereApp.Helpers;

public static class WebSocketHelper
{
    public static async Task SendTextAsync(Stream stream, string message, CancellationToken token)
    {
        byte[] payload = Encoding.UTF8.GetBytes(message);
        byte[] frame = CreateFrame(0x01, payload);
        await stream.WriteAsync(frame, 0, frame.Length, token);
        await stream.FlushAsync(token);
    }

    private static byte[] CreateFrame(byte opcode, byte[] payload)
    {
        using var ms = new MemoryStream();
        ms.WriteByte((byte)(0x80 | opcode)); 

        if (payload.Length <= 125)
        {
            ms.WriteByte((byte)payload.Length);
        }
        else if (payload.Length <= 65535)
        {
            ms.WriteByte(126);
            ms.WriteByte((byte)(payload.Length >> 8));
            ms.WriteByte((byte)(payload.Length & 0xFF));
        }
        else
        {
            ms.WriteByte(127);
            byte[] lenBytes = BitConverter.GetBytes((long)payload.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
            ms.Write(lenBytes, 0, 8);
        }

        ms.Write(payload, 0, payload.Length);
        return ms.ToArray();
    }

    public static string GetAcceptKey(string clientKey)
    {
        string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        byte[] hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(clientKey + guid));
        return Convert.ToBase64String(hash);
    }

    public static string ParseDeviceName(string request)
    {
        var match = Regex.Match(request, @"[?&]name=([^ \s\r\n&]+)");
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : "Unknown Device";
    }

    public static string? ParseWebSocketKey(string request)
    {
        var match = Regex.Match(request, "Sec-WebSocket-Key: (.*)");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public static async Task<(int Opcode, bool IsFinal, byte[] Payload)> ReadFrameAsync(Stream stream, CancellationToken token)
    {
        byte[] header = new byte[2];
        await ReadFullBufferAsync(stream, header, token);

        int opcode = header[0] & 0x0F;
        bool isFinal = (header[0] & 0x80) != 0;
        bool hasMask = (header[1] & 0x80) != 0;
        long payloadLen = header[1] & 0x7F;

        if (payloadLen == 126)
        {
            byte[] lenBytes = new byte[2];
            await ReadFullBufferAsync(stream, lenBytes, token);
            payloadLen = BinaryPrimitives.ReadUInt16BigEndian(lenBytes);
        }
        else if (payloadLen == 127)
        {
            byte[] lenBytes = new byte[8];
            await ReadFullBufferAsync(stream, lenBytes, token);
            payloadLen = (long)BinaryPrimitives.ReadUInt64BigEndian(lenBytes);
        }

        byte[] masks = new byte[4];
        if (hasMask)
        {
            await ReadFullBufferAsync(stream, masks, token);
        }

        byte[] payload = new byte[payloadLen];
        if (payloadLen > 0)
        {
            await ReadFullBufferAsync(stream, payload, token);
        }

        if (hasMask && payloadLen > 0)
        {
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(payload[i] ^ masks[i % 4]);
            }
        }

        return (opcode, isFinal, payload);
    }

    private static async Task ReadFullBufferAsync(Stream stream, byte[] buffer, CancellationToken token)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), token);
            if (read == 0) throw new Exception("Connection closed prematurely while reading expected bytes.");
            totalRead += read;
        }
    }
}
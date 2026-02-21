using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TransAnyWhereApp.Helpers;

namespace TransAnyWhereApp.Services.Storage;

public class FileReceiver : IFileReceiver, IDisposable
{
    private FileStream? _fs;
    private long _expectedPosition = 0;

    public string? CurrentFileName { get; private set; }
    public string? FullFilePath { get; private set; }
    public bool IsActive => _fs != null;


    public long CurrentSize => _fs?.Length ?? _expectedPosition;

    public void PrepareFile(string fileName, string rootPath)
    {
        CloseStream();
        _expectedPosition = 0;

        CurrentFileName = fileName;


        string extension = Path.GetExtension(fileName);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            nameWithoutExt = nameWithoutExt.Replace(c, '_');
        }

        if (nameWithoutExt.Length > 120)
        {
            nameWithoutExt = nameWithoutExt.Substring(0, 120);
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6).ToLower();

        FullFilePath = Path.Combine(rootPath, $"{nameWithoutExt}_{timestamp}{extension}");


        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }

        _fs = new FileStream(
            FullFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough
        );

    }

    public async Task WriteChunkAsync(byte[] data, CancellationToken token)
    {
        if (_fs == null) throw new InvalidOperationException("{{E_File_StreamNotInit}}".Culture());

        if (_fs.Position != _expectedPosition)
        {
            _fs.Seek(_expectedPosition, SeekOrigin.Begin);
        }

        await _fs.WriteAsync(data, 0, data.Length, token);

        await _fs.FlushAsync(token);

        _expectedPosition += data.Length;
    }

    public async Task FinishAsync()
    {
        if (_fs != null)
        {
            await _fs.FlushAsync();

            long finalSize = _fs.Length;
            string finalPath = FullFilePath ?? "{{L_Unknown}}".Culture();

            _fs.Close();
            await _fs.DisposeAsync();
            _fs = null;

            CurrentFileName = null;
            _expectedPosition = 0;

        }
    }

    public void Abort()
    {
        CloseStream();
        if (!string.IsNullOrEmpty(FullFilePath) && File.Exists(FullFilePath))
        {
            try
            {
                File.Delete(FullFilePath);
            }
            catch { }
        }
    }

    private void CloseStream()
    {
        if (_fs != null)
        {
            _fs.Close();
            _fs.Dispose();
            _fs = null;
        }
        _expectedPosition = 0;
    }

    public void Dispose()
    {
        CloseStream();
    }
}
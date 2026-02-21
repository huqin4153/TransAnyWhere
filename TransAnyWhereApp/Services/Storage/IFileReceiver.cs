using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TransAnyWhereApp.Services.Storage
{
    public interface IFileReceiver : IDisposable
    {
        string? CurrentFileName { get; }
        string? FullFilePath { get; }
        bool IsActive { get; }

        long CurrentSize { get; }

        void PrepareFile(string fileName, string rootPath);
        Task WriteChunkAsync(byte[] data, CancellationToken token);
        Task FinishAsync();

        void Abort();
    }
}

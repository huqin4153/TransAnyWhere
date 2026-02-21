using Avalonia;
using System.IO;
using System.Reflection;
using System.Text;

namespace TransAnyWhereApp.Services.Network
{
    public class HtmlProvider : IHtmlProvider
    {
        private readonly string _cachedHtml;

        public HtmlProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("TransAnyWhereApp.Resources.mobile.html");
            if (stream == null) throw new FileNotFoundException("Embedded HTML resource not found. Check Build Action is 'Embedded Resource'.");

            using var reader = new StreamReader(stream);
            _cachedHtml = reader.ReadToEnd();
        }

        public string GetMobilePage(string ip, string port)
        {
            return GetProcessedHtml(_cachedHtml).Replace("{ip}", ip).Replace("{port}", port);
        }

        public string BuildHttpResponse(string html)
        {
            return $"HTTP/1.1 200 OK\r\n" +
                   $"Content-Type: text/html; charset=UTF-8\r\n" +
                   $"Content-Length: {Encoding.UTF8.GetByteCount(html)}\r\n" +
                   $"Connection: close\r\n\r\n{html}";
        }

        public string GetProcessedHtml(string htmlContent)
        {
            string[] keys = {
                "H_TitleReady", "H_BtnSendToPC", "H_LabelCurrentDevice", "H_StatusLoading",
                "H_TipClickToEdit", "H_StatusConnecting", "H_TitleDownloadList", "H_BtnSaveAll",
                "H_BtnClear", "H_BtnReceiveFile", "H_StatusTransferring", "H_StatusPreparing",
                "H_TipKeepAwake", "H_BtnCancel", "H_LabelSize",

                "H_PromptNewName", "H_AlertDownloadTip", "H_ConfirmClearList", "H_ConfirmCancel",
                "H_AlertNoFile", "H_AlertNoConn", "H_AlertConnLost",

                "H_TransferError",
                "H_StatusBackendError",
                "H_ChunkTimeout",

                "H_StatusConnected",
                "H_TransferIndex",
                "H_StatusTransferringDetail",
                "H_StatusWaitingSave",
                "H_StatusSendSuccess"
            };

            foreach (var key in keys)
            {
                if (Application.Current!.TryGetResource(key, null, out var val) && val is string text)
                {
                    htmlContent = htmlContent.Replace($"{{{{{key}}}}}", text);
                }
            }
            return htmlContent;
        }
    }
}

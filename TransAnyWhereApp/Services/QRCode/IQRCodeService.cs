using Avalonia.Media.Imaging;

namespace TransAnyWhereApp.Services.QRCode
{
    public interface IQRCodeService
    {
        Bitmap GenerateQrCode(string text, int moduleSize = 10, int quietZone = 2);
    }
}

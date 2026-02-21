using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Net.Codecrete.QrCodeGenerator;
using System.Runtime.InteropServices;

namespace TransAnyWhereApp.Services.QRCode
{
    public class QRCodeService : IQRCodeService
    {
        public Bitmap GenerateQrCode(string text, int moduleSize = 10, int quietZone = 2)
        {
            var qr = QrCode.EncodeText(text, QrCode.Ecc.Medium);

            int qrSizeWithMargin = qr.Size + (quietZone * 2);
            int totalPixelSize = qrSizeWithMargin * moduleSize;

            var bitmap = new WriteableBitmap(
                new PixelSize(totalPixelSize, totalPixelSize),
                new Vector(96, 96),
                PixelFormat.Rgba8888,
                AlphaFormat.Premul);

            using (var frame = bitmap.Lock())
            {
              
                for (int y = 0; y < qrSizeWithMargin; y++)
                {
                    for (int x = 0; x < qrSizeWithMargin; x++)
                    {
                        bool isDataRegion = (x >= quietZone && x < qr.Size + quietZone &&
                                             y >= quietZone && y < qr.Size + quietZone);

                        uint color = 0xFFFFFFFF; 
                        if (isDataRegion)
                        {
                            color = qr.GetModule(x - quietZone, y - quietZone) ? 0xFF000000 : 0xFFFFFFFF;
                        }

                        for (int sy = 0; sy < moduleSize; sy++)
                        {
                            for (int sx = 0; sx < moduleSize; sx++)
                            {
                                int px = x * moduleSize + sx;
                                int py = y * moduleSize + sy;
                                Marshal.WriteInt32(frame.Address, (py * frame.RowBytes) + (px * 4), (int)color);
                            }
                        }
                    }
                }
            }
            return bitmap;
        }
    }
}

using QRCoder;
using SmartWiFiHelpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartWiFiControls
{
    public static class WiFiUrlToQRCode
    {
        public static async Task ConnectWriteQR(Image image, WiFiUrl url)
        {
            QRCodeGenerator.ECCLevel eccLevel = QRCodeGenerator.ECCLevel.M;

            //Create raw qr code data
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url.ToString(), eccLevel);

            //Create byte/raw bitmap qr code
            BitmapByteQRCode qrCodeBmp = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeImageBmp = qrCodeBmp.GetGraphic(20); // Note: these are colors from the original sample (but they are ugly): , new byte[] { 118, 126, 152 }, new byte[] { 144, 201, 111 });
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(qrCodeImageBmp);
                    await writer.StoreAsync();
                }
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);

                image.Source = bitmapImage;
            }
        }
    }
}

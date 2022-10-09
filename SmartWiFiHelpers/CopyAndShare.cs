using MeCardParser;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace SmartWiFiHelpers
{
    public static class CopyAndShare
    {
        public static void Copy(WiFiUrl wifiurl, IRandomAccessStream imageStream)
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            FillDataPackage(dataPackage, wifiurl, imageStream);
            Clipboard.SetContent(dataPackage);
        }

        public static void FillDataPackage(DataPackage dataPackage, WiFiUrl wifiurl, IRandomAccessStream imageStream)
        {

            Uri uri = new Uri(wifiurl.ToString(), UriKind.Absolute);
            dataPackage.Properties.Title = "Wi-Fi URL to connect to";
            dataPackage.Properties.Description = "Wi-Fi URL and QR Code to connect to";
            //request.SetUri(uri);
            //request.SetWebLink(uri);
            dataPackage.SetApplicationLink(uri);
            dataPackage.SetText(wifiurl.ToString());
            if (imageStream != null)
            {
                // Must have an image; grab it.
                var streamref = RandomAccessStreamReference.CreateFromStream(imageStream);
                dataPackage.SetBitmap(streamref);
            }
        }
    }
}

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;

namespace elevator_simulation.Views
{
    public partial class QrCodeWindow : Window
    {
        public QrCodeWindow() { InitializeComponent(); Loaded += (s,e) => GenerateQr(); }

        private void GenerateQr()
        {
            string ip = GetLocalIp();
            string url = $"http://{ip}:5001";
            UrlTextBlock.Text = url;
            using var qr = new QRCodeGenerator();
            using var data = qr.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var code = new PngByteQRCode(data);
            byte[] png = code.GetGraphic(10);
            using var ms = new MemoryStream(png);
            var bmp = new BitmapImage();
            bmp.BeginInit(); bmp.StreamSource = ms; bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.EndInit();
            QrImage.Source = bmp;
        }

        private static string GetLocalIp()
        {
            try { using var s = new Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0); s.Connect("8.8.8.8", 65530); return (s.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1"; }
            catch { return "127.0.0.1"; }
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();
    }
}

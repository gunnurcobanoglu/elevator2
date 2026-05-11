using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

namespace elevator_simulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Sabitler: Katlarýn görsel ayarlarý
        private const int KatYuksekligi = 55;
        private const int ToplamKatSayisi = 19; // 0'dan 19'a kadar (20 kat)

        // ScrollViewer referanslarý - Sadece 2 asansör
        private ScrollViewer? _scrollViewer1;
        private ScrollViewer? _scrollViewer2;

        /// <summary>
        /// Yapýcý (Constructor): Uygulama baþladýðýnda çalýþýr
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Kat çizgilerini oluþtur (Ýki asansör için)
            var canvas = FindName("MainCanvas") as Canvas;
            if (canvas != null)
            {
                CizgileriOlustur(canvas);
            }

            var canvas2 = FindName("MainCanvas2") as Canvas;
            if (canvas2 != null)
            {
                CizgileriOlustur(canvas2);
            }

            // ScrollViewer referanslarýný al
            _scrollViewer1 = FindName("ScrollViewer1") as ScrollViewer;
            _scrollViewer2 = FindName("ScrollViewer2") as ScrollViewer;

            // ViewModel property deðiþikliklerini dinle
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            // ML veri sayacýný güncelle
            UpdateDataCountDisplay();

            // Saat deðiþikliklerini dinle
            if (txtHour != null)
            {
                txtHour.TextChanged += OnTimeChanged;
            }
            if (txtMinute != null)
            {
                txtMinute.TextChanged += OnTimeChanged;
            }

            // Baþlangýç saatini ayarla
            UpdateSimulationTime();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Asansör katý deðiþtiðinde scroll yap
            if (e.PropertyName == "CurrentFloor")
            {
                var viewModel = sender as ViewModels.MainViewModel;
                if (viewModel != null)
                {
                    ScrollToFloor(_scrollViewer1, viewModel.CurrentFloor);
                }
            }
            else if (e.PropertyName == "CurrentFloor2")
            {
                var viewModel = sender as ViewModels.MainViewModel;
                if (viewModel != null)
                {
                    ScrollToFloor(_scrollViewer2, viewModel.CurrentFloor2);
                }
            }
        }

        private void ScrollToFloor(ScrollViewer? scrollViewer, int floor)
        {
            if (scrollViewer == null) return;

            // Her katýn yüksekliði 55 piksel
            // Asansör yukarý hareket ettikçe, scroll aþaðý kaydýrýlmalý
            // Floor 0 = en altta, Floor 19 = en üstte

            // ScrollViewer'ýn görünür alanýnýn ortasýna getir
            double totalHeight = ToplamKatSayisi * KatYuksekligi;
            double targetPosition = totalHeight - (floor * KatYuksekligi) - (scrollViewer.ViewportHeight / 2);

            // Sýnýrlarý kontrol et
            targetPosition = Math.Max(0, Math.Min(targetPosition, scrollViewer.ScrollableHeight));

            // Yumuþak scroll animasyonu
            scrollViewer.ScrollToVerticalOffset(targetPosition);
        }

        private void OnTimeChanged(object sender, TextChangedEventArgs e)
        {
            // Saat deðiþtiðinde ViewModel'i güncelle
            UpdateSimulationTime();
            UpdateDataCountDisplay();
        }

        private void UpdateSimulationTime()
        {
            if (int.TryParse(txtHour.Text, out int hour) && 
                int.TryParse(txtMinute.Text, out int minute))
            {
                if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60)
                {
                    // ViewModel'e simülasyon zamanýný ilet
                    var viewModel = DataContext as elevator_simulation.ViewModels.MainViewModel;
                    if (viewModel != null)
                    {
                        viewModel.CurrentSimulationTime = new TimeSpan(hour, minute, 0);
                    }
                }
            }
        }

        private void UpdateDataCountDisplay()
        {
            try
            {
                var collector = new Services.MLDataCollector();
                int count = collector.GetRecordCount();
                if (txtDataCount != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtDataCount.Text = $"Toplanan Veri: {count}";
                    });
                }
            }
            catch
            {
                // Hata durumunda sessizce geç
            }
        }

        /// <summary>
        /// Acil Durum butonu týklandýðýnda çalýþýr
        /// </summary>
        private void BtnEmergency_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                if (viewModel.EmergencyStopCommand?.CanExecute(null) == true)
                {
                    viewModel.EmergencyStopCommand.Execute(null);
                }
            }
        }

        private void BtnQrCode_Click(object sender, RoutedEventArgs e)
        {
            var qrWindow = new Views.QrCodeWindow();
            qrWindow.Owner = this;
            qrWindow.Show();
        }

        /// <summary>
        /// Bina kat çizgilerini ve numaralarýný döngü ile Canvas üzerine yerleþtirir.
        /// Her kat için bir çizgi (Line) ve bir kat numarasý (TextBlock) oluþturur.
        /// </summary>
        private void CizgileriOlustur(Canvas canvas)
        {
            if (canvas == null)
            {
                // Eðer parametre null ise MainCanvas'ý kullan
                canvas = MainCanvas;
            }
            
            // N, 0. kattan (zemin) baþlayýp 19. kata (en üst) kadar ilerler.
            for (int N = 0; N <= ToplamKatSayisi; N++)
            {
                // Canvas.Bottom Formülleri:
                // Her kat, bir önceki kattan 55 birim (piksel) yukarýdadýr.
                double lineBottom = N * KatYuksekligi;
                
                // Kat numarasý, çizginin 15 birim yukarýsýna yerleþtirilir.
                double textBottom = lineBottom + 15;

                // 1. Kat Çizgisini Oluþturma (Line)
                Line katCizgisi = new Line
                {
                    X1 = 0,
                    Y1 = 0,
                    X2 = 1000, // Çizginin yatay uzunluðu
                    Y2 = 0,
                    // Koþullu Operatör (Ternary Operator): 
                    // Zemin (0) veya En Üst (19) ise Siyah/Kalýn, deðilse Kahverengi/Ýnce.
                    Stroke = (N == 0 || N == ToplamKatSayisi) ? Brushes.Black : Brushes.Brown,
                    StrokeThickness = (N == 0 || N == ToplamKatSayisi) ? 2 : 1
                };
                
                // Çizginin dikey konumunu Canvas üzerinde ayarla (Canvas.Bottom)
                Canvas.SetBottom(katCizgisi, lineBottom);

                // Koþullu Stil: Ara katlarda (1'den 18'e kadar) kesikli çizgi (StrokeDashArray)
                if (N > 0 && N < ToplamKatSayisi)
                {
                    katCizgisi.StrokeDashArray = new DoubleCollection { 5, 2 }; 
                }

                // 2. Kat Numarasýný Oluþturma (TextBlock)
                TextBlock katNumarasi = new TextBlock
                {
                    Text = N.ToString(), // Kat numarasýný metin olarak ayarla
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Brown
                };
                
                // Kat numarasýnýn yatay konumunu ayarla (Canvas.Left)
                Canvas.SetLeft(katNumarasi, 15);
                
                // Kat numarasýnýn dikey konumunu ayarla (Canvas.Bottom)
                Canvas.SetBottom(katNumarasi, textBottom);

                // 3. Oluþturulan görsel öðeleri Canvas'a ekle
                canvas.Children.Add(katCizgisi);
                canvas.Children.Add(katNumarasi);
            }
        }
    }
}

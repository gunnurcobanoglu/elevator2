using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Threading;

namespace elevator_simulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Sabitler: Katlar�n g�rsel ayarlar�
        private const int KatYuksekligi = 55;
        private const int ToplamKatSayisi = 19; // 0'dan 19'a kadar (20 kat)

        // ScrollViewer referanslar� - Sadece 2 asans�r
        private ScrollViewer? _scrollViewer1;
        private ScrollViewer? _scrollViewer2;
        // Simülatif saat timerı
        private readonly DispatcherTimer _simulationClockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        /// <summary>
        /// Yap�c� (Constructor): Uygulama ba�lad���nda �al���r
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Kat �izgilerini olu�tur (�ki asans�r i�in)
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

            // ScrollViewer referanslar�n� al
            _scrollViewer1 = FindName("ScrollViewer1") as ScrollViewer;
            _scrollViewer2 = FindName("ScrollViewer2") as ScrollViewer;

            // ViewModel property de�i�ikliklerini dinle
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            // ML veri sayac�n� g�ncelle
            UpdateDataCountDisplay();

            // Saat de�i�ikliklerini dinle
            if (txtHour != null)
            {
                txtHour.TextChanged += OnTimeChanged;
            }
            if (txtMinute != null)
            {
                txtMinute.TextChanged += OnTimeChanged;
            }

            // Ba�lang�� saatini ayarla
            UpdateSimulationTime();
            // Simülatif saat timerını başlat
            _simulationClockTimer.Tick += SimulationClockTimer_Tick;
            _simulationClockTimer.Start();
            txtHour.IsReadOnly = true;
            txtMinute.IsReadOnly = true;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Asans�r kat� de�i�ti�inde scroll yap
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

            // Her kat�n y�ksekli�i 55 piksel
            // Asans�r yukar� hareket ettik�e, scroll a�a�� kayd�r�lmal�
            // Floor 0 = en altta, Floor 19 = en �stte

            // ScrollViewer'�n g�r�n�r alan�n�n ortas�na getir
            double totalHeight = ToplamKatSayisi * KatYuksekligi;
            double targetPosition = totalHeight - (floor * KatYuksekligi) - (scrollViewer.ViewportHeight / 2);

            // S�n�rlar� kontrol et
            targetPosition = Math.Max(0, Math.Min(targetPosition, scrollViewer.ScrollableHeight));

            // Yumu�ak scroll animasyonu
            scrollViewer.ScrollToVerticalOffset(targetPosition);
        }
        private void SimulationClockTimer_Tick(object? sender, EventArgs e)
        {
            var viewModel = DataContext as elevator_simulation.ViewModels.MainViewModel;
            if (viewModel == null) return;

            // Mevcut zamana 1 dakika ekle, 24 saatlik döngü sağla
            var newTime = viewModel.CurrentSimulationTime.Add(TimeSpan.FromMinutes(1));
            if (newTime.TotalHours >= 24)
                newTime = newTime.Subtract(TimeSpan.FromHours(24));

            viewModel.CurrentSimulationTime = newTime;

            // TextBox'ları güncelle (TextChanged event'ini geçici olarak devre dışı bırak)
            txtHour.TextChanged -= OnTimeChanged;
            txtMinute.TextChanged -= OnTimeChanged;

            txtHour.Text = newTime.Hours.ToString("D2");
            txtMinute.Text = newTime.Minutes.ToString("D2");

            txtHour.TextChanged += OnTimeChanged;
            txtMinute.TextChanged += OnTimeChanged;

            // Veri sayacını güncelle
            UpdateDataCountDisplay();
        }
        private void OnTimeChanged(object sender, TextChangedEventArgs e)
        {
            // Saat de�i�ti�inde ViewModel'i g�ncelle
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
                    // ViewModel'e sim�lasyon zaman�n� ilet
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
                var viewModel = DataContext as elevator_simulation.ViewModels.MainViewModel;
                if (viewModel == null) return;
                int count = viewModel.GetDataCount();
                if (txtDataCount != null)
                {
                    txtDataCount.Text = $"Toplanan Veri: {count}";
                }
            }
            catch
            {
                // Hata durumunda sessizce ge�
            }
        }

        /// <summary>
        /// Acil Durum butonu t�kland���nda �al���r
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

        private void BtnToggleClock_Click(object sender, RoutedEventArgs e)
        {
            if (_simulationClockTimer.IsEnabled)
            {
                _simulationClockTimer.Stop();
                btnToggleClock.Content = "▶ Devam Et";
                btnToggleClock.Background = new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#388E3C"));
                txtHour.IsReadOnly = false;
                txtMinute.IsReadOnly = false;
            }
            else
            {
                // Kullanıcının girdiği saatten devam et
                UpdateSimulationTime();
                _simulationClockTimer.Start();
                btnToggleClock.Content = "⏸ Durdur";
                btnToggleClock.Background = new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F57C00"));
                txtHour.IsReadOnly = true;
                txtMinute.IsReadOnly = true;
            }
        }

        private void BtnQrCode_Click(object sender, RoutedEventArgs e)
        {
            var qrWindow = new Views.QrCodeWindow();
            qrWindow.Owner = this;
            qrWindow.Show();
        }

        /// <summary>
        /// Bina kat �izgilerini ve numaralar�n� d�ng� ile Canvas �zerine yerle�tirir.
        /// Her kat i�in bir �izgi (Line) ve bir kat numaras� (TextBlock) olu�turur.
        /// </summary>
        private void CizgileriOlustur(Canvas canvas)
        {
            if (canvas == null)
            {
                // E�er parametre null ise MainCanvas'� kullan
                canvas = MainCanvas;
            }
            
            // N, 0. kattan (zemin) ba�lay�p 19. kata (en �st) kadar ilerler.
            for (int N = 0; N <= ToplamKatSayisi; N++)
            {
                // Canvas.Bottom Form�lleri:
                // Her kat, bir �nceki kattan 55 birim (piksel) yukar�dad�r.
                double lineBottom = N * KatYuksekligi;
                
                // Kat numaras�, �izginin 15 birim yukar�s�na yerle�tirilir.
                double textBottom = lineBottom + 15;

                // 1. Kat �izgisini Olu�turma (Line)
                Line katCizgisi = new Line
                {
                    X1 = 0,
                    Y1 = 0,
                    X2 = 1000, // �izginin yatay uzunlu�u
                    Y2 = 0,
                    // Ko�ullu Operat�r (Ternary Operator): 
                    // Zemin (0) veya En �st (19) ise Siyah/Kal�n, de�ilse Kahverengi/�nce.
                    Stroke = (N == 0 || N == ToplamKatSayisi) ? Brushes.Black : Brushes.Brown,
                    StrokeThickness = (N == 0 || N == ToplamKatSayisi) ? 2 : 1
                };
                
                // �izginin dikey konumunu Canvas �zerinde ayarla (Canvas.Bottom)
                Canvas.SetBottom(katCizgisi, lineBottom);

                // Ko�ullu Stil: Ara katlarda (1'den 18'e kadar) kesikli �izgi (StrokeDashArray)
                if (N > 0 && N < ToplamKatSayisi)
                {
                    katCizgisi.StrokeDashArray = new DoubleCollection { 5, 2 }; 
                }

                // 2. Kat Numaras�n� Olu�turma (TextBlock)
                TextBlock katNumarasi = new TextBlock
                {
                    Text = N.ToString(), // Kat numaras�n� metin olarak ayarla
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Brown
                };
                
                // Kat numaras�n�n yatay konumunu ayarla (Canvas.Left)
                Canvas.SetLeft(katNumarasi, 15);
                
                // Kat numaras�n�n dikey konumunu ayarla (Canvas.Bottom)
                Canvas.SetBottom(katNumarasi, textBottom);

                // 3. Olu�turulan g�rsel ��eleri Canvas'a ekle
                canvas.Children.Add(katCizgisi);
                canvas.Children.Add(katNumarasi);
            }
        }
    }
}

using System.IO;
using System.Text;
using elevator_simulation.Models;

namespace elevator_simulation.Services
{
    /// <summary>
    /// Machine Learning için veri toplama servisi
    /// CSV formatýnda eðitim verisi kaydeder
    /// </summary>
    public class MLDataCollector
    {
        private readonly string _csvFilePath;
        private bool _headerWritten = false;

        public MLDataCollector(string csvFilePath = "ml_training_data.csv")
        {
            _csvFilePath = csvFilePath;
            
            // Eðer dosya varsa header'ý kontrol et
            if (File.Exists(_csvFilePath))
            {
                var firstLine = File.ReadLines(_csvFilePath).FirstOrDefault();
                
                // Eski format header'ý varsa dosyayý sil ve yeniden oluþtur
                if (firstLine != null && 
                    (firstLine.Contains("SimulationTime") || firstLine.Contains("Hour,Minute,PickupFloor")))
                {
                    // Eski format - dosyayý sil
                    File.Delete(_csvFilePath);
                    WriteHeader();
                }
                else if (firstLine != null && firstLine.Contains("Tarih,Saat"))
                {
                    // Yeni format - header zaten yazýlmýþ
                    _headerWritten = true;
                }
                else
                {
                    // Header yok veya bozuk - yeniden yaz
                    WriteHeader();
                }
            }
            else
            {
                // Dosya yok - oluþtur
                WriteHeader();
            }
        }

        private void WriteHeader()
        {
            var header = "Tarih,Saat,Saat_Tam,Dakika,Cagri_Kat,Asansor_Kat,Bekleme_Saniye,Toplam_Yolcu,Asansor_Durum";
            File.WriteAllText(_csvFilePath, header + Environment.NewLine, Encoding.UTF8);
            _headerWritten = true;
        }

        /// <summary>
        /// ML veri kaydý oluþtur
        /// </summary>
        public void RecordRequest(
            TimeSpan simulationTime,
            int pickupFloor,
            int elevatorFloorAtRequest,
            int waitTimeSeconds,
            int totalPassengers,
            string elevatorState)
        {
            if (!_headerWritten)
            {
                WriteHeader();
            }

            // Tarih ve saat formatýný düzenle
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var time = simulationTime.ToString(@"hh\:mm\:ss");
            
            var line = $"{date},{time},{simulationTime.Hours},{simulationTime.Minutes}," +
                       $"{pickupFloor},{elevatorFloorAtRequest},{waitTimeSeconds}," +
                       $"{totalPassengers},{elevatorState}";

            File.AppendAllText(_csvFilePath, line + Environment.NewLine, Encoding.UTF8);
        }

        /// <summary>
        /// Tüm veriyi temizle (yeni eðitim için)
        /// </summary>
        public void ClearData()
        {
            if (File.Exists(_csvFilePath))
            {
                File.Delete(_csvFilePath);
            }
            _headerWritten = false;
            WriteHeader();
        }

        /// <summary>
        /// Toplanan veri sayýsýný döndür
        /// </summary>
        public int GetRecordCount()
        {
            if (!File.Exists(_csvFilePath))
                return 0;

            var lines = File.ReadAllLines(_csvFilePath);
            return Math.Max(0, lines.Length - 1); // Header hariç
        }
    }
}

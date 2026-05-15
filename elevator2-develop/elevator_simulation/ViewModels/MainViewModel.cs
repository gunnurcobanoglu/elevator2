using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using elevator_simulation.Commands;
using elevator_simulation.Models;
using elevator_simulation.Services;

namespace elevator_simulation.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // ==================== FIELDS ====================
        
        // Asansör Modelleri
        private readonly ElevatorModel _elevator;
        private readonly ElevatorModel _elevator2;
        
        // Timer ve Servisler
        private readonly DispatcherTimer _timer;
        private readonly MLDataCollector _mlDataCollector;
        
        // Simulasyon Değişkenleri
        private DateTime _simulationStartTime;
        private bool _isSimulationRunning;
        private TimeSpan _currentSimulationTime;
        
        // Asansör 1 Değişkenleri
        private int _currentFloor;
        private string _elevatorState;
        private string _statusMessage;
        private string _totalTime;
        private int _passengerCount;
        private bool _isInnerPanelOpen;
        private double _doorOpenAmount;
        private bool _isProcessingRequests;
        private ObservableCollection<int> _passengerIcons;
        
        // Asansör 2 Değişkenleri
        private int _currentFloor2;
        private string _elevatorState2;
        private int _passengerCount2;
        private bool _isInnerPanelOpen2;
        private double _doorOpenAmount2;
        private bool _isProcessingRequests2;
        private ObservableCollection<int> _passengerIcons2;
        
        // Yolcu İstekleri
        private readonly List<PassengerRequest> _pendingRequests = new();
        private readonly List<PassengerRequest> _pendingRequests2 = new();
        private readonly HashSet<int> _destinationFloors = new();
        private readonly HashSet<int> _destinationFloors2 = new();
        private const int MaxCapacity = 10;

        // Kat bazlı çağrı sayacı (her kattan kaç kişi çağrı yaptı)
        private readonly Dictionary<int, int> _floorCallCounts = new();

        // Enerji Tasarrufu Modu
        private const int ParkingFloor = 0; // Zemin kat
        private readonly TimeSpan _idleTimeBeforeParking = TimeSpan.FromSeconds(30);
        private DateTime? _elevator1IdleStartTime;
        private DateTime? _elevator2IdleStartTime;

        // Maksimum Bekleme Süresi Garantisi
        private const int MaxWaitTimeSeconds = 60;

        // Doluluk ve Performans İzleme
        private int _elevator1TotalDistance;
        private int _elevator2TotalDistance;
        
        // ==================== PROPERTIES ====================
        
        public ObservableCollection<int> Floors { get; }
        
        public ObservableCollection<int> PassengerIcons
        {
            get => _passengerIcons;
            set => SetProperty(ref _passengerIcons, value);
        }
        
        public ObservableCollection<int> PassengerIcons2
        {
            get => _passengerIcons2;
            set => SetProperty(ref _passengerIcons2, value);
        }
        
        public ICommand CallElevatorCommand { get; }
        public ICommand SelectDestinationCommand { get; }
        public ICommand SelectDestinationCommand2 { get; }
        public ICommand EmergencyStopCommand { get; }
        
        public int CurrentFloor
        {
            get => _currentFloor;
            set => SetProperty(ref _currentFloor, value);
        }
        
        public int CurrentFloor2
        {
            get => _currentFloor2;
            set => SetProperty(ref _currentFloor2, value);
        }
        
        public string ElevatorStateDisplay
        {
            get => _elevatorState;
            set => SetProperty(ref _elevatorState, value);
        }
        
        public string ElevatorStateDisplay2
        {
            get => _elevatorState2;
            set => SetProperty(ref _elevatorState2, value);
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        public string TotalTime
        {
            get => _totalTime;
            set => SetProperty(ref _totalTime, value);
        }
        
        public int PassengerCount
        {
            get => _passengerCount;
            set
            {
                SetProperty(ref _passengerCount, value);
                OnPropertyChanged(nameof(HasPassenger));
                OnPropertyChanged(nameof(Elevator1LoadPercentage));
                OnPropertyChanged(nameof(Elevator1LoadStatus));

                PassengerIcons.Clear();
                for (int i = 0; i < value; i++)
                {
                    PassengerIcons.Add(i);
                }
            }
        }

        public int PassengerCount2
        {
            get => _passengerCount2;
            set
            {
                SetProperty(ref _passengerCount2, value);
                OnPropertyChanged(nameof(HasPassenger2));
                OnPropertyChanged(nameof(Elevator2LoadPercentage));
                OnPropertyChanged(nameof(Elevator2LoadStatus));

                PassengerIcons2.Clear();
                for (int i = 0; i < value; i++)
                {
                    PassengerIcons2.Add(i);
                }
            }
        }
        
        public bool HasPassenger => _passengerCount > 0;
        public bool HasPassenger2 => _passengerCount2 > 0;
        
        public TimeSpan CurrentSimulationTime
        {
            get => _currentSimulationTime;
            set => SetProperty(ref _currentSimulationTime, value);
        }

        public int GetDataCount() => _mlDataCollector.GetRecordCount();
        
        public bool IsInnerPanelOpen
        {
            get => _isInnerPanelOpen;
            set
            {
                // Z-Index'i SetProperty'den ÖNCE ayarla
                if (value)
                {
                    // Asansör 1'in popup'ını öne getir
                    Elevator1PanelZIndex = 1001;
                    Elevator2PanelZIndex = 1000;
                }
                else
                {
                    // Panel kapandığında Z-Index'i reset et
                    Elevator1PanelZIndex = 1000;
                }

                // Sonra property'yi set et
                SetProperty(ref _isInnerPanelOpen, value);
            }
        }

        public bool IsInnerPanelOpen2
        {
            get => _isInnerPanelOpen2;
            set
            {
                // Z-Index'i SetProperty'den ÖNCE ayarla
                if (value)
                {
                    // Asansör 2'nin popup'ını öne getir
                    Elevator2PanelZIndex = 1001;
                    Elevator1PanelZIndex = 1000;
                }
                else
                {
                    // Panel kapandığında Z-Index'i reset et
                    Elevator2PanelZIndex = 1000;
                }

                // Sonra property'yi set et
                SetProperty(ref _isInnerPanelOpen2, value);
            }
        }

        public double DoorOpenAmount
        {
            get => _doorOpenAmount;
            set => SetProperty(ref _doorOpenAmount, value);
        }

        public double DoorOpenAmount2
        {
            get => _doorOpenAmount2;
            set => SetProperty(ref _doorOpenAmount2, value);
        }

        // Doluluk Göstergeleri
        public string Elevator1LoadPercentage => $"{(_passengerCount * 100 / MaxCapacity)}%";
        public string Elevator2LoadPercentage => $"{(_passengerCount2 * 100 / MaxCapacity)}%";

        public string Elevator1LoadStatus => _passengerCount switch
        {
            0 => "🟢 Boş",
            <= 3 => "🟡 Az Yolcu",
            <= 7 => "🟠 Normal",
            <= 9 => "🟠 Kalabalık",
            _ => "🔴 Dolu"
        };

        public string Elevator2LoadStatus => _passengerCount2 switch
        {
            0 => "🟢 Boş",
            <= 3 => "🟡 Az Yolcu",
            <= 7 => "🟠 Normal",
            <= 9 => "🟠 Kalabalık",
            _ => "🔴 Dolu"
        };

        // İç Panel Progress Bilgileri
        private int _elevator1WaitingPassengers;
        private int _elevator2WaitingPassengers;

        public int Elevator1WaitingPassengers
        {
            get => _elevator1WaitingPassengers;
            set
            {
                SetProperty(ref _elevator1WaitingPassengers, value);
                OnPropertyChanged(nameof(Elevator1PanelTitle));
            }
        }

        public int Elevator2WaitingPassengers
        {
            get => _elevator2WaitingPassengers;
            set
            {
                SetProperty(ref _elevator2WaitingPassengers, value);
                OnPropertyChanged(nameof(Elevator2PanelTitle));
            }
        }

        public string Elevator1PanelTitle => _elevator1WaitingPassengers > 0 
            ? $"🔵 ASANSÖR 1 - {_elevator1WaitingPassengers} Yolcu Hedef Seçiyor"
            : "🔵 ASANSÖR 1 - İÇ PANEL";

        public string Elevator2PanelTitle => _elevator2WaitingPassengers > 0 
            ? $"🟢 ASANSÖR 2 - {_elevator2WaitingPassengers} Yolcu Hedef Seçiyor"
            : "🟢 ASANSÖR 2 - İÇ PANEL";

        // Popup Z-Index (Hangi popup öndeyse onu göstermek için)
        private int _elevator1PanelZIndex = 1000;
        private int _elevator2PanelZIndex = 1000;

        public int Elevator1PanelZIndex
        {
            get => _elevator1PanelZIndex;
            set
            {
                if (SetProperty(ref _elevator1PanelZIndex, value))
                {
                    // UI'yı zorla güncelle
                    OnPropertyChanged(nameof(Elevator1PanelZIndex));
                    OnPropertyChanged(nameof(IsElevator1PanelInteractive));
                    OnPropertyChanged(nameof(IsElevator2PanelInteractive));
                }
            }
        }

        public int Elevator2PanelZIndex
        {
            get => _elevator2PanelZIndex;
            set
            {
                if (SetProperty(ref _elevator2PanelZIndex, value))
                {
                    // UI'yı zorla güncelle
                    OnPropertyChanged(nameof(Elevator2PanelZIndex));
                    OnPropertyChanged(nameof(IsElevator1PanelInteractive));
                    OnPropertyChanged(nameof(IsElevator2PanelInteractive));
                }
            }
        }

        // Popup'ların tıklanabilirliği için property'ler
        // Eşit durumunda Elevator1 öncelikli olur (>=), böylece sadece biri aktif olur
        public bool IsElevator1PanelInteractive => _elevator1PanelZIndex >= _elevator2PanelZIndex;
        // Elevator2 sadece Z-Index'i kesinlikle daha yüksekse (>) aktif olur
        public bool IsElevator2PanelInteractive => _elevator2PanelZIndex > _elevator1PanelZIndex;

        // ==================== CONSTRUCTOR ====================
        
        public MainViewModel()
        {
            _elevator = new ElevatorModel();
            _elevator2 = new ElevatorModel();
            _mlDataCollector = new MLDataCollector();
            Floors = new ObservableCollection<int>();
            _passengerIcons = new ObservableCollection<int>();
            _passengerIcons2 = new ObservableCollection<int>();
            
            for (int i = 0; i < ElevatorModel.TotalFloors; i++)
            {
                Floors.Add(i);
            }
            
            CallElevatorCommand = new RelayCommand(OnCallElevator, CanCallElevator);
            SelectDestinationCommand = new RelayCommand(OnSelectDestination, CanSelectDestination);
            SelectDestinationCommand2 = new RelayCommand(OnSelectDestination2, CanSelectDestination2);
            EmergencyStopCommand = new RelayCommand(async (param) => await ExecuteEmergencyStop(), (param) => true);
            
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;
            
            _currentFloor = 0;
            _currentFloor2 = 0;
            _elevatorState = "Beklemede";
            _elevatorState2 = "Beklemede";
            _statusMessage = "[Sistem] Asansörler hazır.";
            _totalTime = "00:00:00";
            _passengerCount = 0;
            _passengerCount2 = 0;
            _isSimulationRunning = false;
            _isInnerPanelOpen = false;
            _isInnerPanelOpen2 = false;
            _doorOpenAmount = 0.0;
            _doorOpenAmount2 = 0.0;
            _isProcessingRequests = false;
            _isProcessingRequests2 = false;
        }
        
        // ==================== HELPER METHODS ====================

        private void AddStatusMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var newMessage = $"[{timestamp}] {message}";

            if (string.IsNullOrEmpty(_statusMessage))
            {
                StatusMessage = newMessage;
            }
            else
            {
                StatusMessage = newMessage + "\n" + _statusMessage;
            }
        }

        /// <summary>
        /// Asansörün bir çağrıya ne kadar uygun olduğunu hesaplar (yön bazlı optimizasyon)
        /// </summary>
        private int CalculateElevatorScore(int callingFloor, int elevatorFloor, Models.ElevatorState elevatorState, int distance, bool isBusy)
        {
            int score = 100; // Başlangıç skoru

            // Meşgul asansöre ceza
            if (isBusy)
            {
                score -= 80;
            }

            // Mesafe cezası (her kat için -3 puan)
            score -= distance * 3;

            // Yön bonusu: Asansör aynı yönde mi?
            int elevatorToCall = callingFloor - elevatorFloor;

            if (elevatorState == Models.ElevatorState.MovingUp && elevatorToCall > 0)
            {
                // Asansör yukarı gidiyor ve çağrı da yukarıda
                score += 40;
                AddStatusMessage($"[Optimizasyon] Yukarı giden asansöre +40 bonus (Aynı yön)");
            }
            else if (elevatorState == Models.ElevatorState.MovingDown && elevatorToCall < 0)
            {
                // Asansör aşağı gidiyor ve çağrı da aşağıda
                score += 40;
                AddStatusMessage($"[Optimizasyon] Aşağı giden asansöre +40 bonus (Aynı yön)");
            }
            else if (elevatorState == Models.ElevatorState.Idle)
            {
                // Boşta olan asansöre bonus
                score += 30;
            }
            else
            {
                // Ters yönde giden asansöre ceza
                score -= 20;
                AddStatusMessage($"[Optimizasyon] Ters yönde giden asansöre -20 ceza");
            }

            return score;
        }

        /// <summary>
        /// Yolcu sayısına göre kat geçiş süresini hesaplar (ağırlık sensörü)
        /// </summary>
        private double CalculateFloorTravelTime(int passengerCount)
        {
            double baseTime = ElevatorModel.FloorTravelTime;
            double loadFactor = (double)passengerCount / MaxCapacity;

            // %100 dolulukta %20 daha yavaş (ağırlık etkisi)
            double adjustedTime = baseTime * (1 + loadFactor * 0.2);

            return adjustedTime;
        }

        /// <summary>
        /// Uzun süre bekleyen yolcuları kontrol eder ve öncelik verir
        /// </summary>
        private void CheckLongWaitingPassengers()
        {
            // Asansör 1 için kontrol
            var longWaiting1 = _pendingRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .Where(r => (DateTime.Now - r.RequestTime).TotalSeconds > MaxWaitTimeSeconds)
                .OrderBy(r => r.RequestTime)
                .ToList();

            foreach (var urgent in longWaiting1)
            {
                var waitTime = (int)(DateTime.Now - urgent.RequestTime).TotalSeconds;
                AddStatusMessage($"⚠️ ACİL [Asansör 1]: Kat {urgent.PickupFloor} - {waitTime}s bekledi! Öncelik veriliyor");

                // İsteği listenin başına al (öncelik)
                _pendingRequests.Remove(urgent);
                _pendingRequests.Insert(0, urgent);
            }

            // Asansör 2 için kontrol
            var longWaiting2 = _pendingRequests2
                .Where(r => r.Status == RequestStatus.Pending)
                .Where(r => (DateTime.Now - r.RequestTime).TotalSeconds > MaxWaitTimeSeconds)
                .OrderBy(r => r.RequestTime)
                .ToList();

            foreach (var urgent in longWaiting2)
            {
                var waitTime = (int)(DateTime.Now - urgent.RequestTime).TotalSeconds;
                AddStatusMessage($"⚠️ ACİL [Asansör 2]: Kat {urgent.PickupFloor} - {waitTime}s bekledi! Öncelik veriliyor");

                // İsteği listenin başına al (öncelik)
                _pendingRequests2.Remove(urgent);
                _pendingRequests2.Insert(0, urgent);
            }
        }
        
        // ==================== COMMAND HANDLERS ====================
        
        private bool CanCallElevator(object? parameter)
        {
            return parameter is int;
        }
        
        private async void OnCallElevator(object? parameter)
        {
            if (parameter is int callingFloor)
            {
                if (!_isSimulationRunning)
                {
                    _simulationStartTime = DateTime.Now;
                    _timer.Start();
                    _isSimulationRunning = true;
                }

                // Bu kattan kaç kişi çağrı yaptığını takip et
                if (!_floorCallCounts.ContainsKey(callingFloor))
                {
                    _floorCallCounts[callingFloor] = 0;
                }
                _floorCallCounts[callingFloor]++;

                int callCountOnFloor = _floorCallCounts[callingFloor];

                // Aynı kata zaten bir asansör gönderilmiş mi kontrol et
                bool elevator1AlreadyAssigned = _pendingRequests.Any(r => 
                    r.PickupFloor == callingFloor && r.Status == RequestStatus.Pending);
                bool elevator2AlreadyAssigned = _pendingRequests2.Any(r => 
                    r.PickupFloor == callingFloor && r.Status == RequestStatus.Pending);

                // ===== GERÇEKÇİ MANTIK =====

                // 1. Eğer bir asansör zaten bu kata gidiyorsa, ona request ekle
                if (elevator1AlreadyAssigned && !elevator2AlreadyAssigned)
                {
                    // Asansör 1 zaten gidiyor

                    // 10'dan fazla kişi varsa ikinci asansörü de gönder
                    if (callCountOnFloor > MaxCapacity)
                    {
                        AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] Kat {callingFloor}: Kapasite aşıldı! Asansör 2 de gönderiliyor (Toplam: {callCountOnFloor} kişi)");
                        await HandleElevatorCall(callingFloor, 2, _currentFloor2, _pendingRequests2);
                        return;
                    }

                    // Sadece request ekle
                    var newRequest = new PassengerRequest(callingFloor);
                    newRequest.SimulationTime = CurrentSimulationTime;
                    newRequest.ElevatorFloorAtRequest = _currentFloor;
                    newRequest.RequestTime = DateTime.Now;
                    _pendingRequests.Add(newRequest);

                    AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] Kat {callingFloor}: Yolcu eklendi → Asansör 1 yolda (Toplam: {callCountOnFloor} kişi)");
                    return;
                }

                if (elevator2AlreadyAssigned && !elevator1AlreadyAssigned)
                {
                    // Asansör 2 zaten gidiyor

                    // 10'dan fazla kişi varsa birinci asansörü de gönder
                    if (callCountOnFloor > MaxCapacity)
                    {
                        AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] Kat {callingFloor}: Kapasite aşıldı! Asansör 1 de gönderiliyor (Toplam: {callCountOnFloor} kişi)");
                        await HandleElevatorCall(callingFloor, 1, _currentFloor, _pendingRequests);
                        return;
                    }

                    // Sadece request ekle
                    var newRequest = new PassengerRequest(callingFloor);
                    newRequest.SimulationTime = CurrentSimulationTime;
                    newRequest.ElevatorFloorAtRequest = _currentFloor2;
                    newRequest.RequestTime = DateTime.Now;
                    _pendingRequests2.Add(newRequest);

                    AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] Kat {callingFloor}: Yolcu eklendi → Asansör 2 yolda (Toplam: {callCountOnFloor} kişi)");
                    return;
                }

                // 2. Her iki asansör de zaten gidiyorsa (10'dan fazla kişi durumu)
                if (elevator1AlreadyAssigned && elevator2AlreadyAssigned)
                {
                    // İki asansör de yolda, request'i daha az dolu olana ekle
                    if (_pendingRequests.Count(r => r.PickupFloor == callingFloor) <= _pendingRequests2.Count(r => r.PickupFloor == callingFloor))
                    {
                        var newRequest = new PassengerRequest(callingFloor);
                        newRequest.SimulationTime = CurrentSimulationTime;
                        newRequest.ElevatorFloorAtRequest = _currentFloor;
                        newRequest.RequestTime = DateTime.Now;
                        _pendingRequests.Add(newRequest);
                        AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] Kat {callingFloor}: Yolcu eklendi → Asansör 1'e (Her iki asansör de yolda)");
                    }
                    else
                    {
                        var newRequest = new PassengerRequest(callingFloor);
                        newRequest.SimulationTime = CurrentSimulationTime;
                        newRequest.ElevatorFloorAtRequest = _currentFloor2;
                        newRequest.RequestTime = DateTime.Now;
                        _pendingRequests2.Add(newRequest);
                        AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] Kat {callingFloor}: Yolcu eklendi → Asansör 2'ye (Her iki asansör de yolda)");
                    }
                    return;
                }

                // 3. Hiçbir asansör o kata gitmiyorsa, SKOR SİSTEMİNE göre en uygun asansörü seç
                int distance1 = Math.Abs(_currentFloor - callingFloor);
                int distance2 = Math.Abs(_currentFloor2 - callingFloor);

                bool elevator1Busy = _isProcessingRequests || _elevator.State != Models.ElevatorState.Idle;
                bool elevator2Busy = _isProcessingRequests2 || _elevator2.State != Models.ElevatorState.Idle;

                int score1 = CalculateElevatorScore(callingFloor, _currentFloor, _elevator.State, distance1, elevator1Busy);
                int score2 = CalculateElevatorScore(callingFloor, _currentFloor2, _elevator2.State, distance2, elevator2Busy);

                int selectedElevator = score2 > score1 ? 2 : 1;

                AddStatusMessage($"[Skor] Asansör 1: {score1}, Asansör 2: {score2} → Asansör {selectedElevator} seçildi");

                // Seçilen asansöre göre işlem yap
                switch (selectedElevator)
                {
                    case 1:
                        await HandleElevatorCall(callingFloor, 1, _currentFloor, _pendingRequests);
                        break;
                    case 2:
                        await HandleElevatorCall(callingFloor, 2, _currentFloor2, _pendingRequests2);
                        break;
                }
            }
        }
        
        private async Task HandleElevatorCall(int callingFloor, int elevatorNumber, int currentFloor, List<PassengerRequest> pendingRequests)
        {
            var request = new PassengerRequest(callingFloor);
            request.SimulationTime = CurrentSimulationTime;
            request.ElevatorFloorAtRequest = currentFloor;
            request.RequestTime = DateTime.Now;
            
            pendingRequests.Add(request);
            
            AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] [Asansör {elevatorNumber}] Kat {callingFloor}: Çağrı geldi");
            
            int floorDistance = Math.Abs(callingFloor - currentFloor);
            
            var elevatorState = elevatorNumber switch
            {
                1 => _elevator.State.ToString(),
                2 => _elevator2.State.ToString(),
                _ => "Unknown"
            };
            
            var passengerCount = elevatorNumber switch
            {
                1 => _passengerCount,
                2 => _passengerCount2,
                _ => 0
            };
            
            _mlDataCollector.RecordRequest(
                CurrentSimulationTime,
                callingFloor,
                currentFloor,
                -1,
                floorDistance,
                $"Asansör {elevatorNumber}",
                0,
                passengerCount,
                elevatorState
            );
            
            bool isIdleState = elevatorNumber switch
            {
                1 => _elevator.State == Models.ElevatorState.Idle,
                2 => _elevator2.State == Models.ElevatorState.Idle,
                _ => false
            };
            
            bool isProcessing = elevatorNumber switch
            {
                1 => _isProcessingRequests,
                2 => _isProcessingRequests2,
                _ => false
            };
            
            if (callingFloor == currentFloor && isIdleState)
            {
                switch (elevatorNumber)
                {
                    case 1: await HandleSameFloorPickup(request); break;
                    case 2: await HandleSameFloorPickup2(request); break;
                }
            }
            else if (!isProcessing)
            {
                switch (elevatorNumber)
                {
                    case 1: await ProcessRequests(); break;
                    case 2: await ProcessRequests2(); break;
                }
            }
        }
        
        private bool CanSelectDestination(object? parameter)
        {
            return parameter is int targetFloor && 
                   _isInnerPanelOpen && 
                   targetFloor != _currentFloor &&
                   _passengerCount > 0;
        }
        
        private bool CanSelectDestination2(object? parameter)
        {
            return parameter is int targetFloor && 
                   _isInnerPanelOpen2 && 
                   targetFloor != _currentFloor2 &&
                   _passengerCount2 > 0;
        }
        
        private void OnSelectDestination(object? parameter)
        {
            if (parameter is int targetFloor)
            {
                var lastPickedUp = _pendingRequests
                    .Where(r => r.Status == RequestStatus.PickedUp && r.DestinationFloor == -1)
                    .OrderByDescending(r => r.PickupFloor == _currentFloor)
                    .FirstOrDefault();
                
                if (lastPickedUp != null)
                {
                    lastPickedUp.DestinationFloor = targetFloor;
                    _destinationFloors.Add(targetFloor);
                    AddStatusMessage($"[Asansör 1] Kat {lastPickedUp.PickupFloor}: Yolcu bindi, {targetFloor}. kata gidecek");
                }
            }
        }
        
        private void OnSelectDestination2(object? parameter)
        {
            if (parameter is int targetFloor)
            {
                var lastPickedUp = _pendingRequests2
                    .Where(r => r.Status == RequestStatus.PickedUp && r.DestinationFloor == -1)
                    .OrderByDescending(r => r.PickupFloor == _currentFloor2)
                    .FirstOrDefault();
                
                if (lastPickedUp != null)
                {
                    lastPickedUp.DestinationFloor = targetFloor;
                    _destinationFloors2.Add(targetFloor);
                    AddStatusMessage($"[Asansör 2] Kat {lastPickedUp.PickupFloor}: Yolcu bindi, {targetFloor}. kata gidecek");
                }
            }
        }
        
        // ==================== ASANSÖR 1 İŞLEMLERİ ====================
        
        private async Task HandleSameFloorPickup(PassengerRequest request)
        {
            if (_passengerCount >= MaxCapacity)
            {
                AddStatusMessage($"Kat {_currentFloor}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                request.Status = RequestStatus.Completed;
                return;
            }

            _elevator.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay = "Kapı Açılıyor";
            await AnimateDoor(0.0, 1.0, ElevatorModel.DoorOperationTime);

            _elevator.State = Models.ElevatorState.WaitingForPassenger;
            ElevatorStateDisplay = "Yolcu Biniyor";
            await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

            request.Status = RequestStatus.PickedUp;
            PassengerCount++;

            var waitTimeSeconds = (int)(DateTime.Now - request.RequestTime).TotalSeconds;
            request.WaitTimeSeconds = waitTimeSeconds;

            int floorDistance = Math.Abs(request.PickupFloor - request.ElevatorFloorAtRequest);

            _mlDataCollector.RecordRequest(
                CurrentSimulationTime,
                request.PickupFloor,
                request.ElevatorFloorAtRequest,
                -1,
                floorDistance,
                "Asansör 1",
                waitTimeSeconds,
                _passengerCount - 1,
                "PickedUp"
            );

            AddStatusMessage($"[Asansör 1] Kat {_currentFloor}: Yolcu bindi (Bekleme: {waitTimeSeconds}s) | Doluluk: {Elevator1LoadStatus} ({Elevator1LoadPercentage})");

            // Sayacı güncelle
            if (_floorCallCounts.ContainsKey(_currentFloor))
            {
                _floorCallCounts[_currentFloor] = Math.Max(0, _floorCallCounts[_currentFloor] - 1);
                if (_floorCallCounts[_currentFloor] == 0)
                {
                    _floorCallCounts.Remove(_currentFloor);
                }
            }

            // Hedef kat seçimi için paneli aç
            IsInnerPanelOpen = true;

            int waitCount = 0;
            while (request.DestinationFloor == -1 && waitCount < 300)
            {
                await Task.Delay(100);
                waitCount++;
            }

            // Hedef kat seçildiyse kaydet; seçilmediyse yolcuyu indir
            if (request.DestinationFloor != -1)
            {
                _destinationFloors.Add(request.DestinationFloor);
            }
            else
            {
                // Süre doldu, yolcu kat seçmedi → asansörden at
                request.Status = RequestStatus.Completed;
                PassengerCount--;
                AddStatusMessage($"[Asansör 1] Kat {_currentFloor}: Yolcu hedef seçmedi, asansörden çıkarıldı!");
            }

            IsInnerPanelOpen = false;

            ElevatorStateDisplay = "Kapı Kapanıyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor(1.0, 0.0, ElevatorModel.DoorOperationTime);

            if (request.DestinationFloor != -1 && !_isProcessingRequests)
            {
                await ProcessRequests();
            }
        }
        
        private async Task ProcessRequests()
        {
            _isProcessingRequests = true;
            
            while (_pendingRequests.Any(r => r.Status != RequestStatus.Completed) || 
                   _destinationFloors.Count > 0)
            {
                var direction = DetermineDirection();
                
                if (direction == 0)
                {
                    _elevator.State = Models.ElevatorState.Idle;
                    ElevatorStateDisplay = "Beklemede";
                    await Task.Delay(500);
                    continue;
                }
                
                var stopsInDirection = GetAllStopsInDirection(direction);
                
                if (!stopsInDirection.Any())
                {
                    await Task.Delay(100);
                    continue;
                }
                
                var nextStop = stopsInDirection.First();
                
                _elevator.State = direction > 0 
                    ? Models.ElevatorState.MovingUp 
                    : Models.ElevatorState.MovingDown;
                ElevatorStateDisplay = direction > 0 ? "Yukarı Gidiyor" : "Aşağı Gidiyor";

                // Ağırlık bazlı hız ayarı
                double travelTime = CalculateFloorTravelTime(_passengerCount);
                await Task.Delay(TimeSpan.FromSeconds(travelTime));

                if (direction > 0)
                {
                    _elevator.CurrentFloor++;
                    _elevator1TotalDistance++;
                }
                else
                {
                    _elevator.CurrentFloor--;
                    _elevator1TotalDistance++;
                }
                
                CurrentFloor = _elevator.CurrentFloor;
                
                if (_elevator.CurrentFloor == nextStop)
                {
                    bool hasDropOff = _destinationFloors.Contains(nextStop);
                    var pickupRequests = _pendingRequests
                        .Where(r => r.PickupFloor == nextStop && r.Status == RequestStatus.Pending)
                        .ToList();
                    
                    if (hasDropOff || pickupRequests.Any())
                    {
                        await HandleStopOperations(nextStop, hasDropOff, pickupRequests);
                    }
                }
            }
            
            _isProcessingRequests = false;
            _elevator.State = Models.ElevatorState.Idle;
            ElevatorStateDisplay = "Beklemede";
            AddStatusMessage("Tüm istekler tamamlandı");
        }
        
        private List<int> GetAllStopsInDirection(int direction)
        {
            var stops = new HashSet<int>();
            
            if (direction > 0)
            {
                foreach (var floor in _destinationFloors.Where(f => f > _currentFloor))
                {
                    stops.Add(floor);
                }
                
                foreach (var request in _pendingRequests.Where(r => r.Status == RequestStatus.Pending && r.PickupFloor > _currentFloor))
                {
                    stops.Add(request.PickupFloor);
                }
                
                return stops.OrderBy(f => f).ToList();
            }
            else if (direction < 0)
            {
                foreach (var floor in _destinationFloors.Where(f => f < _currentFloor))
                {
                    stops.Add(floor);
                }
                
                foreach (var request in _pendingRequests.Where(r => r.Status == RequestStatus.Pending && r.PickupFloor < _currentFloor))
                {
                    stops.Add(request.PickupFloor);
                }
                
                return stops.OrderByDescending(f => f).ToList();
            }
            
            return new List<int>();
        }
        
        private int DetermineDirection()
        {
            // Oncelik 1: Icerideki yolcularin hedef katlari (drop-off)
            // Asansorde yolcu varsa once onlari birak.
            // Dis cagrilar ters yonde ise yolcular inene kadar beklesin.
            if (_destinationFloors.Any())
            {
                bool dropOffUp   = _destinationFloors.Any(f => f > _currentFloor);
                bool dropOffDown = _destinationFloors.Any(f => f < _currentFloor);

                if (_elevator.State == Models.ElevatorState.MovingUp   && dropOffUp)   return  1;
                if (_elevator.State == Models.ElevatorState.MovingDown && dropOffDown) return -1;

                if (dropOffUp   && !dropOffDown) return  1;
                if (dropOffDown && !dropOffUp)   return -1;

                var nearUp   = _destinationFloors.Where(f => f > _currentFloor).Min();
                var nearDown = _destinationFloors.Where(f => f < _currentFloor).Max();
                return (nearUp - _currentFloor) <= (_currentFloor - nearDown) ? 1 : -1;
            }

            // Oncelik 2: Iceride yolcu yok - pickup isteklerine bak
            var pendingPickups = _pendingRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .Select(r => r.PickupFloor)
                .ToList();

            if (!pendingPickups.Any())
                return 0;

            bool hasUp   = pendingPickups.Any(f => f > _currentFloor);
            bool hasDown = pendingPickups.Any(f => f < _currentFloor);

            if (_elevator.State == Models.ElevatorState.MovingUp   && hasUp)   return  1;
            if (_elevator.State == Models.ElevatorState.MovingDown && hasDown) return -1;

            if (hasUp   && !hasDown) return  1;
            if (hasDown && !hasUp)   return -1;

            if (hasUp && hasDown)
            {
                var closestUp   = pendingPickups.Where(f => f > _currentFloor).Min();
                var closestDown = pendingPickups.Where(f => f < _currentFloor).Max();
                return (closestUp - _currentFloor) <= (_currentFloor - closestDown) ? 1 : -1;
            }

            return 0;
        }
        private async Task HandleStopOperations(int floor, bool hasDropOff, List<PassengerRequest> pickupRequests)
        {
            _elevator.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay = "Kapı Açılıyor";
            await AnimateDoor(0.0, 1.0, ElevatorModel.DoorOperationTime);
            
            if (hasDropOff)
            {
                _elevator.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay = "Yolcu İniyor";
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));
                
                var completedRequests = _pendingRequests
                    .Where(r => r.Status == RequestStatus.PickedUp && r.DestinationFloor == floor)
                    .ToList();
                
                foreach (var req in completedRequests)
                {
                    req.Status = RequestStatus.Completed;
                    PassengerCount--;
                    
                    var totalTravelTime = (int)(DateTime.Now - req.RequestTime).TotalSeconds;
                    int floorDistance = Math.Abs(req.DestinationFloor - req.PickupFloor);
                    
                    _mlDataCollector.RecordRequest(
                        CurrentSimulationTime,
                        req.PickupFloor,
                        req.ElevatorFloorAtRequest,
                        req.DestinationFloor,
                        floorDistance,
                        "Asansör 1",
                        totalTravelTime,
                        _passengerCount,
                        "Completed"
                    );
                    
                    AddStatusMessage($"Kat {floor}: Yolcu indi (Toplam süre: {totalTravelTime} saniye, Mesafe: {floorDistance} kat)");
                }
                
                _destinationFloors.Remove(floor);
            }
            
            if (pickupRequests.Any() && _passengerCount < MaxCapacity)
            {
                _elevator.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay = "Yolcu Biniyor";
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                int pickupCount = 0;
                int remainingPassengers = 0;

                // Önce tüm yolcuları bin
                foreach (var request in pickupRequests)
                {
                    if (_passengerCount >= MaxCapacity)
                    {
                        remainingPassengers++;
                        AddStatusMessage($"[Asansör 1] Kat {floor}: Kapasite dolu, yolcu alınamadı");
                        continue;
                    }

                    request.Status = RequestStatus.PickedUp;
                    PassengerCount++;
                    pickupCount++;

                    var waitTimeSeconds = (int)(DateTime.Now - request.RequestTime).TotalSeconds;
                    request.WaitTimeSeconds = waitTimeSeconds;

                    int floorDistance = Math.Abs(request.PickupFloor - request.ElevatorFloorAtRequest);

                    _mlDataCollector.RecordRequest(
                        CurrentSimulationTime,
                        request.PickupFloor,
                        request.ElevatorFloorAtRequest,
                        -1,
                        floorDistance,
                        "Asansör 1",
                        waitTimeSeconds,
                        _passengerCount - 1,
                        "PickedUp"
                    );

                    AddStatusMessage($"[Asansör 1] Kat {floor}: Yolcu bindi (Bekleme: {waitTimeSeconds}s) | Doluluk: {Elevator1LoadStatus} ({Elevator1LoadPercentage})");
                }

                // Şimdi TÜM yolcular için hedef kat seçimini bekle
                IsInnerPanelOpen = true;

                var pickedUpPassengers = pickupRequests.Where(r => r.Status == RequestStatus.PickedUp).ToList();

                if (pickedUpPassengers.Any())
                {
                    Elevator1WaitingPassengers = pickedUpPassengers.Count;
                    AddStatusMessage($"[Asansör 1] Kat {floor}: {pickedUpPassengers.Count} yolcu hedef kat seçiyor...");

                    int maxWaitCount = 300; // 30 saniye toplam
                    int waitCount = 0;

                    // Tüm yolcular hedef katlarını seçene kadar bekle
                    while (pickedUpPassengers.Any(r => r.DestinationFloor == -1) && waitCount < maxWaitCount)
                    {
                        await Task.Delay(100);
                        waitCount++;

                        // Progress güncelle (kaç kişi seçti)
                        int selectedCount = pickedUpPassengers.Count(r => r.DestinationFloor != -1);
                        Elevator1WaitingPassengers = pickedUpPassengers.Count - selectedCount;
                    }

                    // Hedef katlarını seçenleri kaydet; seçmeyenleri asansörden at
                    foreach (var passenger in pickedUpPassengers)
                    {
                        if (passenger.DestinationFloor != -1)
                        {
                            _destinationFloors.Add(passenger.DestinationFloor);
                        }
                        else
                        {
                            // Süre doldu, yolcu kat seçmedi → asansörden at
                            passenger.Status = RequestStatus.Completed;
                            PassengerCount--;
                            AddStatusMessage($"[Asansör 1] Kat {floor}: Yolcu hedef seçmedi, asansörden çıkarıldı!");
                        }
                    }
                }

                Elevator1WaitingPassengers = 0;
                IsInnerPanelOpen = false;

                // Bu kattan alınan yolcu sayısı kadar sayacı azalt
                if (_floorCallCounts.ContainsKey(floor))
                {
                    _floorCallCounts[floor] = Math.Max(0, _floorCallCounts[floor] - pickupCount);
                    if (_floorCallCounts[floor] == 0)
                    {
                        _floorCallCounts.Remove(floor);
                    }
                }

                // Eğer kapasite doldu ve hala bekleyen yolcular varsa, Asansör 2'yi çağır
                if (remainingPassengers > 0 && !_isProcessingRequests2)
                {
                    // Bekleyen istekleri Asansör 2'ye aktar
                    var unhandledRequests = pickupRequests.Where(r => r.Status == RequestStatus.Pending).ToList();
                    if (unhandledRequests.Any())
                    {
                        AddStatusMessage($"[Asansör 1] Kat {floor}: {remainingPassengers} yolcu için Asansör 2 çağrılıyor");

                        // Asansör 2'nin bu kata gönderilmediğinden emin ol
                        bool elevator2AlreadyAssigned = _pendingRequests2.Any(r => 
                            r.PickupFloor == floor && r.Status == RequestStatus.Pending);

                        if (!elevator2AlreadyAssigned)
                        {
                            // İlk bekleyen isteği Asansör 2'ye aktar
                            var firstUnhandled = unhandledRequests.First();
                            firstUnhandled.Status = RequestStatus.Pending; // Reset status
                            _pendingRequests.Remove(firstUnhandled);
                            _pendingRequests2.Add(firstUnhandled);

                            // Asansör 2'nin işlemeye başlamasını tetikle
                            if (!_isProcessingRequests2)
                            {
                                _ = ProcessRequests2();
                            }
                        }
                    }
                }
            }
            
            ElevatorStateDisplay = "Kapı Kapanıyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));
            
            _elevator.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor(1.0, 0.0, ElevatorModel.DoorOperationTime);
        }
        
        private async Task AnimateDoor(double from, double to, double durationSeconds)
        {
            int steps = 20;
            double increment = (to - from) / steps;
            double delay = (durationSeconds * 1000) / steps;
            
            for (int i = 0; i < steps; i++)
            {
                DoorOpenAmount = from + (increment * i);
                await Task.Delay((int)delay);
            }
            
            DoorOpenAmount = to;
        }
        
        // ==================== ASANSÖR 2 İŞLEMLERİ ====================
        
        private async Task HandleSameFloorPickup2(PassengerRequest request)
        {
            if (_passengerCount2 >= MaxCapacity)
            {
                AddStatusMessage($"[Asansör 2] Kat {_currentFloor2}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                request.Status = RequestStatus.Completed;
                return;
            }

            _elevator2.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay2 = "Kapı Açılıyor";
            await AnimateDoor2(0.0, 1.0, ElevatorModel.DoorOperationTime);

            _elevator2.State = Models.ElevatorState.WaitingForPassenger;
            ElevatorStateDisplay2 = "Yolcu Biniyor";
            await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

            request.Status = RequestStatus.PickedUp;
            PassengerCount2++;

            var waitTimeSeconds = (int)(DateTime.Now - request.RequestTime).TotalSeconds;
            request.WaitTimeSeconds = waitTimeSeconds;

            int floorDistance = Math.Abs(request.PickupFloor - request.ElevatorFloorAtRequest);

            _mlDataCollector.RecordRequest(
                CurrentSimulationTime,
                request.PickupFloor,
                request.ElevatorFloorAtRequest,
                -1,
                floorDistance,
                "Asansör 2",
                waitTimeSeconds,
                _passengerCount2 - 1,
                "PickedUp"
            );

            AddStatusMessage($"[Asansör 2] Kat {_currentFloor2}: Yolcu bindi (Bekleme: {waitTimeSeconds}s) | Doluluk: {Elevator2LoadStatus} ({Elevator2LoadPercentage})");

            // Sayacı güncelle
            if (_floorCallCounts.ContainsKey(_currentFloor2))
            {
                _floorCallCounts[_currentFloor2] = Math.Max(0, _floorCallCounts[_currentFloor2] - 1);
                if (_floorCallCounts[_currentFloor2] == 0)
                {
                    _floorCallCounts.Remove(_currentFloor2);
                }
            }

            // Hedef kat seçimi için paneli aç
            IsInnerPanelOpen2 = true;

            int waitCount = 0;
            while (request.DestinationFloor == -1 && waitCount < 300)
            {
                await Task.Delay(100);
                waitCount++;
            }

            // Hedef kat seçildiyse kaydet; seçilmediyse yolcuyu indir
            if (request.DestinationFloor != -1)
            {
                _destinationFloors2.Add(request.DestinationFloor);
            }
            else
            {
                // Süre doldu, yolcu kat seçmedi → asansörden at
                request.Status = RequestStatus.Completed;
                PassengerCount2--;
                AddStatusMessage($"[Asansör 2] Kat {_currentFloor2}: Yolcu hedef seçmedi, asansörden çıkarıldı!");
            }

            IsInnerPanelOpen2 = false;

            ElevatorStateDisplay2 = "Kapı Kapanıyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator2.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor2(1.0, 0.0, ElevatorModel.DoorOperationTime);

            if (request.DestinationFloor != -1 && !_isProcessingRequests2)
            {
                await ProcessRequests2();
            }
        }
        
        private async Task ProcessRequests2()
        {
            _isProcessingRequests2 = true;
            
            while (_pendingRequests2.Any(r => r.Status != RequestStatus.Completed) || 
                   _destinationFloors2.Count > 0)
            {
                var direction = DetermineDirection2();
                
                if (direction == 0)
                {
                    _elevator2.State = Models.ElevatorState.Idle;
                    ElevatorStateDisplay2 = "Beklemede";
                    await Task.Delay(500);
                    continue;
                }
                
                var stopsInDirection = GetAllStopsInDirection2(direction);
                
                if (!stopsInDirection.Any())
                {
                    await Task.Delay(100);
                    continue;
                }
                
                var nextStop = stopsInDirection.First();
                
                _elevator2.State = direction > 0 
                    ? Models.ElevatorState.MovingUp 
                    : Models.ElevatorState.MovingDown;
                ElevatorStateDisplay2 = direction > 0 ? "Yukarı Gidiyor" : "Aşağı Gidiyor";

                // Ağırlık bazlı hız ayarı
                double travelTime = CalculateFloorTravelTime(_passengerCount2);
                await Task.Delay(TimeSpan.FromSeconds(travelTime));

                if (direction > 0)
                {
                    _elevator2.CurrentFloor++;
                    _elevator2TotalDistance++;
                }
                else
                {
                    _elevator2.CurrentFloor--;
                    _elevator2TotalDistance++;
                }
                
                CurrentFloor2 = _elevator2.CurrentFloor;
                
                if (_elevator2.CurrentFloor == nextStop)
                {
                    bool hasDropOff = _destinationFloors2.Contains(nextStop);
                    var pickupRequests = _pendingRequests2
                        .Where(r => r.PickupFloor == nextStop && r.Status == RequestStatus.Pending)
                        .ToList();
                    
                    if (hasDropOff || pickupRequests.Any())
                    {
                        await HandleStopOperations2(nextStop, hasDropOff, pickupRequests);
                    }
                }
            }
            
            _isProcessingRequests2 = false;
            _elevator2.State = Models.ElevatorState.Idle;
            ElevatorStateDisplay2 = "Beklemede";
            AddStatusMessage("[Asansör 2] Tüm istekler tamamlandı");
        }
        
        private List<int> GetAllStopsInDirection2(int direction)
        {
            var stops = new HashSet<int>();
            
            if (direction > 0)
            {
                foreach (var floor in _destinationFloors2.Where(f => f > _currentFloor2))
                {
                    stops.Add(floor);
                }
                
                foreach (var request in _pendingRequests2.Where(r => r.Status == RequestStatus.Pending && r.PickupFloor > _currentFloor2))
                {
                    stops.Add(request.PickupFloor);
                }
                
                return stops.OrderBy(f => f).ToList();
            }
            else if (direction < 0)
            {
                foreach (var floor in _destinationFloors2.Where(f => f < _currentFloor2))
                {
                    stops.Add(floor);
                }
                
                foreach (var request in _pendingRequests2.Where(r => r.Status == RequestStatus.Pending && r.PickupFloor < _currentFloor2))
                {
                    stops.Add(request.PickupFloor);
                }
                
                return stops.OrderByDescending(f => f).ToList();
            }
            
            return new List<int>();
        }
        
        private int DetermineDirection2()
        {
            // Oncelik 1: Icerideki yolcularin hedef katlari (drop-off)
            // Asansorde yolcu varsa once onlari birak.
            // Dis cagrilar ters yonde ise yolcular inene kadar beklesin.
            if (_destinationFloors2.Any())
            {
                bool dropOffUp   = _destinationFloors2.Any(f => f > _currentFloor2);
                bool dropOffDown = _destinationFloors2.Any(f => f < _currentFloor2);

                if (_elevator2.State == Models.ElevatorState.MovingUp   && dropOffUp)   return  1;
                if (_elevator2.State == Models.ElevatorState.MovingDown && dropOffDown) return -1;

                if (dropOffUp   && !dropOffDown) return  1;
                if (dropOffDown && !dropOffUp)   return -1;

                var nearUp   = _destinationFloors2.Where(f => f > _currentFloor2).Min();
                var nearDown = _destinationFloors2.Where(f => f < _currentFloor2).Max();
                return (nearUp - _currentFloor2) <= (_currentFloor2 - nearDown) ? 1 : -1;
            }

            // Oncelik 2: Iceride yolcu yok - pickup isteklerine bak
            var pendingPickups = _pendingRequests2
                .Where(r => r.Status == RequestStatus.Pending)
                .Select(r => r.PickupFloor)
                .ToList();

            if (!pendingPickups.Any())
                return 0;

            bool hasUp   = pendingPickups.Any(f => f > _currentFloor2);
            bool hasDown = pendingPickups.Any(f => f < _currentFloor2);

            if (_elevator2.State == Models.ElevatorState.MovingUp   && hasUp)   return  1;
            if (_elevator2.State == Models.ElevatorState.MovingDown && hasDown) return -1;

            if (hasUp   && !hasDown) return  1;
            if (hasDown && !hasUp)   return -1;

            if (hasUp && hasDown)
            {
                var closestUp   = pendingPickups.Where(f => f > _currentFloor2).Min();
                var closestDown = pendingPickups.Where(f => f < _currentFloor2).Max();
                return (closestUp - _currentFloor2) <= (_currentFloor2 - closestDown) ? 1 : -1;
            }

            return 0;
        }
        
        private async Task HandleStopOperations2(int floor, bool hasDropOff, List<PassengerRequest> pickupRequests)
        {
            _elevator2.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay2 = "Kapı Açılıyor";
            await AnimateDoor2(0.0, 1.0, ElevatorModel.DoorOperationTime);
            
            if (hasDropOff)
            {
                _elevator2.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay2 = "Yolcu İniyor";
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));
                
                var completedRequests = _pendingRequests2
                    .Where(r => r.Status == RequestStatus.PickedUp && r.DestinationFloor == floor)
                    .ToList();
                
                foreach (var req in completedRequests)
                {
                    req.Status = RequestStatus.Completed;
                    PassengerCount2--;
                    
                    var totalTravelTime = (int)(DateTime.Now - req.RequestTime).TotalSeconds;
                    int floorDistance = Math.Abs(req.DestinationFloor - req.PickupFloor);
                    
                    _mlDataCollector.RecordRequest(
                        CurrentSimulationTime,
                        req.PickupFloor,
                        req.ElevatorFloorAtRequest,
                        req.DestinationFloor,
                        floorDistance,
                        "Asansör 2",
                        totalTravelTime,
                        _passengerCount2,
                        "Completed"
                    );
                    
                    AddStatusMessage($"[Asansör 2] Kat {floor}: Yolcu indi (Toplam süre: {totalTravelTime} saniye, Mesafe: {floorDistance} kat)");
                }
                
                _destinationFloors2.Remove(floor);
            }
            
            if (pickupRequests.Any() && _passengerCount2 < MaxCapacity)
            {
                _elevator2.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay2 = "Yolcu Biniyor";
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                int pickupCount = 0;
                int remainingPassengers = 0;

                // Önce tüm yolcuları bin
                foreach (var request in pickupRequests)
                {
                    if (_passengerCount2 >= MaxCapacity)
                    {
                        remainingPassengers++;
                        AddStatusMessage($"[Asansör 2] Kat {floor}: Kapasite dolu, yolcu alınamadı");
                        continue;
                    }

                    request.Status = RequestStatus.PickedUp;
                    PassengerCount2++;
                    pickupCount++;

                    var waitTimeSeconds = (int)(DateTime.Now - request.RequestTime).TotalSeconds;
                    request.WaitTimeSeconds = waitTimeSeconds;

                    int floorDistance = Math.Abs(request.PickupFloor - request.ElevatorFloorAtRequest);

                    _mlDataCollector.RecordRequest(
                        CurrentSimulationTime,
                        request.PickupFloor,
                        request.ElevatorFloorAtRequest,
                        -1,
                        floorDistance,
                        "Asansör 2",
                        waitTimeSeconds,
                        _passengerCount2 - 1,
                        "PickedUp"
                    );

                    AddStatusMessage($"[Asansör 2] Kat {floor}: Yolcu bindi (Bekleme: {waitTimeSeconds}s) | Doluluk: {Elevator2LoadStatus} ({Elevator2LoadPercentage})");
                }

                // Şimdi TÜM yolcular için hedef kat seçimini bekle
                IsInnerPanelOpen2 = true;

                var pickedUpPassengers = pickupRequests.Where(r => r.Status == RequestStatus.PickedUp).ToList();

                if (pickedUpPassengers.Any())
                {
                    Elevator2WaitingPassengers = pickedUpPassengers.Count;
                    AddStatusMessage($"[Asansör 2] Kat {floor}: {pickedUpPassengers.Count} yolcu hedef kat seçiyor...");

                    int maxWaitCount = 300; // 30 saniye toplam
                    int waitCount = 0;

                    // Tüm yolcular hedef katlarını seçene kadar bekle
                    while (pickedUpPassengers.Any(r => r.DestinationFloor == -1) && waitCount < maxWaitCount)
                    {
                        await Task.Delay(100);
                        waitCount++;

                        // Progress güncelle (kaç kişi seçti)
                        int selectedCount = pickedUpPassengers.Count(r => r.DestinationFloor != -1);
                        Elevator2WaitingPassengers = pickedUpPassengers.Count - selectedCount;
                    }

                    // Hedef katlarını seçenleri kaydet; seçmeyenleri asansörden at
                    foreach (var passenger in pickedUpPassengers)
                    {
                        if (passenger.DestinationFloor != -1)
                        {
                            _destinationFloors2.Add(passenger.DestinationFloor);
                        }
                        else
                        {
                            // Süre doldu, yolcu kat seçmedi → asansörden at
                            passenger.Status = RequestStatus.Completed;
                            PassengerCount2--;
                            AddStatusMessage($"[Asansör 2] Kat {floor}: Yolcu hedef seçmedi, asansörden çıkarıldı!");
                        }
                    }
                }

                Elevator2WaitingPassengers = 0;
                IsInnerPanelOpen2 = false;

                // Bu kattan alınan yolcu sayısı kadar sayacı azalt
                if (_floorCallCounts.ContainsKey(floor))
                {
                    _floorCallCounts[floor] = Math.Max(0, _floorCallCounts[floor] - pickupCount);
                    if (_floorCallCounts[floor] == 0)
                    {
                        _floorCallCounts.Remove(floor);
                    }
                }

                // Eğer kapasite doldu ve hala bekleyen yolcular varsa, Asansör 1'i çağır
                if (remainingPassengers > 0 && !_isProcessingRequests)
                {
                    // Bekleyen istekleri Asansör 1'e aktar
                    var unhandledRequests = pickupRequests.Where(r => r.Status == RequestStatus.Pending).ToList();
                    if (unhandledRequests.Any())
                    {
                        AddStatusMessage($"[Asansör 2] Kat {floor}: {remainingPassengers} yolcu için Asansör 1 çağrılıyor");

                        // Asansör 1'in bu kata gönderilmediğinden emin ol
                        bool elevator1AlreadyAssigned = _pendingRequests.Any(r => 
                            r.PickupFloor == floor && r.Status == RequestStatus.Pending);

                        if (!elevator1AlreadyAssigned)
                        {
                            // İlk bekleyen isteği Asansör 1'e aktar
                            var firstUnhandled = unhandledRequests.First();
                            firstUnhandled.Status = RequestStatus.Pending; // Reset status
                            _pendingRequests2.Remove(firstUnhandled);
                            _pendingRequests.Add(firstUnhandled);

                            // Asansör 1'in işlemeye başlamasını tetikle
                            if (!_isProcessingRequests)
                            {
                                _ = ProcessRequests();
                            }
                        }
                    }
                }
            }
            
            ElevatorStateDisplay2 = "Kapı Kapanıyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));
            
            _elevator2.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor2(1.0, 0.0, ElevatorModel.DoorOperationTime);
        }
        
        private async Task AnimateDoor2(double from, double to, double durationSeconds)
        {
            int steps = 20;
            double increment = (to - from) / steps;
            double delay = (durationSeconds * 1000) / steps;
            
            for (int i = 0; i < steps; i++)
            {
                DoorOpenAmount2 = from + (increment * i);
                await Task.Delay((int)delay);
            }
            
            DoorOpenAmount2 = to;
        }
        
        // ==================== TIMER ====================

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isSimulationRunning)
            {
                var elapsed = DateTime.Now - _simulationStartTime;
                TotalTime = elapsed.ToString(@"hh\:mm\:ss");

                // Enerji tasarrufu kontrolü
                CheckEnergyModeAndPark();

                // Uzun bekleyen yolcuları kontrol et (her saniye)
                if (elapsed.Milliseconds % 1000 < 100)
                {
                    CheckLongWaitingPassengers();
                }
            }
        }

        /// <summary>
        /// Boşta kalan asansörleri enerji tasarrufu için zemin kata gönderir
        /// </summary>
        private void CheckEnergyModeAndPark()
        {
            // Asansör 1 kontrolü
            if (_elevator.State == Models.ElevatorState.Idle && !_isProcessingRequests)
            {
                if (_elevator1IdleStartTime == null)
                {
                    _elevator1IdleStartTime = DateTime.Now;
                }
                else
                {
                    var idleDuration = DateTime.Now - _elevator1IdleStartTime.Value;
                    if (idleDuration >= _idleTimeBeforeParking && _currentFloor != ParkingFloor)
                    {
                        AddStatusMessage($"🔋 [Asansör 1] Enerji tasarrufu: {(int)idleDuration.TotalSeconds}s boşta, zemin kata dönüyor");
                        _ = ParkElevator(1);
                        _elevator1IdleStartTime = null;
                    }
                }
            }
            else
            {
                _elevator1IdleStartTime = null;
            }

            // Asansör 2 kontrolü
            if (_elevator2.State == Models.ElevatorState.Idle && !_isProcessingRequests2)
            {
                if (_elevator2IdleStartTime == null)
                {
                    _elevator2IdleStartTime = DateTime.Now;
                }
                else
                {
                    var idleDuration = DateTime.Now - _elevator2IdleStartTime.Value;
                    if (idleDuration >= _idleTimeBeforeParking && _currentFloor2 != ParkingFloor)
                    {
                        AddStatusMessage($"🔋 [Asansör 2] Enerji tasarrufu: {(int)idleDuration.TotalSeconds}s boşta, zemin kata dönüyor");
                        _ = ParkElevator(2);
                        _elevator2IdleStartTime = null;
                    }
                }
            }
            else
            {
                _elevator2IdleStartTime = null;
            }
        }

        /// <summary>
        /// Asansörü zemin kata park eder (enerji tasarrufu)
        /// </summary>
        private async Task ParkElevator(int elevatorNumber)
        {
            if (elevatorNumber == 1)
            {
                if (_currentFloor == ParkingFloor || _isProcessingRequests)
                    return;

                _elevator.State = _currentFloor > ParkingFloor 
                    ? Models.ElevatorState.MovingDown 
                    : Models.ElevatorState.MovingUp;
                ElevatorStateDisplay = "Park Modunda";

                while (_currentFloor != ParkingFloor)
                {
                    // Eğer yeni bir çağrı gelirse park işlemini iptal et
                    if (_pendingRequests.Any() || _isProcessingRequests)
                    {
                        AddStatusMessage($"🔋 [Asansör 1] Park işlemi iptal edildi (Yeni çağrı var)");
                        return;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.FloorTravelTime));

                    if (_currentFloor > ParkingFloor)
                    {
                        _elevator.CurrentFloor--;
                    }
                    else
                    {
                        _elevator.CurrentFloor++;
                    }

                    CurrentFloor = _elevator.CurrentFloor;
                }

                _elevator.State = Models.ElevatorState.Idle;
                ElevatorStateDisplay = "Park Edildi (Zemin Kat)";
                AddStatusMessage($"🔋 [Asansör 1] Zemin kata park edildi");
            }
            else if (elevatorNumber == 2)
            {
                if (_currentFloor2 == ParkingFloor || _isProcessingRequests2)
                    return;

                _elevator2.State = _currentFloor2 > ParkingFloor 
                    ? Models.ElevatorState.MovingDown 
                    : Models.ElevatorState.MovingUp;
                ElevatorStateDisplay2 = "Park Modunda";

                while (_currentFloor2 != ParkingFloor)
                {
                    // Eğer yeni bir çağrı gelirse park işlemini iptal et
                    if (_pendingRequests2.Any() || _isProcessingRequests2)
                    {
                        AddStatusMessage($"🔋 [Asansör 2] Park işlemi iptal edildi (Yeni çağrı var)");
                        return;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.FloorTravelTime));

                    if (_currentFloor2 > ParkingFloor)
                    {
                        _elevator2.CurrentFloor--;
                    }
                    else
                    {
                        _elevator2.CurrentFloor++;
                    }

                    CurrentFloor2 = _elevator2.CurrentFloor;
                }

                _elevator2.State = Models.ElevatorState.Idle;
                ElevatorStateDisplay2 = "Park Edildi (Zemin Kat)";
                AddStatusMessage($"🔋 [Asansör 2] Zemin kata park edildi");
            }
        }

        // ==================== EMERGENCY STOP ====================

        /// <summary>
        /// Acil durum - Tüm asansörleri durdur ve zemin kata gönder
        /// </summary>
        private async Task ExecuteEmergencyStop()
        {
            AddStatusMessage("🚨 ACİL DURUM AKTİF! Tüm asansörler durduruluyor...");

            // 📊 ML KAYDI: Acil durum başlangıç durumu - Asansör 1
            int totalPassengers = _passengerCount + _passengerCount2;
            int elevator1Distance = Math.Abs(_currentFloor - 0);
            int elevator2Distance = Math.Abs(_currentFloor2 - 0);

            _mlDataCollector.RecordRequest(
                CurrentSimulationTime,
                _currentFloor,           // Asansör 1'in bulunduğu kat
                _currentFloor2,          // Asansör 2'nin bulunduğu kat (Hedef_Kat alanında)
                0,                       // Hedef: Zemin kat (Kat_Mesafesi için)
                elevator1Distance,       // Asansör 1'in zemin kata mesafesi
                "ACİL DURUM - Asansör 1",
                0,                       // Bekleme süresi yok
                _passengerCount,         // Asansör 1'deki yolcu sayısı
                $"Emergency_Triggered_Floor{_currentFloor}_Passengers{_passengerCount}"
            );

            // 📊 ML KAYDI: Acil durum başlangıç durumu - Asansör 2
            _mlDataCollector.RecordRequest(
                CurrentSimulationTime,
                _currentFloor2,          // Asansör 2'nin bulunduğu kat
                _currentFloor,           // Asansör 1'in bulunduğu kat (Hedef_Kat alanında)
                0,                       // Hedef: Zemin kat
                elevator2Distance,       // Asansör 2'nin zemin kata mesafesi
                "ACİL DURUM - Asansör 2",
                0,                       // Bekleme süresi yok
                _passengerCount2,        // Asansör 2'deki yolcu sayısı
                $"Emergency_Triggered_Floor{_currentFloor2}_Passengers{_passengerCount2}"
            );

            AddStatusMessage($"📊 ML: Acil durum kaydedildi - Toplam {totalPassengers} yolcu, Asansör 1: Kat {_currentFloor}, Asansör 2: Kat {_currentFloor2}");

            // Simülasyonu duraklat
            _isSimulationRunning = false;

            // Tüm bekleyen istekleri iptal et
            _pendingRequests.Clear();
            _pendingRequests2.Clear();
            _destinationFloors.Clear();
            _destinationFloors2.Clear();
            _floorCallCounts.Clear();

            // Panel kapalıysa kapat
            IsInnerPanelOpen = false;
            IsInnerPanelOpen2 = false;

            // İşlem bayraklarını kapat
            _isProcessingRequests = false;
            _isProcessingRequests2 = false;

            // Asansör durumlarını güncelle
            ElevatorStateDisplay = "ACİL DURUM - Zemin kata iniyor";
            ElevatorStateDisplay2 = "ACİL DURUM - Zemin kata iniyor";

            // Her iki asansörü de zemin kata gönder
            var emergencyStartTime = DateTime.Now;
            await Task.WhenAll(
                EmergencyMoveToGroundFloor(_elevator, 1),
                EmergencyMoveToGroundFloor(_elevator2, 2)
            );
            var emergencyDuration = (int)(DateTime.Now - emergencyStartTime).TotalSeconds;

            // 📊 ML KAYDI: Acil durum tamamlandı - Asansör 1
            _mlDataCollector.RecordRequest(
                CurrentSimulationTime,
                0,                       // Şimdi zemin katta
                _currentFloor,           // Başlangıç katı (Hedef_Kat alanında)
                0,                       // Hedef: Zemin kat
                elevator1Distance,       // Kat edilen mesafe
                "ACİL DURUM - Asansör 1",
                emergencyDuration,       // Acil durum süresi
                0,                       // Yolcular boşaltıldı
                $"Emergency_Completed_Duration{emergencyDuration}s"
            );

            // 📊 ML KAYDI: Acil durum tamamlandı - Asansör 2
            _mlDataCollector.RecordRequest(
                CurrentSimulationTime,
                0,                       // Şimdi zemin katta
                _currentFloor2,          // Başlangıç katı (Hedef_Kat alanında)
                0,                       // Hedef: Zemin kat
                elevator2Distance,       // Kat edilen mesafe
                "ACİL DURUM - Asansör 2",
                emergencyDuration,       // Acil durum süresi
                0,                       // Yolcular boşaltıldı
                $"Emergency_Completed_Duration{emergencyDuration}s"
            );

            // İşlem tamamlandı
            AddStatusMessage($"✅ Acil durum tamamlandı ({emergencyDuration}s). Tüm asansörler zemin katta güvende.");
            AddStatusMessage($"📊 ML: Acil durum tamamlama kaydedildi - Süre: {emergencyDuration}s");
            ElevatorStateDisplay = "Zemin Katta - Beklemede";
            ElevatorStateDisplay2 = "Zemin Katta - Beklemede";

            // Simülasyonu yeniden başlat
            await Task.Delay(2000); // 2 saniye bekle
            _isSimulationRunning = true;
            AddStatusMessage("✅ Sistem normale döndü. Asansörler kullanıma hazır.");
        }

        /// <summary>
        /// Tek bir asansörü acil durumda zemin kata indir
        /// </summary>
        private async Task EmergencyMoveToGroundFloor(ElevatorModel elevator, int elevatorNumber)
        {
            int currentFloor = elevatorNumber == 1 ? CurrentFloor : CurrentFloor2;

            // Zaten zemin kattaysa işlem yapma
            if (currentFloor == 0)
            {
                return;
            }

            // Kapıları kapat (animasyon)
            if (elevatorNumber == 1)
            {
                _elevator.State = Models.ElevatorState.DoorClosing;
                await AnimateDoor(1.0, 0.0, ElevatorModel.DoorOperationTime);
            }
            else
            {
                _elevator2.State = Models.ElevatorState.DoorClosing;
                await AnimateDoor2(1.0, 0.0, ElevatorModel.DoorOperationTime);
            }

            // Zemin kata kadar in
            while (currentFloor > 0)
            {
                await Task.Delay(500); // Hızlı iniş (normal 1000ms yerine 500ms)
                currentFloor--;

                if (elevatorNumber == 1)
                {
                    CurrentFloor = currentFloor;
                    _elevator.CurrentFloor = currentFloor;
                }
                else
                {
                    CurrentFloor2 = currentFloor;
                    _elevator2.CurrentFloor = currentFloor;
                }
            }

            // Kapıları aç ve yolcuları boşalt
            if (elevatorNumber == 1)
            {
                _elevator.State = Models.ElevatorState.DoorOpening;
                await AnimateDoor(0.0, 1.0, ElevatorModel.DoorOperationTime);
            }
            else
            {
                _elevator2.State = Models.ElevatorState.DoorOpening;
                await AnimateDoor2(0.0, 1.0, ElevatorModel.DoorOperationTime);
            }

            await Task.Delay(1000);

            // Yolcuları temizle
            if (elevatorNumber == 1)
            {
                PassengerCount = 0;
            }
            else
            {
                PassengerCount2 = 0;
            }

            // Kapıları kapat
            if (elevatorNumber == 1)
            {
                _elevator.State = Models.ElevatorState.DoorClosing;
                await AnimateDoor(1.0, 0.0, ElevatorModel.DoorOperationTime);
            }
            else
            {
                _elevator2.State = Models.ElevatorState.DoorClosing;
                await AnimateDoor2(1.0, 0.0, ElevatorModel.DoorOperationTime);
            }
        }
    }
}

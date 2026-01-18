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
        private readonly ElevatorModel _elevator;
        private readonly DispatcherTimer _timer;
        private readonly MLDataCollector _mlDataCollector;
        private DateTime _simulationStartTime;
        private bool _isSimulationRunning;
        private TimeSpan _currentSimulationTime;

        private int _currentFloor;
        private string _elevatorState;
        private string _statusMessage;
        private string _totalTime;
        private int _passengerCount;
        private bool _isInnerPanelOpen;
        private double _doorOpenAmount;
        private bool _isProcessingRequests;
        private ObservableCollection<int> _passengerIcons;

        // Yolcu istekleri ve hedefler
        private readonly List<PassengerRequest> _pendingRequests = new();
        private readonly HashSet<int> _destinationFloors = new();
        private const int MaxCapacity = 10;

        public ObservableCollection<int> Floors { get; }
        public ObservableCollection<int> PassengerIcons
        {
            get => _passengerIcons;
            set => SetProperty(ref _passengerIcons, value);
        }
        public ICommand CallElevatorCommand { get; }
        public ICommand SelectDestinationCommand { get; }

        public int CurrentFloor
        {
            get => _currentFloor;
            set => SetProperty(ref _currentFloor, value);
        }

        public string ElevatorStateDisplay
        {
            get => _elevatorState;
            set => SetProperty(ref _elevatorState, value);
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
                
                // Ýkon listesini güncelle
                PassengerIcons.Clear();
                for (int i = 0; i < value; i++)
                {
                    PassengerIcons.Add(i);
                }
            }
        }

        public bool HasPassenger => _passengerCount > 0;

        public TimeSpan CurrentSimulationTime
        {
            get => _currentSimulationTime;
            set => SetProperty(ref _currentSimulationTime, value);
        }

        public bool IsInnerPanelOpen
        {
            get => _isInnerPanelOpen;
            set => SetProperty(ref _isInnerPanelOpen, value);
        }

        public double DoorOpenAmount
        {
            get => _doorOpenAmount;
            set => SetProperty(ref _doorOpenAmount, value);
        }

        public MainViewModel()
        {
            _elevator = new ElevatorModel();
            _mlDataCollector = new MLDataCollector();
            Floors = new ObservableCollection<int>();
            _passengerIcons = new ObservableCollection<int>();
            
            for (int i = 0; i < ElevatorModel.TotalFloors; i++)
            {
                Floors.Add(i);
            }

            CallElevatorCommand = new RelayCommand(OnCallElevator, CanCallElevator);
            SelectDestinationCommand = new RelayCommand(OnSelectDestination, CanSelectDestination);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;

            _currentFloor = 0;
            _elevatorState = "Beklemede";
            _statusMessage = "[Sistem] Asansör hazýr.";
            _totalTime = "00:00:00";
            _passengerCount = 0;
            _isSimulationRunning = false;
            _isInnerPanelOpen = false;
            _doorOpenAmount = 0.0;
            _isProcessingRequests = false;
        }

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
                // Yeni mesajý EN ÜSTE ekle
                StatusMessage = newMessage + "\n" + _statusMessage;
            }
        }

        private bool CanCallElevator(object? parameter)
        {
            // Sol panelden her zaman çaðrýlabilir
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

                var request = new PassengerRequest(callingFloor);
                request.SimulationTime = CurrentSimulationTime; // UI'dan alýnan gerçek saat
                request.ElevatorFloorAtRequest = _currentFloor;
                request.RequestTime = DateTime.Now; // Gerçek zaman - bekleme süresi için
                
                _pendingRequests.Add(request);

                AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] Kat {callingFloor}: Çaðrý geldi");

                // ML VERÝ TOPLAMA: Her çaðrýda kaydet
                _mlDataCollector.RecordRequest(
                    CurrentSimulationTime,
                    callingFloor,
                    _currentFloor,
                    0, // Bekleme süresi baþlangýçta 0 - asansör varýnca güncellenecek
                    _passengerCount,
                    _elevator.State.ToString()
                );

                // ÖZEL DURUM: Asansör zaten çaðrý yapýlan kattaysa
                if (callingFloor == _currentFloor && _elevator.State == Models.ElevatorState.Idle)
                {
                    // Direkt yolcu alma iþlemini baþlat
                    await HandleSameFloorPickup(request);
                }
                else if (!_isProcessingRequests)
                {
                    await ProcessRequests();
                }
            }
        }

        private bool CanSelectDestination(object? parameter)
        {
            // Ýç panel sadece yolcu varken aktif
            return parameter is int targetFloor && 
                   _isInnerPanelOpen && 
                   targetFloor != _currentFloor &&
                   _passengerCount > 0;
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
                    AddStatusMessage($"Kat {lastPickedUp.PickupFloor}: Yolcu bindi, {targetFloor}. kata gidecek");
                }
            }
        }

        private async Task HandleSameFloorPickup(PassengerRequest request)
        {
            // Kapasite kontrolü
            if (_passengerCount >= MaxCapacity)
            {
                AddStatusMessage($"Kat {_currentFloor}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                request.Status = RequestStatus.Completed;
                return;
            }

            // Kapýyý aç
            _elevator.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay = "Kapý Açýlýyor";
            await AnimateDoor(0.0, 1.0, ElevatorModel.DoorOperationTime);

            // Yolcu binme iþlemi
            _elevator.State = Models.ElevatorState.WaitingForPassenger;
            ElevatorStateDisplay = "Yolcu Biniyor";
            await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

            request.Status = RequestStatus.PickedUp;
            PassengerCount++;

            // Bekleme süresi (ayný kattaysa çok kýsa)
            var waitTimeSeconds = (int)(DateTime.Now - request.RequestTime).TotalSeconds;
            request.WaitTimeSeconds = waitTimeSeconds;

            // ML verisi kaydet
            _mlDataCollector.RecordRequest(
                request.SimulationTime,
                request.PickupFloor,
                request.ElevatorFloorAtRequest,
                waitTimeSeconds,
                _passengerCount - 1,
                "PickedUp"
            );

            AddStatusMessage($"Kat {_currentFloor}: Yolcu bindi (Bekleme: {waitTimeSeconds} saniye)");

            // Ýç paneli aç ve hedef seçilene kadar bekle
            IsInnerPanelOpen = true;

            int waitCount = 0;
            while (request.DestinationFloor == -1 && waitCount < 300)
            {
                await Task.Delay(100);
                waitCount++;
            }

            IsInnerPanelOpen = false;

            // Kapýyý kapat
            ElevatorStateDisplay = "Kapý Kapanýyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor(1.0, 0.0, ElevatorModel.DoorOperationTime);

            // Hedef seçildiyse iþleme devam et
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
                // Her adýmda yönü ve hedefi yeniden hesapla
                var direction = DetermineDirection();

                if (direction == 0)
                {
                    _elevator.State = Models.ElevatorState.Idle;
                    ElevatorStateDisplay = "Beklemede";
                    await Task.Delay(500);
                    continue;
                }

                // Mevcut yöndeki tüm duraklarý al
                var stopsInDirection = GetAllStopsInDirection(direction);

                if (!stopsInDirection.Any())
                {
                    await Task.Delay(100);
                    continue;
                }

                // EN YAKIN katý al
                var nextStop = stopsInDirection.First();

                // Durumunu güncelle
                _elevator.State = direction > 0 
                    ? Models.ElevatorState.MovingUp 
                    : Models.ElevatorState.MovingDown;
                ElevatorStateDisplay = direction > 0 ? "Yukarý Gidiyor" : "Aþaðý Gidiyor";

                // TEK KAT HAREKET ET
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.FloorTravelTime));
                
                if (direction > 0)
                {
                    _elevator.CurrentFloor++;
                }
                else
                {
                    _elevator.CurrentFloor--;
                }
                
                CurrentFloor = _elevator.CurrentFloor;

                // Hedefe ulaþtýk mý?
                if (_elevator.CurrentFloor == nextStop)
                {
                    // Bu katta durulmasý gerekiyor mu?
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
            AddStatusMessage("Tüm istekler tamamlandý");
        }

        private List<int> GetAllStopsInDirection(int direction)
        {
            var stops = new HashSet<int>();
            
            if (direction > 0) // Yukarý
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
            else if (direction < 0) // Aþaðý
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
            var allTargets = _destinationFloors
                .Concat(_pendingRequests.Where(r => r.Status == RequestStatus.Pending)
                                       .Select(r => r.PickupFloor))
                .ToList();

            if (!allTargets.Any()) return 0;

            var aboveCount = allTargets.Count(f => f > _currentFloor);
            var belowCount = allTargets.Count(f => f < _currentFloor);

            if (aboveCount > 0 && belowCount == 0) return 1;
            if (belowCount > 0 && aboveCount == 0) return -1;

            if (_elevator.State == Models.ElevatorState.MovingUp) return 1;
            if (_elevator.State == Models.ElevatorState.MovingDown) return -1;

            return aboveCount >= belowCount ? 1 : -1;
        }

        private async Task HandleStopOperations(int floor, bool hasDropOff, List<PassengerRequest> pickupRequests)
        {
            _elevator.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay = "Kapý Açýlýyor";
            await AnimateDoor(0.0, 1.0, ElevatorModel.DoorOperationTime);

            // 1. ÖNCE YOLCU ÝNDÝRME
            if (hasDropOff)
            {
                _elevator.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay = "Yolcu Ýniyor";
                
                var droppingPassengers = _pendingRequests
                    .Where(r => r.DestinationFloor == floor && r.Status == RequestStatus.PickedUp)
                    .ToList();

                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                foreach (var request in droppingPassengers)
                {
                    request.Status = RequestStatus.Completed;
                    AddStatusMessage($"Kat {floor}: {request.PickupFloor}. kattan binen yolcu indi");
                }

                while (_destinationFloors.Remove(floor)) { }
                PassengerCount -= droppingPassengers.Count;
                await Task.Delay(500);
            }

            // 2. SONRA YOLCU ALMA
            foreach (var pickupRequest in pickupRequests)
            {
                if (_passengerCount >= MaxCapacity)
                {
                    AddStatusMessage($"Kat {floor}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                    break;
                }

                _elevator.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay = "Yolcu Biniyor";
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                pickupRequest.Status = RequestStatus.PickedUp;
                PassengerCount++;
                
                // BEKLEME SÜRESÝNÝ HESAPLA ve ML VERÝSÝNÝ KAYDET
                var waitTimeSeconds = (int)(DateTime.Now - pickupRequest.RequestTime).TotalSeconds;
                pickupRequest.WaitTimeSeconds = waitTimeSeconds;
                
                _mlDataCollector.RecordRequest(
                    pickupRequest.SimulationTime,
                    pickupRequest.PickupFloor,
                    pickupRequest.ElevatorFloorAtRequest,
                    waitTimeSeconds,
                    _passengerCount - 1, // Bu yolcu binmeden önceki sayý
                    "PickedUp" // Yolcu alýndý
                );
                
                AddStatusMessage($"Kat {floor}: Yolcu bindi (Bekleme: {waitTimeSeconds} saniye)");
                
                // Ýç paneli aç ve hedef seçilene kadar bekle
                IsInnerPanelOpen = true;
                
                // Hedef seçilene kadar bekle (maksimum 30 saniye)
                int waitCount = 0;
                while (pickupRequest.DestinationFloor == -1 && waitCount < 300)
                {
                    await Task.Delay(100);
                    waitCount++;
                }
                
                IsInnerPanelOpen = false;
                
                // Eðer hedef seçildiyse mesaj zaten OnSelectDestination'da eklendi
                await Task.Delay(300);
            }

            ElevatorStateDisplay = "Kapý Kapanýyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor(1.0, 0.0, ElevatorModel.DoorOperationTime);
        }

        private async Task MoveToFloor(int targetFloor)
        {
            if (_elevator.CurrentFloor < targetFloor)
            {
                _elevator.State = Models.ElevatorState.MovingUp;
                ElevatorStateDisplay = "Yukarý Çýkýyor";
                
                while (_elevator.CurrentFloor < targetFloor)
                {
                    await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.FloorTravelTime));
                    _elevator.CurrentFloor++;
                    CurrentFloor = _elevator.CurrentFloor;
                }
            }
            else if (_elevator.CurrentFloor > targetFloor)
            {
                _elevator.State = Models.ElevatorState.MovingDown;
                ElevatorStateDisplay = "Aþaðý Ýniyor";
                
                while (_elevator.CurrentFloor > targetFloor)
                {
                    await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.FloorTravelTime));
                    _elevator.CurrentFloor--;
                    CurrentFloor = _elevator.CurrentFloor;
                }
            }
        }

        private async Task AnimateDoor(double from, double to, double duration)
        {
            const int steps = 20;
            double stepDuration = duration / steps;
            double increment = (to - from) / steps;

            for (int i = 0; i <= steps; i++)
            {
                DoorOpenAmount = from + (increment * i);
                await Task.Delay(TimeSpan.FromSeconds(stepDuration));
            }

            DoorOpenAmount = to;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isSimulationRunning)
            {
                var elapsed = DateTime.Now - _simulationStartTime;
                TotalTime = elapsed.ToString(@"hh\:mm\:ss");
            }
        }
    }
}

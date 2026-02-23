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
        private readonly ElevatorModel _elevator2; // Ýkinci asansör
        private readonly ElevatorModel _elevator3; // Üçüncü asansör
        private readonly ElevatorModel _elevator4; // Dördüncü asansör
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

        // Ýkinci asansör için deðiþkenler
        private int _currentFloor2;
        private string _elevatorState2;
        private int _passengerCount2;
        private bool _isInnerPanelOpen2;
        private double _doorOpenAmount2;
        private bool _isProcessingRequests2;
        private ObservableCollection<int> _passengerIcons2;

        // Üçüncü asansör için deðiþkenler
        private int _currentFloor3;
        private string _elevatorState3;
        private int _passengerCount3;
        private bool _isInnerPanelOpen3;
        private double _doorOpenAmount3;
        private bool _isProcessingRequests3;
        private ObservableCollection<int> _passengerIcons3;

        // Dördüncü asansör için deðiþkenler
        private int _currentFloor4;
        private string _elevatorState4;
        private int _passengerCount4;
        private bool _isInnerPanelOpen4;
        private double _doorOpenAmount4;
        private bool _isProcessingRequests4;
        private ObservableCollection<int> _passengerIcons4;

        // Yolcu istekleri ve hedefler
        private readonly List<PassengerRequest> _pendingRequests = new();
        private readonly List<PassengerRequest> _pendingRequests2 = new(); // Ýkinci asansör için
        private readonly List<PassengerRequest> _pendingRequests3 = new(); // Üçüncü asansör için
        private readonly List<PassengerRequest> _pendingRequests4 = new(); // Dördüncü asansör için
        private readonly HashSet<int> _destinationFloors = new();
        private readonly HashSet<int> _destinationFloors2 = new(); // Ýkinci asansör için
        private readonly HashSet<int> _destinationFloors3 = new(); // Üçüncü asansör için
        private readonly HashSet<int> _destinationFloors4 = new(); // Dördüncü asansör için
        private const int MaxCapacity = 10;

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
        public ObservableCollection<int> PassengerIcons3
        {
            get => _passengerIcons3;
            set => SetProperty(ref _passengerIcons3, value);
        }
        public ObservableCollection<int> PassengerIcons4
        {
            get => _passengerIcons4;
            set => SetProperty(ref _passengerIcons4, value);
        }
        public ICommand CallElevatorCommand { get; }
        public ICommand SelectDestinationCommand { get; }
        public ICommand SelectDestinationCommand2 { get; } // Ýkinci asansör için
        public ICommand SelectDestinationCommand3 { get; } // Üçüncü asansör için
        public ICommand SelectDestinationCommand4 { get; } // Dördüncü asansör için

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

        public int CurrentFloor3
        {
            get => _currentFloor3;
            set => SetProperty(ref _currentFloor3, value);
        }

        public int CurrentFloor4
        {
            get => _currentFloor4;
            set => SetProperty(ref _currentFloor4, value);
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

        public string ElevatorStateDisplay3
        {
            get => _elevatorState3;
            set => SetProperty(ref _elevatorState3, value);
        }

        public string ElevatorStateDisplay4
        {
            get => _elevatorState4;
            set => SetProperty(ref _elevatorState4, value);
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

        public int PassengerCount2
        {
            get => _passengerCount2;
            set
            {
                SetProperty(ref _passengerCount2, value);
                OnPropertyChanged(nameof(HasPassenger2));

                // Ýkon listesini güncelle
                PassengerIcons2.Clear();
                for (int i = 0; i < value; i++)
                {
                    PassengerIcons2.Add(i);
                }
            }
        }

        public int PassengerCount3
        {
            get => _passengerCount3;
            set
            {
                SetProperty(ref _passengerCount3, value);
                OnPropertyChanged(nameof(HasPassenger3));

                // Ýkon listesini güncelle
                PassengerIcons3.Clear();
                for (int i = 0; i < value; i++)
                {
                    PassengerIcons3.Add(i);
                }
            }
        }

        public int PassengerCount4
        {
            get => _passengerCount4;
            set
            {
                SetProperty(ref _passengerCount4, value);
                OnPropertyChanged(nameof(HasPassenger4));

                // Ýkon listesini güncelle
                PassengerIcons4.Clear();
                for (int i = 0; i < value; i++)
                {
                    PassengerIcons4.Add(i);
                }
            }
        }

        public bool HasPassenger => _passengerCount > 0;
        public bool HasPassenger2 => _passengerCount2 > 0;
        public bool HasPassenger3 => _passengerCount3 > 0;
        public bool HasPassenger4 => _passengerCount4 > 0;

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

        public bool IsInnerPanelOpen2
        {
            get => _isInnerPanelOpen2;
            set => SetProperty(ref _isInnerPanelOpen2, value);
        }

        public bool IsInnerPanelOpen3
        {
            get => _isInnerPanelOpen3;
            set => SetProperty(ref _isInnerPanelOpen3, value);
        }

        public bool IsInnerPanelOpen4
        {
            get => _isInnerPanelOpen4;
            set => SetProperty(ref _isInnerPanelOpen4, value);
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

        public double DoorOpenAmount3
        {
            get => _doorOpenAmount3;
            set => SetProperty(ref _doorOpenAmount3, value);
        }

        public double DoorOpenAmount4
        {
            get => _doorOpenAmount4;
            set => SetProperty(ref _doorOpenAmount4, value);
        }

        public MainViewModel()
        {
            _elevator = new ElevatorModel();
            _elevator2 = new ElevatorModel(); // Ýkinci asansör
            _elevator3 = new ElevatorModel(); // Üçüncü asansör
            _elevator4 = new ElevatorModel(); // Dördüncü asansör
            _mlDataCollector = new MLDataCollector();
            Floors = new ObservableCollection<int>();
            _passengerIcons = new ObservableCollection<int>();
            _passengerIcons2 = new ObservableCollection<int>();
            _passengerIcons3 = new ObservableCollection<int>();
            _passengerIcons4 = new ObservableCollection<int>();

            for (int i = 0; i < ElevatorModel.TotalFloors; i++)
            {
                Floors.Add(i);
            }

            CallElevatorCommand = new RelayCommand(OnCallElevator, CanCallElevator);
            SelectDestinationCommand = new RelayCommand(OnSelectDestination, CanSelectDestination);
            SelectDestinationCommand2 = new RelayCommand(OnSelectDestination2, CanSelectDestination2);
            SelectDestinationCommand3 = new RelayCommand(OnSelectDestination3, CanSelectDestination3);
            SelectDestinationCommand4 = new RelayCommand(OnSelectDestination4, CanSelectDestination4);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;

            _currentFloor = 0;
            _currentFloor2 = 0; // Ýkinci asansör baþlangýç katý
            _currentFloor3 = 0; // Üçüncü asansör baþlangýç katý
            _currentFloor4 = 0; // Dördüncü asansör baþlangýç katý
            _elevatorState = "Beklemede";
            _elevatorState2 = "Beklemede"; // Ýkinci asansör durumu
            _elevatorState3 = "Beklemede"; // Üçüncü asansör durumu
            _elevatorState4 = "Beklemede"; // Dördüncü asansör durumu
            _statusMessage = "[Sistem] Asansörler hazýr.";
            _totalTime = "00:00:00";
            _passengerCount = 0;
            _passengerCount2 = 0; // Ýkinci asansör yolcu sayýsý
            _passengerCount3 = 0; // Üçüncü asansör yolcu sayýsý
            _passengerCount4 = 0; // Dördüncü asansör yolcu sayýsý
            _isSimulationRunning = false;
            _isInnerPanelOpen = false;
            _isInnerPanelOpen2 = false; // Ýkinci asansör paneli
            _isInnerPanelOpen3 = false; // Üçüncü asansör paneli
            _isInnerPanelOpen4 = false; // Dördüncü asansör paneli
            _doorOpenAmount = 0.0;
            _doorOpenAmount2 = 0.0; // Ýkinci asansör kapýsý
            _doorOpenAmount3 = 0.0; // Üçüncü asansör kapýsý
            _doorOpenAmount4 = 0.0; // Dördüncü asansör kapýsý
            _isProcessingRequests = false;
            _isProcessingRequests2 = false; // Ýkinci asansör iþlem durumu
            _isProcessingRequests3 = false; // Üçüncü asansör iþlem durumu
            _isProcessingRequests4 = false; // Dördüncü asansör iþlem durumu
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

                // 4 asansör için mesafe hesapla
                int distance1 = Math.Abs(_currentFloor - callingFloor);
                int distance2 = Math.Abs(_currentFloor2 - callingFloor);
                int distance3 = Math.Abs(_currentFloor3 - callingFloor);
                int distance4 = Math.Abs(_currentFloor4 - callingFloor);

                // Her asansörün meþgul olup olmadýðýný kontrol et
                bool elevator1Busy = _isProcessingRequests || _elevator.State != Models.ElevatorState.Idle;
                bool elevator2Busy = _isProcessingRequests2 || _elevator2.State != Models.ElevatorState.Idle;
                bool elevator3Busy = _isProcessingRequests3 || _elevator3.State != Models.ElevatorState.Idle;
                bool elevator4Busy = _isProcessingRequests4 || _elevator4.State != Models.ElevatorState.Idle;

                // En uygun asansörü seç
                int selectedElevator = 1;
                int minDistance = distance1;

                // Boþta olan en yakýn asansörü bul
                if (!elevator2Busy && (elevator1Busy || distance2 < minDistance))
                {
                    selectedElevator = 2;
                    minDistance = distance2;
                }
                if (!elevator3Busy && (elevator1Busy || distance3 < minDistance))
                {
                    selectedElevator = 3;
                    minDistance = distance3;
                }
                if (!elevator4Busy && (elevator1Busy || distance4 < minDistance))
                {
                    selectedElevator = 4;
                    minDistance = distance4;
                }

                // Seçilen asansöre göre iþlem yap
                switch (selectedElevator)
                {
                    case 1:
                        await HandleElevatorCall(callingFloor, 1, _currentFloor, _pendingRequests);
                        break;
                    case 2:
                        await HandleElevatorCall(callingFloor, 2, _currentFloor2, _pendingRequests2);
                        break;
                    case 3:
                        await HandleElevatorCall(callingFloor, 3, _currentFloor3, _pendingRequests3);
                        break;
                    case 4:
                        await HandleElevatorCall(callingFloor, 4, _currentFloor4, _pendingRequests4);
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

            AddStatusMessage($"[{CurrentSimulationTime:hh\\:mm}] [Asansör {elevatorNumber}] Kat {callingFloor}: Çaðrý geldi");

            // Kat mesafesini hesapla
            int floorDistance = Math.Abs(callingFloor - currentFloor);

            var elevatorState = elevatorNumber switch
            {
                1 => _elevator.State.ToString(),
                2 => _elevator2.State.ToString(),
                3 => _elevator3.State.ToString(),
                4 => _elevator4.State.ToString(),
                _ => "Unknown"
            };

            var passengerCount = elevatorNumber switch
            {
                1 => _passengerCount,
                2 => _passengerCount2,
                3 => _passengerCount3,
                4 => _passengerCount4,
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

            // Ayný katta mý kontrol et
            bool isIdleState = elevatorNumber switch
            {
                1 => _elevator.State == Models.ElevatorState.Idle,
                2 => _elevator2.State == Models.ElevatorState.Idle,
                3 => _elevator3.State == Models.ElevatorState.Idle,
                4 => _elevator4.State == Models.ElevatorState.Idle,
                _ => false
            };

            bool isProcessing = elevatorNumber switch
            {
                1 => _isProcessingRequests,
                2 => _isProcessingRequests2,
                3 => _isProcessingRequests3,
                4 => _isProcessingRequests4,
                _ => false
            };

            if (callingFloor == currentFloor && isIdleState)
            {
                switch (elevatorNumber)
                {
                    case 1: await HandleSameFloorPickup(request); break;
                    case 2: await HandleSameFloorPickup2(request); break;
                    case 3: await HandleSameFloorPickup3(request); break;
                    case 4: await HandleSameFloorPickup4(request); break;
                }
            }
            else if (!isProcessing)
            {
                switch (elevatorNumber)
                {
                    case 1: await ProcessRequests(); break;
                    case 2: await ProcessRequests2(); break;
                    case 3: await ProcessRequests3(); break;
                    case 4: await ProcessRequests4(); break;
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

        private bool CanSelectDestination2(object? parameter)
        {
            // Ýkinci asansör iç panel sadece yolcu varken aktif
            return parameter is int targetFloor && 
                   _isInnerPanelOpen2 && 
                   targetFloor != _currentFloor2 &&
                   _passengerCount2 > 0;
        }

        private bool CanSelectDestination3(object? parameter)
        {
            // Üçüncü asansör iç panel sadece yolcu varken aktif
            return parameter is int targetFloor && 
                   _isInnerPanelOpen3 && 
                   targetFloor != _currentFloor3 &&
                   _passengerCount3 > 0;
        }

        private bool CanSelectDestination4(object? parameter)
        {
            // Dördüncü asansör iç panel sadece yolcu varken aktif
            return parameter is int targetFloor && 
                   _isInnerPanelOpen4 && 
                   targetFloor != _currentFloor4 &&
                   _passengerCount4 > 0;
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

        private void OnSelectDestination3(object? parameter)
        {
            if (parameter is int targetFloor)
            {
                var lastPickedUp = _pendingRequests3
                    .Where(r => r.Status == RequestStatus.PickedUp && r.DestinationFloor == -1)
                    .OrderByDescending(r => r.PickupFloor == _currentFloor3)
                    .FirstOrDefault();

                if (lastPickedUp != null)
                {
                    lastPickedUp.DestinationFloor = targetFloor;
                    _destinationFloors3.Add(targetFloor);
                    AddStatusMessage($"[Asansör 3] Kat {lastPickedUp.PickupFloor}: Yolcu bindi, {targetFloor}. kata gidecek");
                }
            }
        }

        private void OnSelectDestination4(object? parameter)
        {
            if (parameter is int targetFloor)
            {
                var lastPickedUp = _pendingRequests4
                    .Where(r => r.Status == RequestStatus.PickedUp && r.DestinationFloor == -1)
                    .OrderByDescending(r => r.PickupFloor == _currentFloor4)
                    .FirstOrDefault();

                if (lastPickedUp != null)
                {
                    lastPickedUp.DestinationFloor = targetFloor;
                    _destinationFloors4.Add(targetFloor);
                    AddStatusMessage($"[Asansör 4] Kat {lastPickedUp.PickupFloor}: Yolcu bindi, {targetFloor}. kata gidecek");
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

            // Kat mesafesini hesapla
            int floorDistance = Math.Abs(request.PickupFloor - request.ElevatorFloorAtRequest);

            // ML verisi kaydet - Hedef kat henüz bilinmiyor
            _mlDataCollector.RecordRequest(
                request.SimulationTime,
                request.PickupFloor,
                request.ElevatorFloorAtRequest,
                -1,                          // Hedef kat henüz seçilmedi
                floorDistance,
                "Asansör 1",
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

                // Kat mesafesini hesapla
                int floorDistance = Math.Abs(pickupRequest.PickupFloor - pickupRequest.ElevatorFloorAtRequest);

                _mlDataCollector.RecordRequest(
                    pickupRequest.SimulationTime,
                    pickupRequest.PickupFloor,
                    pickupRequest.ElevatorFloorAtRequest,
                    -1,                          // Hedef kat henüz seçilmedi
                    floorDistance,
                    "Asansör 1",
                    waitTimeSeconds,
                    _passengerCount - 1,         // Bu yolcu binmeden önceki sayý
                    "PickedUp"                   // Yolcu alýndý
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

        // ==================== ÝKÝNCÝ ASANSÖR METODLARÝ ====================

        private async Task HandleSameFloorPickup2(PassengerRequest request)
        {
            if (_passengerCount2 >= MaxCapacity)
            {
                AddStatusMessage($"[Asansör 2] Kat {_currentFloor2}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                request.Status = RequestStatus.Completed;
                return;
            }

            _elevator2.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay2 = "Kapý Açýlýyor";
            await AnimateDoor2(0.0, 1.0, ElevatorModel.DoorOperationTime);

            _elevator2.State = Models.ElevatorState.WaitingForPassenger;
            ElevatorStateDisplay2 = "Yolcu Biniyor";
            await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

            request.Status = RequestStatus.PickedUp;
            PassengerCount2++;

            var waitTimeSeconds = (int)(DateTime.Now - request.RequestTime).TotalSeconds;
            request.WaitTimeSeconds = waitTimeSeconds;

            // Kat mesafesini hesapla
            int floorDistance = Math.Abs(request.PickupFloor - request.ElevatorFloorAtRequest);

            _mlDataCollector.RecordRequest(
                request.SimulationTime,
                request.PickupFloor,
                request.ElevatorFloorAtRequest,
                -1,                          // Hedef kat henüz seçilmedi
                floorDistance,
                "Asansör 2",
                waitTimeSeconds,
                _passengerCount2 - 1,
                "PickedUp"
            );

            AddStatusMessage($"[Asansör 2] Kat {_currentFloor2}: Yolcu bindi (Bekleme: {waitTimeSeconds} saniye)");

            IsInnerPanelOpen2 = true;

            int waitCount = 0;
            while (request.DestinationFloor == -1 && waitCount < 300)
            {
                await Task.Delay(100);
                waitCount++;
            }

            IsInnerPanelOpen2 = false;

            ElevatorStateDisplay2 = "Kapý Kapanýyor";
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
                ElevatorStateDisplay2 = direction > 0 ? "Yukarý Gidiyor" : "Aþaðý Gidiyor";

                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.FloorTravelTime));

                if (direction > 0)
                {
                    _elevator2.CurrentFloor++;
                }
                else
                {
                    _elevator2.CurrentFloor--;
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
            var allTargets = _destinationFloors2
                .Concat(_pendingRequests2.Where(r => r.Status == RequestStatus.Pending)
                                       .Select(r => r.PickupFloor))
                .ToList();

            if (!allTargets.Any()) return 0;

            var aboveCount = allTargets.Count(f => f > _currentFloor2);
            var belowCount = allTargets.Count(f => f < _currentFloor2);

            if (aboveCount > 0 && belowCount == 0) return 1;
            if (belowCount > 0 && aboveCount == 0) return -1;

            if (_elevator2.State == Models.ElevatorState.MovingUp) return 1;
            if (_elevator2.State == Models.ElevatorState.MovingDown) return -1;

            return aboveCount >= belowCount ? 1 : -1;
        }

        private async Task HandleStopOperations2(int floor, bool hasDropOff, List<PassengerRequest> pickupRequests)
        {
            _elevator2.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay2 = "Kapý Açýlýyor";
            await AnimateDoor2(0.0, 1.0, ElevatorModel.DoorOperationTime);

            if (hasDropOff)
            {
                _elevator2.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay2 = "Yolcu Ýniyor";

                var droppingPassengers = _pendingRequests2
                    .Where(r => r.DestinationFloor == floor && r.Status == RequestStatus.PickedUp)
                    .ToList();

                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                foreach (var request in droppingPassengers)
                {
                    request.Status = RequestStatus.Completed;
                    AddStatusMessage($"[Asansör 2] Kat {floor}: {request.PickupFloor}. kattan binen yolcu indi");
                }

                while (_destinationFloors2.Remove(floor)) { }
                PassengerCount2 -= droppingPassengers.Count;
                await Task.Delay(500);
            }

            foreach (var pickupRequest in pickupRequests)
            {
                if (_passengerCount2 >= MaxCapacity)
                {
                    AddStatusMessage($"[Asansör 2] Kat {floor}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                    break;
                }

                _elevator2.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay2 = "Yolcu Biniyor";
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                pickupRequest.Status = RequestStatus.PickedUp;
                PassengerCount2++;

                var waitTimeSeconds = (int)(DateTime.Now - pickupRequest.RequestTime).TotalSeconds;
                pickupRequest.WaitTimeSeconds = waitTimeSeconds;

                // Kat mesafesini hesapla
                int floorDistance = Math.Abs(pickupRequest.PickupFloor - pickupRequest.ElevatorFloorAtRequest);

                _mlDataCollector.RecordRequest(
                    pickupRequest.SimulationTime,
                    pickupRequest.PickupFloor,
                    pickupRequest.ElevatorFloorAtRequest,
                    -1,                          // Hedef kat henüz seçilmedi
                    floorDistance,
                    "Asansör 2",
                    waitTimeSeconds,
                    _passengerCount2 - 1,
                    "PickedUp"
                );

                AddStatusMessage($"[Asansör 2] Kat {floor}: Yolcu bindi (Bekleme: {waitTimeSeconds} saniye)");

                IsInnerPanelOpen2 = true;

                int waitCount = 0;
                while (pickupRequest.DestinationFloor == -1 && waitCount < 300)
                {
                    await Task.Delay(100);
                    waitCount++;
                }

                IsInnerPanelOpen2 = false;
                await Task.Delay(300);
            }

            ElevatorStateDisplay2 = "Kapý Kapanýyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator2.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor2(1.0, 0.0, ElevatorModel.DoorOperationTime);
        }

        private async Task AnimateDoor2(double from, double to, double duration)
        {
            const int steps = 20;
            double stepDuration = duration / steps;
            double increment = (to - from) / steps;

            for (int i = 0; i <= steps; i++)
            {
                DoorOpenAmount2 = from + (increment * i);
                await Task.Delay(TimeSpan.FromSeconds(stepDuration));
            }

            DoorOpenAmount2 = to;
        }

        // ==================== ÜÇÜNCÜ ASANSÖR METODLARÝ ====================

        private async Task HandleSameFloorPickup3(PassengerRequest request)
        {
            if (_passengerCount3 >= MaxCapacity)
            {
                AddStatusMessage($"[Asansör 3] Kat {_currentFloor3}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                request.Status = RequestStatus.Completed;
                return;
            }

            _elevator3.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay3 = "Kapý Açýlýyor";
            await AnimateDoor3(0.0, 1.0, ElevatorModel.DoorOperationTime);

            _elevator3.State = Models.ElevatorState.WaitingForPassenger;
            ElevatorStateDisplay3 = "Yolcu Biniyor";
            await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

            request.Status = RequestStatus.PickedUp;
            PassengerCount3++;

            var waitTimeSeconds = (int)(DateTime.Now - request.RequestTime).TotalSeconds;
            request.WaitTimeSeconds = waitTimeSeconds;

            int floorDistance = Math.Abs(request.PickupFloor - request.ElevatorFloorAtRequest);

            _mlDataCollector.RecordRequest(
                request.SimulationTime,
                request.PickupFloor,
                request.ElevatorFloorAtRequest,
                -1,
                floorDistance,
                "Asansör 3",
                waitTimeSeconds,
                _passengerCount3 - 1,
                "PickedUp"
            );

            AddStatusMessage($"[Asansör 3] Kat {_currentFloor3}: Yolcu bindi (Bekleme: {waitTimeSeconds} saniye)");

            IsInnerPanelOpen3 = true;

            int waitCount = 0;
            while (request.DestinationFloor == -1 && waitCount < 300)
            {
                await Task.Delay(100);
                waitCount++;
            }

            IsInnerPanelOpen3 = false;

            ElevatorStateDisplay3 = "Kapý Kapanýyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator3.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor3(1.0, 0.0, ElevatorModel.DoorOperationTime);

            if (request.DestinationFloor != -1 && !_isProcessingRequests3)
            {
                await ProcessRequests3();
            }
        }

        private async Task ProcessRequests3()
        {
            _isProcessingRequests3 = true;

            while (_pendingRequests3.Any(r => r.Status != RequestStatus.Completed) || 
                   _destinationFloors3.Count > 0)
            {
                var direction = DetermineDirection3();

                if (direction == 0)
                {
                    _elevator3.State = Models.ElevatorState.Idle;
                    ElevatorStateDisplay3 = "Beklemede";
                    await Task.Delay(500);
                    continue;
                }

                var stopsInDirection = GetAllStopsInDirection3(direction);

                if (!stopsInDirection.Any())
                {
                    await Task.Delay(100);
                    continue;
                }

                var nextStop = stopsInDirection.First();

                _elevator3.State = direction > 0 
                    ? Models.ElevatorState.MovingUp 
                    : Models.ElevatorState.MovingDown;
                ElevatorStateDisplay3 = direction > 0 ? "Yukarý Gidiyor" : "Aþaðý Gidiyor";

                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.FloorTravelTime));

                if (direction > 0)
                {
                    _elevator3.CurrentFloor++;
                }
                else
                {
                    _elevator3.CurrentFloor--;
                }

                CurrentFloor3 = _elevator3.CurrentFloor;

                if (_elevator3.CurrentFloor == nextStop)
                {
                    bool hasDropOff = _destinationFloors3.Contains(nextStop);
                    var pickupRequests = _pendingRequests3
                        .Where(r => r.PickupFloor == nextStop && r.Status == RequestStatus.Pending)
                        .ToList();

                    if (hasDropOff || pickupRequests.Any())
                    {
                        await HandleStopOperations3(nextStop, hasDropOff, pickupRequests);
                    }
                }
            }

            _isProcessingRequests3 = false;
            _elevator3.State = Models.ElevatorState.Idle;
            ElevatorStateDisplay3 = "Beklemede";
        }

        private List<int> GetAllStopsInDirection3(int direction)
        {
            var stops = new HashSet<int>();

            if (direction > 0)
            {
                foreach (var floor in _destinationFloors3.Where(f => f > _currentFloor3))
                {
                    stops.Add(floor);
                }

                foreach (var request in _pendingRequests3.Where(r => r.Status == RequestStatus.Pending && r.PickupFloor > _currentFloor3))
                {
                    stops.Add(request.PickupFloor);
                }

                return stops.OrderBy(f => f).ToList();
            }

            if (direction < 0)
            {
                foreach (var floor in _destinationFloors3.Where(f => f < _currentFloor3))
                {
                    stops.Add(floor);
                }

                foreach (var request in _pendingRequests3.Where(r => r.Status == RequestStatus.Pending && r.PickupFloor < _currentFloor3))
                {
                    stops.Add(request.PickupFloor);
                }

                return stops.OrderByDescending(f => f).ToList();
            }

            return new List<int>();
        }

        private int DetermineDirection3()
        {
            var allTargets = _destinationFloors3
                .Concat(_pendingRequests3.Where(r => r.Status == RequestStatus.Pending)
                                       .Select(r => r.PickupFloor))
                .ToList();

            if (!allTargets.Any()) return 0;

            var aboveCount = allTargets.Count(f => f > _currentFloor3);
            var belowCount = allTargets.Count(f => f < _currentFloor3);

            if (aboveCount > 0 && belowCount == 0) return 1;
            if (belowCount > 0 && aboveCount == 0) return -1;

            if (_elevator3.State == Models.ElevatorState.MovingUp) return 1;
            if (_elevator3.State == Models.ElevatorState.MovingDown) return -1;

            return aboveCount >= belowCount ? 1 : -1;
        }

        private async Task HandleStopOperations3(int floor, bool hasDropOff, List<PassengerRequest> pickupRequests)
        {
            _elevator3.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay3 = "Kapý Açýlýyor";
            await AnimateDoor3(0.0, 1.0, ElevatorModel.DoorOperationTime);

            if (hasDropOff)
            {
                _elevator3.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay3 = "Yolcu Ýniyor";

                var droppingPassengers = _pendingRequests3
                    .Where(r => r.DestinationFloor == floor && r.Status == RequestStatus.PickedUp)
                    .ToList();

                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                foreach (var request in droppingPassengers)
                {
                    request.Status = RequestStatus.Completed;
                    AddStatusMessage($"[Asansör 3] Kat {floor}: {request.PickupFloor}. kattan binen yolcu indi");
                }

                while (_destinationFloors3.Remove(floor)) { }
                PassengerCount3 -= droppingPassengers.Count;
                await Task.Delay(500);
            }

            foreach (var pickupRequest in pickupRequests)
            {
                if (_passengerCount3 >= MaxCapacity)
                {
                    AddStatusMessage($"[Asansör 3] Kat {floor}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                    break;
                }

                _elevator3.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay3 = "Yolcu Biniyor";
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                pickupRequest.Status = RequestStatus.PickedUp;
                PassengerCount3++;

                var waitTimeSeconds = (int)(DateTime.Now - pickupRequest.RequestTime).TotalSeconds;
                pickupRequest.WaitTimeSeconds = waitTimeSeconds;

                int floorDistance = Math.Abs(pickupRequest.PickupFloor - pickupRequest.ElevatorFloorAtRequest);

                _mlDataCollector.RecordRequest(
                    pickupRequest.SimulationTime,
                    pickupRequest.PickupFloor,
                    pickupRequest.ElevatorFloorAtRequest,
                    -1,
                    floorDistance,
                    "Asansör 3",
                    waitTimeSeconds,
                    _passengerCount3 - 1,
                    "PickedUp"
                );

                AddStatusMessage($"[Asansör 3] Kat {floor}: Yolcu bindi (Bekleme: {waitTimeSeconds} saniye)");

                IsInnerPanelOpen3 = true;

                int waitCount = 0;
                while (pickupRequest.DestinationFloor == -1 && waitCount < 300)
                {
                    await Task.Delay(100);
                    waitCount++;
                }

                IsInnerPanelOpen3 = false;
                await Task.Delay(300);
            }

            ElevatorStateDisplay3 = "Kapý Kapanýyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator3.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor3(1.0, 0.0, ElevatorModel.DoorOperationTime);
        }

        private async Task AnimateDoor3(double from, double to, double duration)
        {
            const int steps = 20;
            double stepDuration = duration / steps;
            double increment = (to - from) / steps;

            for (int i = 0; i <= steps; i++)
            {
                DoorOpenAmount3 = from + (increment * i);
                await Task.Delay(TimeSpan.FromSeconds(stepDuration));
            }

            DoorOpenAmount3 = to;
        }

        // ==================== DÖRDÜNCÜ ASANSÖR METODLARÝ ====================

        private async Task HandleSameFloorPickup4(PassengerRequest request)
        {
            if (_passengerCount4 >= MaxCapacity)
            {
                AddStatusMessage($"[Asansör 4] Kat {_currentFloor4}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                request.Status = RequestStatus.Completed;
                return;
            }

            _elevator4.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay4 = "Kapý Açýlýyor";
            await AnimateDoor4(0.0, 1.0, ElevatorModel.DoorOperationTime);

            _elevator4.State = Models.ElevatorState.WaitingForPassenger;
            ElevatorStateDisplay4 = "Yolcu Biniyor";
            await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

            request.Status = RequestStatus.PickedUp;
            PassengerCount4++;

            var waitTimeSeconds = (int)(DateTime.Now - request.RequestTime).TotalSeconds;
            request.WaitTimeSeconds = waitTimeSeconds;

            int floorDistance = Math.Abs(request.PickupFloor - request.ElevatorFloorAtRequest);

            _mlDataCollector.RecordRequest(
                request.SimulationTime,
                request.PickupFloor,
                request.ElevatorFloorAtRequest,
                -1,
                floorDistance,
                "Asansör 4",
                waitTimeSeconds,
                _passengerCount4 - 1,
                "PickedUp"
            );

            AddStatusMessage($"[Asansör 4] Kat {_currentFloor4}: Yolcu bindi (Bekleme: {waitTimeSeconds} saniye)");

            IsInnerPanelOpen4 = true;

            int waitCount = 0;
            while (request.DestinationFloor == -1 && waitCount < 300)
            {
                await Task.Delay(100);
                waitCount++;
            }

            IsInnerPanelOpen4 = false;

            ElevatorStateDisplay4 = "Kapý Kapanýyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator4.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor4(1.0, 0.0, ElevatorModel.DoorOperationTime);

            if (request.DestinationFloor != -1 && !_isProcessingRequests4)
            {
                await ProcessRequests4();
            }
        }

        private async Task ProcessRequests4()
        {
            _isProcessingRequests4 = true;

            while (_pendingRequests4.Any(r => r.Status != RequestStatus.Completed) || 
                   _destinationFloors4.Count > 0)
            {
                var direction = DetermineDirection4();

                if (direction == 0)
                {
                    _elevator4.State = Models.ElevatorState.Idle;
                    ElevatorStateDisplay4 = "Beklemede";
                    await Task.Delay(500);
                    continue;
                }

                var stopsInDirection = GetAllStopsInDirection4(direction);

                if (!stopsInDirection.Any())
                {
                    await Task.Delay(100);
                    continue;
                }

                var nextStop = stopsInDirection.First();

                _elevator4.State = direction > 0 
                    ? Models.ElevatorState.MovingUp 
                    : Models.ElevatorState.MovingDown;
                ElevatorStateDisplay4 = direction > 0 ? "Yukarý Gidiyor" : "Aþaðý Gidiyor";

                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.FloorTravelTime));

                if (direction > 0)
                {
                    _elevator4.CurrentFloor++;
                }
                else
                {
                    _elevator4.CurrentFloor--;
                }

                CurrentFloor4 = _elevator4.CurrentFloor;

                if (_elevator4.CurrentFloor == nextStop)
                {
                    bool hasDropOff = _destinationFloors4.Contains(nextStop);
                    var pickupRequests = _pendingRequests4
                        .Where(r => r.PickupFloor == nextStop && r.Status == RequestStatus.Pending)
                        .ToList();

                    if (hasDropOff || pickupRequests.Any())
                    {
                        await HandleStopOperations4(nextStop, hasDropOff, pickupRequests);
                    }
                }
            }

            _isProcessingRequests4 = false;
            _elevator4.State = Models.ElevatorState.Idle;
            ElevatorStateDisplay4 = "Beklemede";
        }

        private List<int> GetAllStopsInDirection4(int direction)
        {
            var stops = new HashSet<int>();

            if (direction > 0)
            {
                foreach (var floor in _destinationFloors4.Where(f => f > _currentFloor4))
                {
                    stops.Add(floor);
                }

                foreach (var request in _pendingRequests4.Where(r => r.Status == RequestStatus.Pending && r.PickupFloor > _currentFloor4))
                {
                    stops.Add(request.PickupFloor);
                }

                return stops.OrderBy(f => f).ToList();
            }

            if (direction < 0)
            {
                foreach (var floor in _destinationFloors4.Where(f => f < _currentFloor4))
                {
                    stops.Add(floor);
                }

                foreach (var request in _pendingRequests4.Where(r => r.Status == RequestStatus.Pending && r.PickupFloor < _currentFloor4))
                {
                    stops.Add(request.PickupFloor);
                }

                return stops.OrderByDescending(f => f).ToList();
            }

            return new List<int>();
        }

        private int DetermineDirection4()
        {
            var allTargets = _destinationFloors4
                .Concat(_pendingRequests4.Where(r => r.Status == RequestStatus.Pending)
                                       .Select(r => r.PickupFloor))
                .ToList();

            if (!allTargets.Any()) return 0;

            var aboveCount = allTargets.Count(f => f > _currentFloor4);
            var belowCount = allTargets.Count(f => f < _currentFloor4);

            if (aboveCount > 0 && belowCount == 0) return 1;
            if (belowCount > 0 && aboveCount == 0) return -1;

            if (_elevator4.State == Models.ElevatorState.MovingUp) return 1;
            if (_elevator4.State == Models.ElevatorState.MovingDown) return -1;

            return aboveCount >= belowCount ? 1 : -1;
        }

        private async Task HandleStopOperations4(int floor, bool hasDropOff, List<PassengerRequest> pickupRequests)
        {
            _elevator4.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay4 = "Kapý Açýlýyor";
            await AnimateDoor4(0.0, 1.0, ElevatorModel.DoorOperationTime);

            if (hasDropOff)
            {
                _elevator4.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay4 = "Yolcu Ýniyor";

                var droppingPassengers = _pendingRequests4
                    .Where(r => r.DestinationFloor == floor && r.Status == RequestStatus.PickedUp)
                    .ToList();

                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                foreach (var request in droppingPassengers)
                {
                    request.Status = RequestStatus.Completed;
                    AddStatusMessage($"[Asansör 4] Kat {floor}: {request.PickupFloor}. kattan binen yolcu indi");
                }

                while (_destinationFloors4.Remove(floor)) { }
                PassengerCount4 -= droppingPassengers.Count;
                await Task.Delay(500);
            }

            foreach (var pickupRequest in pickupRequests)
            {
                if (_passengerCount4 >= MaxCapacity)
                {
                    AddStatusMessage($"[Asansör 4] Kat {floor}: Kapasite dolu ({MaxCapacity}/{MaxCapacity})");
                    break;
                }

                _elevator4.State = Models.ElevatorState.WaitingForPassenger;
                ElevatorStateDisplay4 = "Yolcu Biniyor";
                await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

                pickupRequest.Status = RequestStatus.PickedUp;
                PassengerCount4++;

                var waitTimeSeconds = (int)(DateTime.Now - pickupRequest.RequestTime).TotalSeconds;
                pickupRequest.WaitTimeSeconds = waitTimeSeconds;

                int floorDistance = Math.Abs(pickupRequest.PickupFloor - pickupRequest.ElevatorFloorAtRequest);

                _mlDataCollector.RecordRequest(
                    pickupRequest.SimulationTime,
                    pickupRequest.PickupFloor,
                    pickupRequest.ElevatorFloorAtRequest,
                    -1,
                    floorDistance,
                    "Asansör 4",
                    waitTimeSeconds,
                    _passengerCount4 - 1,
                    "PickedUp"
                );

                AddStatusMessage($"[Asansör 4] Kat {floor}: Yolcu bindi (Bekleme: {waitTimeSeconds} saniye)");

                IsInnerPanelOpen4 = true;

                int waitCount = 0;
                while (pickupRequest.DestinationFloor == -1 && waitCount < 300)
                {
                    await Task.Delay(100);
                    waitCount++;
                }

                IsInnerPanelOpen4 = false;
                await Task.Delay(300);
            }

            ElevatorStateDisplay4 = "Kapý Kapanýyor";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            _elevator4.State = Models.ElevatorState.DoorClosing;
            await AnimateDoor4(1.0, 0.0, ElevatorModel.DoorOperationTime);
        }

        private async Task AnimateDoor4(double from, double to, double duration)
        {
            const int steps = 20;
            double stepDuration = duration / steps;
            double increment = (to - from) / steps;

            for (int i = 0; i <= steps; i++)
            {
                DoorOpenAmount4 = from + (increment * i);
                await Task.Delay(TimeSpan.FromSeconds(stepDuration));
            }

            DoorOpenAmount4 = to;
        }
    }
}

using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using elevator_simulation.Commands;
using elevator_simulation.Models;

namespace elevator_simulation.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ElevatorModel _elevator;
        private readonly DispatcherTimer _timer;
        private DateTime _simulationStartTime;
        private bool _isSimulationRunning;

        private int _currentFloor;
        private string _elevatorState;
        private string _statusMessage;
        private string _totalTime;
        private bool _hasPassenger;
        private bool _isInnerPanelOpen;
        private int _callingFloor;
        private double _doorOpenAmount;

        public ObservableCollection<int> Floors { get; }
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

        public bool HasPassenger
        {
            get => _hasPassenger;
            set => SetProperty(ref _hasPassenger, value);
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
            Floors = new ObservableCollection<int>();
            
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
            _statusMessage = "Asansörü çaðýrmak için bir kat seçin";
            _totalTime = "00:00:00";
            _hasPassenger = false;
            _isSimulationRunning = false;
            _isInnerPanelOpen = false;
            _doorOpenAmount = 0.0;
        }

        private bool CanCallElevator(object? parameter)
        {
            if (parameter is int targetFloor)
            {
                return _elevator.State == Models.ElevatorState.Idle && !_hasPassenger;
            }
            return false;
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

                _callingFloor = callingFloor;
                StatusMessage = $"{callingFloor}. kattan asansör çaðrýldý. Gidiliyor...";
                
                await MoveToFloor(callingFloor);
                await PickUpPassenger();
                
                StatusMessage = "Yolcu bindi. Hedef katý seçin.";
                IsInnerPanelOpen = true;
            }
        }

        private bool CanSelectDestination(object? parameter)
        {
            if (parameter is int targetFloor)
            {
                return _hasPassenger && targetFloor != _currentFloor;
            }
            return false;
        }

        private async void OnSelectDestination(object? parameter)
        {
            if (parameter is int targetFloor)
            {
                IsInnerPanelOpen = false;
                StatusMessage = $"Hedef: {targetFloor}. kat. Gidiliyor...";
                
                await MoveToFloor(targetFloor);
                await DropOffPassenger();
                
                StatusMessage = "Yolcu indi. Yeni çaðrý bekleniyor.";
            }
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

        private async Task PickUpPassenger()
        {
            // Kapý açýlýyor
            _elevator.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay = "Kapý Açýlýyor";
            await AnimateDoor(0.0, 1.0, ElevatorModel.DoorOperationTime);

            // Yolcu biniyor
            _elevator.State = Models.ElevatorState.WaitingForPassenger;
            ElevatorStateDisplay = "Yolcu Biniyor";
            await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

            _elevator.HasPassenger = true;
            HasPassenger = true;

            // Yolcu bindikten sonra 1 saniye bekle
            ElevatorStateDisplay = "Kapý Kapanýyor...";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            // Kapý kapanýyor
            _elevator.State = Models.ElevatorState.DoorClosing;
            ElevatorStateDisplay = "Kapý Kapanýyor";
            await AnimateDoor(1.0, 0.0, ElevatorModel.DoorOperationTime);

            _elevator.State = Models.ElevatorState.Idle;
            ElevatorStateDisplay = "Hedef Bekleniyor";
        }

        private async Task DropOffPassenger()
        {
            // Kapý açýlýyor
            _elevator.State = Models.ElevatorState.DoorOpening;
            ElevatorStateDisplay = "Kapý Açýlýyor";
            await AnimateDoor(0.0, 1.0, ElevatorModel.DoorOperationTime);

            // Yolcu iniyor
            _elevator.State = Models.ElevatorState.WaitingForPassenger;
            ElevatorStateDisplay = "Yolcu Ýniyor";
            await Task.Delay(TimeSpan.FromSeconds(ElevatorModel.WaitingTime));

            _elevator.HasPassenger = false;
            HasPassenger = false;

            // Yolcu indikten sonra 1 saniye bekle
            ElevatorStateDisplay = "Kapý Kapanýyor...";
            await Task.Delay(TimeSpan.FromSeconds(1.0));

            // Kapý kapanýyor
            _elevator.State = Models.ElevatorState.DoorClosing;
            ElevatorStateDisplay = "Kapý Kapanýyor";
            await AnimateDoor(1.0, 0.0, ElevatorModel.DoorOperationTime);

            _elevator.State = Models.ElevatorState.Idle;
            ElevatorStateDisplay = "Beklemede";
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

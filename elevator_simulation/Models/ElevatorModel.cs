namespace elevator_simulation.Models
{
    public enum ElevatorState
    {
        Idle,
        MovingUp,
        MovingDown,
        DoorOpening,
        DoorClosing,
        WaitingForPassenger
    }

    public class ElevatorModel
    {
        public int CurrentFloor { get; set; }
        public int TargetFloor { get; set; }
        public ElevatorState State { get; set; }
        public bool HasPassenger { get; set; }
        public TimeSpan TotalSimulationTime { get; set; }

        public const int TotalFloors = 20;
        public const double FloorTravelTime = 1.0; // seconds
        public const double DoorOperationTime = 0.5; // seconds
        public const double WaitingTime = 2.0; // seconds

        public ElevatorModel()
        {
            CurrentFloor = 0;
            TargetFloor = 0;
            State = ElevatorState.Idle;
            HasPassenger = false;
            TotalSimulationTime = TimeSpan.Zero;
        }
    }
}

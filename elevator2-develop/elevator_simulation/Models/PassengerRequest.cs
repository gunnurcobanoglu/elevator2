namespace elevator_simulation.Models
{
    public class PassengerRequest
    {
        public int PickupFloor { get; set; }       // Yolcunun bindiði kat
        public int DestinationFloor { get; set; }  // Yolcunun hedef katý
        public DateTime RequestTime { get; set; }   // Ýstek zamaný
        public TimeSpan SimulationTime { get; set; }  // Simülasyon saati (ML için)
        public int ElevatorFloorAtRequest { get; set; }  // Ýstek geldiðinde asansör hangi kattaydý
        public int WaitTimeSeconds { get; set; }  // Yolcunun bekleme süresi (saniye)
        public RequestStatus Status { get; set; }   // Ýsteðin durumu

        public PassengerRequest(int pickupFloor, int destinationFloor = -1)
        {
            PickupFloor = pickupFloor;
            DestinationFloor = destinationFloor;
            RequestTime = DateTime.Now;
            SimulationTime = TimeSpan.Zero;  // Varsayýlan
            Status = RequestStatus.Pending;
        }
    }

    public enum RequestStatus
    {
        Pending,      // Bekliyor (henüz alýnmadý)
        PickedUp,     // Yolcu bindi
        Completed     // Yolcu indi
    }
}

namespace elevator_simulation.Models
{
    public class PassengerRequest
    {
        public int PickupFloor { get; set; }       // Yolcunun bindiði kat
        public int DestinationFloor { get; set; }  // Yolcunun hedef katý
        public DateTime RequestTime { get; set; }   // Ýstek zamaný
        public RequestStatus Status { get; set; }   // Ýsteðin durumu

        public PassengerRequest(int pickupFloor, int destinationFloor = -1)
        {
            PickupFloor = pickupFloor;
            DestinationFloor = destinationFloor;
            RequestTime = DateTime.Now;
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

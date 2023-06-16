namespace lab10.Models;

public class GameRentingViewModel
{
    public int GameId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime FinishDate { get; set; }
    public List<Reservation> Reservations = new List<Reservation>();
}

public class Reservation
{
    public int ReservationId { get; set; }
    public string User { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime FinishDate { get; set; }
}
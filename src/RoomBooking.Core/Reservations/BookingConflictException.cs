namespace RoomBooking.Core.Reservations;

/// <summary>Заявка пересекается с действующим бронированием аудитории.</summary>
public class BookingConflictException(string message, Reservation conflict) : DomainException(message)
{
    public Reservation Conflict { get; } = conflict;
}

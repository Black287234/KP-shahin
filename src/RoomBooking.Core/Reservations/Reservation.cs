using RoomBooking.Core.Rooms;

namespace RoomBooking.Core.Reservations;

/// <summary>
/// Бронирование аудитории на интервал времени [Start; End).
/// Бронирование не удаляется: при отмене сохраняются причина и момент отмены.
/// </summary>
public class Reservation
{
    public int Id { get; private set; }
    public int RoomId { get; private set; }
    public Room? Room { get; private set; }

    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }

    /// <summary>Ответственный — преподаватель или сотрудник.</summary>
    public string Organizer { get; private set; } = "";
    public string? StudyGroup { get; private set; }
    public string Purpose { get; private set; } = "";
    public int Attendees { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }

    public bool IsCancelled => CancelledAt != null;

    private Reservation() { }   // для Entity Framework Core

    internal Reservation(Room room, DateTime start, DateTime end, string organizer, string? studyGroup,
        string purpose, int attendees, DateTime createdAt)
    {
        Room = room;
        RoomId = room.Id;
        Start = start;
        End = end;
        Organizer = organizer;
        StudyGroup = studyGroup;
        Purpose = purpose;
        Attendees = attendees;
        CreatedAt = createdAt;
    }

    /// <summary>Учебные данные: бронирование без проверок (используется только при заполнении пустой базы).</summary>
    public static Reservation Seed(Room room, DateTime start, DateTime end, string organizer, string? studyGroup,
        string purpose, int attendees, DateTime createdAt, string? cancelReason = null)
    {
        var r = new Reservation(room, start, end, organizer, studyGroup, purpose, attendees, createdAt);
        if (cancelReason != null)
        {
            r.CancelledAt = start.AddDays(-2);
            r.CancelReason = cancelReason;
        }
        return r;
    }
}

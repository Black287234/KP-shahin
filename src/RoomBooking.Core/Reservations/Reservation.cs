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
    public TimeSlot Slot => new(Start, End);

    private Reservation() { }   // для Entity Framework Core

    internal Reservation(Room room, TimeSlot slot, string organizer, string? studyGroup,
        string purpose, int attendees, DateTime createdAt)
    {
        Room = room;
        RoomId = room.Id;
        Start = slot.Start;
        End = slot.End;
        Organizer = organizer;
        StudyGroup = studyGroup;
        Purpose = purpose;
        Attendees = attendees;
        CreatedAt = createdAt;
    }

    public ReservationState StateAt(DateTime now) =>
        IsCancelled ? ReservationState.Cancelled
        : now < Start ? ReservationState.Planned
        : now < End ? ReservationState.InProgress
        : ReservationState.Finished;

    /// <summary>Отмена допускается только до начала занятия и с указанием причины.</summary>
    public void Cancel(string? reason, DateTime now)
    {
        reason = (reason ?? "").Trim();
        if (IsCancelled) throw new DomainException("Бронирование уже отменено");
        if (now >= Start) throw new DomainException("Отменить можно только бронирование, занятие по которому ещё не началось");
        if (reason.Length < 5) throw new DomainException("Укажите причину отмены (не менее 5 символов)");
        if (reason.Length > 200) throw new DomainException("Причина отмены не должна превышать 200 символов");
        CancelledAt = now;
        CancelReason = reason;
    }

    /// <summary>Учебные данные: бронирование без проверок (используется только при заполнении пустой базы).</summary>
    public static Reservation Seed(Room room, TimeSlot slot, string organizer, string? studyGroup,
        string purpose, int attendees, DateTime createdAt, string? cancelReason = null)
    {
        var r = new Reservation(room, slot, organizer, studyGroup, purpose, attendees, createdAt);
        if (cancelReason != null)
        {
            r.CancelledAt = createdAt.AddHours(3);
            r.CancelReason = cancelReason;
        }
        return r;
    }
}

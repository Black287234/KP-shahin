using Microsoft.EntityFrameworkCore;
using RoomBooking.Core;
using RoomBooking.Core.Reservations;
using RoomBooking.Web.Data;

namespace RoomBooking.Web.Application;

/// <summary>Условия отбора бронирований в журнале.</summary>
public record ReservationQuery(DateOnly? From = null, DateOnly? To = null, int? RoomId = null,
    ReservationState? State = null, string? Organizer = null);

/// <summary>
/// Прикладной сервис бронирования: загружает данные, передаёт проверку доменной службе BookingPolicy
/// и сохраняет результат. Проверка пересечения и запись выполняются в одной транзакции.
/// </summary>
public class ReservationDesk(BookingContext db, TimeProvider clock)
{
    public DateTime Now => clock.GetLocalNow().DateTime;

    public Reservation Book(int roomId, BookingRequest request)
    {
        using var tx = db.Database.BeginTransaction();
        var room = db.Rooms.FirstOrDefault(r => r.Id == roomId) ?? throw new NotFoundException("Аудитория не найдена");
        // Для проверки пересечения достаточно бронирований этой аудитории на ту же дату
        var day = request.Slot.Start.Date;
        var sameDay = db.Reservations.Where(r => r.RoomId == roomId && r.Start >= day && r.Start < day.AddDays(1)).ToList();

        var reservation = BookingPolicy.Book(room, request, sameDay, Now);
        db.Reservations.Add(reservation);
        db.SaveChanges();
        tx.Commit();
        return reservation;
    }

    public void Cancel(int id, string? reason)
    {
        var r = db.Reservations.FirstOrDefault(x => x.Id == id) ?? throw new NotFoundException("Бронирование не найдено");
        r.Cancel(reason, Now);
        db.SaveChanges();
    }

    public Reservation? Get(int id) =>
        db.Reservations.AsNoTracking().Include(r => r.Room).FirstOrDefault(r => r.Id == id);

    public IReadOnlyList<Reservation> Find(ReservationQuery q)
    {
        var query = db.Reservations.AsNoTracking().Include(r => r.Room).AsQueryable();
        if (q.From is { } from) query = query.Where(r => r.Start >= from.ToDateTime(TimeOnly.MinValue));
        if (q.To is { } to) query = query.Where(r => r.Start < to.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (q.RoomId is { } roomId) query = query.Where(r => r.RoomId == roomId);

        IEnumerable<Reservation> list = query.OrderBy(r => r.Start).ThenBy(r => r.Room!.Code).ToList();
        var now = Now;
        if (q.State is { } state) list = list.Where(r => r.StateAt(now) == state);
        if (!string.IsNullOrWhiteSpace(q.Organizer))
            list = list.Where(r => r.Organizer.Contains(q.Organizer.Trim(), StringComparison.OrdinalIgnoreCase));
        return list.ToList();
    }

    /// <summary>Аудитории, свободные в течение всего интервала и вмещающие указанное число участников.</summary>
    public IReadOnlyList<Core.Rooms.Room> FreeRooms(TimeSlot slot, int attendees, bool projector)
    {
        var day = slot.Start.Date;
        var busyIds = db.Reservations
            .Where(r => r.CancelledAt == null && r.Start < slot.End && slot.Start < r.End && r.Start >= day && r.Start < day.AddDays(1))
            .Select(r => r.RoomId).Distinct().ToList();
        return db.Rooms.AsNoTracking()
            .Where(r => !r.IsOutOfService && r.Seats >= attendees && !busyIds.Contains(r.Id) && (!projector || r.HasProjector))
            .OrderBy(r => r.Seats).ThenBy(r => r.Code)
            .ToList();
    }
}

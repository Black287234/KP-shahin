using Microsoft.EntityFrameworkCore;
using RoomBooking.Core;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Data;

namespace RoomBooking.Web.Application;

/// <summary>Условия отбора аудиторий в каталоге.</summary>
public record RoomQuery(string? Text = null, string? Building = null, RoomKind? Kind = null,
    int? MinSeats = null, bool ProjectorOnly = false, bool IncludeOutOfService = false);

/// <summary>Сведения об аудитории, введённые в форме.</summary>
public record RoomInput(string? Code, string? Building, RoomKind Kind, int? Seats, bool HasProjector);

/// <summary>
/// Прикладной сервис каталога аудиторий: поиск, регистрация, изменение, вывод из эксплуатации.
/// Правила самой аудитории проверяет класс Room; здесь — правила, требующие обращения к базе данных.
/// </summary>
public class RoomRegistry(BookingContext db, TimeProvider clock)
{
    public IReadOnlyList<Room> Find(RoomQuery q)
    {
        // Отбор выполняется в памяти: SQLite не сравнивает кириллицу без учёта регистра
        IEnumerable<Room> rooms = db.Rooms.AsNoTracking().OrderBy(r => r.Code).ToList();
        if (!q.IncludeOutOfService) rooms = rooms.Where(r => !r.IsOutOfService);
        if (!string.IsNullOrWhiteSpace(q.Text))
            rooms = rooms.Where(r => r.Code.Contains(q.Text.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(q.Building)) rooms = rooms.Where(r => r.Building == q.Building);
        if (q.Kind is { } kind) rooms = rooms.Where(r => r.Kind == kind);
        if (q.MinSeats is { } min) rooms = rooms.Where(r => r.Seats >= min);
        if (q.ProjectorOnly) rooms = rooms.Where(r => r.HasProjector);
        return rooms.ToList();
    }

    public IReadOnlyList<string> Buildings() =>
        db.Rooms.Select(r => r.Building).Distinct().OrderBy(b => b).ToList();

    public Room? Get(int id) => db.Rooms.AsNoTracking().FirstOrDefault(r => r.Id == id);

    public Room Register(RoomInput input)
    {
        var room = new Room(input.Code ?? "", input.Building ?? "", input.Kind, input.Seats ?? 0, input.HasProjector);
        EnsureUniqueCode(room.Code, null);
        db.Rooms.Add(room);
        db.SaveChanges();
        return room;
    }

    public Room Update(int id, RoomInput input)
    {
        var room = Load(id);
        room.Change(input.Code ?? "", input.Building ?? "", input.Kind, input.Seats ?? 0, input.HasProjector);
        EnsureUniqueCode(room.Code, id);
        // Вместимость нельзя уменьшить ниже числа участников предстоящих бронирований
        var larger = Upcoming(id).Where(r => r.Attendees > room.Seats).OrderBy(r => r.Start).FirstOrDefault();
        if (larger != null)
            throw new DomainException(
                $"Нельзя уменьшить число мест до {room.Seats}: на {larger.Start:dd.MM.yyyy HH\\:mm} оформлено бронирование на {larger.Attendees} участников");
        db.SaveChanges();
        return room;
    }

    public void TakeOutOfService(int id, string? reason)
    {
        var room = Load(id);
        // Нельзя вывести аудиторию, пока на неё есть предстоящие бронирования: они остались бы без места проведения
        var upcoming = Upcoming(id);
        if (upcoming.Count > 0)
            throw new DomainException(
                $"На аудиторию {room.Code} оформлено запланированных бронирований: {upcoming.Count} (ближайшее — {upcoming.Min(r => r.Start):dd.MM.yyyy HH\\:mm}). Отмените или перенесите их перед выводом из эксплуатации");
        room.TakeOutOfService(reason ?? "");
        db.SaveChanges();
    }

    public void ReturnToService(int id)
    {
        var room = Load(id);
        room.ReturnToService();
        db.SaveChanges();
    }

    /// <summary>Действующие бронирования аудитории, занятие по которым ещё не закончилось.</summary>
    private List<Core.Reservations.Reservation> Upcoming(int roomId)
    {
        var now = clock.GetLocalNow().DateTime;
        return db.Reservations.Where(r => r.RoomId == roomId && r.CancelledAt == null && r.End > now).ToList();
    }

    private Room Load(int id) =>
        db.Rooms.FirstOrDefault(r => r.Id == id) ?? throw new NotFoundException("Аудитория не найдена");

    private void EnsureUniqueCode(string code, int? exceptId)
    {
        if (db.Rooms.Any(r => r.Code == code && r.Id != exceptId))
            throw new DomainException($"Аудитория {code} уже зарегистрирована в каталоге");
    }
}

/// <summary>Запрошенная запись отсутствует.</summary>
public class NotFoundException(string message) : Exception(message);

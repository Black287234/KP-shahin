using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Data;

namespace RoomBooking.Tests;

/// <summary>Часы с заданным текущим временем: результат тестов не зависит от момента запуска.</summary>
public sealed class StoppedClock(DateTime now) : TimeProvider
{
    public DateTime Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>Общие данные тестов: «сейчас» — вторник 17.11.2026, 09:00.</summary>
public static class Moment
{
    public static readonly DateTime Now = new(2026, 11, 17, 9, 0, 0);
    public static readonly DateOnly Today = DateOnly.FromDateTime(Now);
    public static readonly DateOnly Tomorrow = Today.AddDays(1);

    public static TimeSlot Slot(DateOnly day, string from, string to) =>
        TimeSlot.On(day, TimeOnly.Parse(from), TimeOnly.Parse(to));

    public static TimeSlot Tomorrow_(string from, string to) => Slot(Tomorrow, from, to);

    public static BookingRequest Request(TimeSlot slot, int attendees = 20, string organizer = "Крючков В. В.",
        string purpose = "Практическое занятие") => new(slot, organizer, "ИСиП-23/1", purpose, attendees);
}

/// <summary>База SQLite в оперативной памяти, существующая на время одного теста.</summary>
public sealed class MemoryDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<BookingContext> _options;

    public MemoryDatabase()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<BookingContext>().UseSqlite(_connection).Options;
        using var db = Open();
        db.Database.Migrate();   // схема создаётся той же миграцией, что и в приложении
    }

    public BookingContext Open() => new(_options);

    public Room AddRoom(string code, int seats = 30, RoomKind kind = RoomKind.Classroom, bool projector = false)
    {
        using var db = Open();
        var room = new Room(code, "Корпус А", kind, seats, projector);
        db.Rooms.Add(room);
        db.SaveChanges();
        return room;
    }

    public void Dispose() => _connection.Dispose();
}

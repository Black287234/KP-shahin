using Microsoft.EntityFrameworkCore;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Data;

namespace RoomBooking.Web.Application;

/// <summary>Отрезок шкалы занятости аудитории: свободное время или бронирование.</summary>
public record ScheduleBlock(TimeOnly From, TimeOnly To, Reservation? Reservation)
{
    public bool IsFree => Reservation is null;
    public int Minutes => (int)(To - From).TotalMinutes;
}

/// <summary>Занятость одной аудитории за день.</summary>
public record RoomDay(Room Room, IReadOnlyList<ScheduleBlock> Blocks)
{
    public int BusyMinutes => Blocks.Where(b => !b.IsFree).Sum(b => b.Minutes);
    public int LoadPercent => (int)Math.Round(100.0 * BusyMinutes / ScheduleBuilder.WorkingMinutes);
}

/// <summary>
/// Построение расписания занятости: рабочий день 08:00–21:00 делится на отрезки
/// «свободно» и «занято» по действующим (не отменённым) бронированиям.
/// </summary>
public class ScheduleBuilder(BookingContext db)
{
    public static readonly int WorkingMinutes = (int)(TimeSlot.DayEnd - TimeSlot.DayStart).TotalMinutes;

    /// <summary>Расписание всех действующих аудиторий на дату.</summary>
    public IReadOnlyList<RoomDay> Day(DateOnly date)
    {
        var rooms = db.Rooms.AsNoTracking().Where(r => !r.IsOutOfService).OrderBy(r => r.Code).ToList();
        var active = Load(date, date, null);
        return rooms.Select(room => new RoomDay(room, Split(active.Where(r => r.RoomId == room.Id)))).ToList();
    }

    /// <summary>Расписание одной аудитории на неделю (понедельник — суббота), начиная с понедельника недели даты.</summary>
    public IReadOnlyList<(DateOnly Date, IReadOnlyList<ScheduleBlock> Blocks)> Week(int roomId, DateOnly anyDay)
    {
        var monday = MondayOf(anyDay);
        var active = Load(monday, monday.AddDays(5), roomId);
        return Enumerable.Range(0, 6)
            .Select(i => monday.AddDays(i))
            .Select(d => (d, Split(active.Where(r => DateOnly.FromDateTime(r.Start) == d))))
            .ToList();
    }

    public static DateOnly MondayOf(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    /// <summary>Разбиение рабочего дня на отрезки по упорядоченным бронированиям.</summary>
    public static IReadOnlyList<ScheduleBlock> Split(IEnumerable<Reservation> reservations)
    {
        var blocks = new List<ScheduleBlock>();
        var cursor = TimeSlot.DayStart;
        foreach (var r in reservations.Where(x => !x.IsCancelled).OrderBy(x => x.Start))
        {
            var from = TimeOnly.FromDateTime(r.Start);
            var to = TimeOnly.FromDateTime(r.End);
            if (from > cursor) blocks.Add(new ScheduleBlock(cursor, from, null));
            blocks.Add(new ScheduleBlock(from, to, r));
            if (to > cursor) cursor = to;
        }
        if (cursor < TimeSlot.DayEnd) blocks.Add(new ScheduleBlock(cursor, TimeSlot.DayEnd, null));
        return blocks;
    }

    private List<Reservation> Load(DateOnly from, DateOnly to, int? roomId)
    {
        var a = from.ToDateTime(TimeOnly.MinValue);
        var b = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var q = db.Reservations.AsNoTracking().Include(r => r.Room)
            .Where(r => r.CancelledAt == null && r.Start >= a && r.Start < b);
        if (roomId is { } id) q = q.Where(r => r.RoomId == id);
        return q.OrderBy(r => r.Start).ToList();
    }
}

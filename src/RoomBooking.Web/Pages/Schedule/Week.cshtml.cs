using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Schedule;

/// <summary>Недельное расписание одной аудитории.</summary>
public class WeekModel(ScheduleBuilder schedule, RoomRegistry rooms, ReservationDesk desk) : PageModel
{
    [BindProperty(SupportsGet = true)] public int? RoomId { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? Date { get; set; }

    public IReadOnlyList<Room> Rooms { get; private set; } = [];
    public Room? Room { get; private set; }
    public DateOnly Monday { get; private set; }
    public IReadOnlyList<(DateOnly Date, IReadOnlyList<ScheduleBlock> Blocks)> Days { get; private set; } = [];

    public IActionResult OnGet()
    {
        Rooms = rooms.Find(new RoomQuery());
        Monday = ScheduleBuilder.MondayOf(Date ?? DateOnly.FromDateTime(desk.Now));
        var id = RoomId ?? Rooms.FirstOrDefault()?.Id;
        if (id is null) return Page();
        Room = rooms.Get(id.Value);
        if (Room is null) return NotFound();
        RoomId = Room.Id;
        Days = schedule.Week(Room.Id, Monday);
        return Page();
    }

    public IEnumerable<Reservation> Listed => Days.SelectMany(d => d.Blocks).Where(b => !b.IsFree).Select(b => b.Reservation!);
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core.Reservations;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages;

/// <summary>Сводка на текущий день.</summary>
public class IndexModel(ReservationDesk desk, RoomRegistry rooms) : PageModel
{
    public DateTime Now { get; private set; }
    public int RoomsInService { get; private set; }
    public int TodayCount { get; private set; }
    public int InProgress { get; private set; }
    public int FreeNow { get; private set; }
    public IReadOnlyList<Reservation> Upcoming { get; private set; } = [];

    public void OnGet()
    {
        Now = desk.Now;
        var today = DateOnly.FromDateTime(Now);
        var inService = rooms.Find(new RoomQuery());
        var todays = desk.Find(new ReservationQuery(today, today)).Where(r => !r.IsCancelled).ToList();
        RoomsInService = inService.Count;
        TodayCount = todays.Count;
        var busyNow = todays.Where(r => r.StateAt(Now) == ReservationState.InProgress).ToList();
        InProgress = busyNow.Count;
        FreeNow = inService.Count(r => busyNow.All(b => b.RoomId != r.Id));
        Upcoming = desk.Find(new ReservationQuery(today, today.AddDays(7), State: ReservationState.Planned)).Take(8).ToList();
    }
}

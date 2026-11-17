using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Schedule;

/// <summary>Расписание занятости всех аудиторий на выбранный день.</summary>
public class DayModel(ScheduleBuilder schedule, ReservationDesk desk) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Date { get; set; }
    public DateOnly Day { get; private set; }
    public IReadOnlyList<RoomDay> Rows { get; private set; } = [];

    public void OnGet()
    {
        Day = Date ?? DateOnly.FromDateTime(desk.Now);
        Rows = schedule.Day(Day);
    }
}

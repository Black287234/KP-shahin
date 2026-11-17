using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Schedule;

/// <summary>Поиск аудиторий, свободных в заданный интервал времени.</summary>
public class FreeModel(ReservationDesk desk) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? Date { get; set; }
    [BindProperty(SupportsGet = true)] public TimeOnly? From { get; set; }
    [BindProperty(SupportsGet = true)] public TimeOnly? To { get; set; }
    [BindProperty(SupportsGet = true)] public int? Seats { get; set; }
    [BindProperty(SupportsGet = true)] public bool Projector { get; set; }

    public IReadOnlyList<Room>? Found { get; private set; }
    public string? Problem { get; private set; }

    public void OnGet()
    {
        var searched = Date != null || From != null || To != null;
        Date ??= DateOnly.FromDateTime(desk.Now).AddDays(1);
        From ??= new TimeOnly(12, 20);
        To ??= new TimeOnly(13, 50);
        if (!searched) return;
        if (!ModelState.IsValid)
        {
            Problem = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return;
        }
        try
        {
            Found = desk.FreeRooms(TimeSlot.On(Date.Value, From.Value, To.Value), Math.Max(1, Seats ?? 1), Projector);
        }
        catch (DomainException ex)
        {
            Problem = ex.Message;
        }
    }
}

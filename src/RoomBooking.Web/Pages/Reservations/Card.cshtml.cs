using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core;
using RoomBooking.Core.Reservations;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Reservations;

public class CardModel(ReservationDesk desk) : PageModel
{
    public Reservation Item { get; private set; } = null!;
    public ReservationState State { get; private set; }

    [BindProperty] public string? Reason { get; set; }

    public IActionResult OnGet(int id)
    {
        var r = desk.Get(id);
        if (r is null) return NotFound();
        Item = r;
        State = r.StateAt(desk.Now);
        return Page();
    }

    public IActionResult OnPostCancel(int id)
    {
        try
        {
            desk.Cancel(id, Reason);
            TempData["Ok"] = "Бронирование отменено";
        }
        catch (DomainException ex) { TempData["Err"] = ex.Message; }
        catch (NotFoundException) { return NotFound(); }
        return RedirectToPage(new { id });
    }
}

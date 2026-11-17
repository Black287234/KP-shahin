using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Rooms;

public class CardModel(RoomRegistry registry) : PageModel
{
    public Room Room { get; private set; } = null!;

    [BindProperty] public string? Reason { get; set; }

    public IActionResult OnGet(int id)
    {
        var room = registry.Get(id);
        if (room is null) return NotFound();   // страницу «Запись не найдена» выводит UseStatusCodePagesWithReExecute
        Room = room;
        return Page();
    }

    public IActionResult OnPostOutOfService(int id) =>
        Run(id, () => registry.TakeOutOfService(id, Reason), "Аудитория выведена из эксплуатации");

    public IActionResult OnPostReturn(int id) =>
        Run(id, () => registry.ReturnToService(id), "Аудитория возвращена в эксплуатацию");

    private IActionResult Run(int id, Action action, string done)
    {
        try
        {
            action();
            TempData["Ok"] = done;
        }
        catch (DomainException ex)
        {
            TempData["Err"] = ex.Message;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        return RedirectToPage(new { id });
    }
}

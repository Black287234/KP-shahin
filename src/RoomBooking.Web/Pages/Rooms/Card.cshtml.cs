using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Infrastructure;

namespace RoomBooking.Web.Pages.Rooms;

public class CardModel(InMemoryRoomCatalog catalog) : PageModel
{
    public Room Room { get; private set; } = null!;

    public IActionResult OnGet(int id)
    {
        var room = catalog.Find(id);
        if (room is null) return NotFound();   // страницу «Запись не найдена» выводит UseStatusCodePagesWithReExecute
        Room = room;
        return Page();
    }
}

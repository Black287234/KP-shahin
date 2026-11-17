using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Infrastructure;

namespace RoomBooking.Web.Pages.Rooms;

public class IndexModel(InMemoryRoomCatalog catalog) : PageModel
{
    public IReadOnlyList<Room> Rooms { get; private set; } = [];

    public void OnGet() => Rooms = catalog.All();
}

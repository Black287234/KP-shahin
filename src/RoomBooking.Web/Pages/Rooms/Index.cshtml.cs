using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Rooms;

public class IndexModel(RoomRegistry registry) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Text { get; set; }
    [BindProperty(SupportsGet = true)] public string? Building { get; set; }
    [BindProperty(SupportsGet = true)] public RoomKind? Kind { get; set; }
    [BindProperty(SupportsGet = true)] public int? MinSeats { get; set; }
    [BindProperty(SupportsGet = true)] public bool Projector { get; set; }
    [BindProperty(SupportsGet = true)] public bool All { get; set; }

    public IReadOnlyList<Room> Rooms { get; private set; } = [];
    public IReadOnlyList<string> Buildings { get; private set; } = [];

    public void OnGet()
    {
        Buildings = registry.Buildings();
        Rooms = registry.Find(new RoomQuery(Text, Building, Kind, MinSeats, Projector, All));
    }
}

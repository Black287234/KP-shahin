using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Reservations;

/// <summary>Журнал бронирований с отбором по периоду, аудитории, состоянию и ответственному.</summary>
public class IndexModel(ReservationDesk desk, RoomRegistry rooms) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? To { get; set; }
    [BindProperty(SupportsGet = true)] public int? RoomId { get; set; }
    [BindProperty(SupportsGet = true)] public ReservationState? State { get; set; }
    [BindProperty(SupportsGet = true)] public string? Organizer { get; set; }

    public IReadOnlyList<Reservation> Items { get; private set; } = [];
    public IReadOnlyList<Room> Rooms { get; private set; } = [];
    public DateTime Now { get; private set; }
    public string? Problem { get; private set; }

    public void OnGet()
    {
        Now = desk.Now;
        Rooms = rooms.Find(new RoomQuery(IncludeOutOfService: true));
        // По умолчанию журнал показывает бронирования с сегодняшнего дня на две недели вперёд
        if (From is null && To is null && RoomId is null && State is null && string.IsNullOrWhiteSpace(Organizer))
        {
            From = DateOnly.FromDateTime(Now);
            To = From.Value.AddDays(14);
        }
        if (From > To)
        {
            Problem = "Начало периода позже его окончания";
            return;
        }
        Items = desk.Find(new ReservationQuery(From, To, RoomId, State, Organizer));
    }
}

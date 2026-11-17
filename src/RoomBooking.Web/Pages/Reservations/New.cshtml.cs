using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Reservations;

/// <summary>Оформление бронирования. При пересечении предлагаются свободные аудитории на то же время.</summary>
public class NewModel(ReservationDesk desk, RoomRegistry rooms) : PageModel
{
    public class Form
    {
        [Display(Name = "Аудитория")] public int? RoomId { get; set; }
        [Display(Name = "Дата")] public DateOnly? Date { get; set; }
        [Display(Name = "Начало")] public TimeOnly? From { get; set; }
        [Display(Name = "Окончание")] public TimeOnly? To { get; set; }
        [Display(Name = "Ответственный")] public string? Organizer { get; set; }
        [Display(Name = "Группа")] public string? StudyGroup { get; set; }
        [Display(Name = "Цель бронирования")] public string? Purpose { get; set; }
        [Display(Name = "Участников")] public int? Attendees { get; set; }
    }

    [BindProperty] public Form Input { get; set; } = new();
    public IReadOnlyList<Room> Rooms { get; private set; } = [];
    public IReadOnlyList<Room> Alternatives { get; private set; } = [];

    public void OnGet(int? roomId, DateOnly? date, TimeOnly? from, TimeOnly? to)
    {
        Rooms = rooms.Find(new RoomQuery());
        var tomorrow = DateOnly.FromDateTime(desk.Now).AddDays(1);
        Input = new Form { RoomId = roomId, Date = date ?? tomorrow, From = from ?? new(10, 10), To = to ?? new(11, 40), Attendees = 20 };
    }

    public IActionResult OnPost()
    {
        Rooms = rooms.Find(new RoomQuery());
        if (!ModelState.IsValid) return Page();
        if (Input.RoomId is null) ModelState.AddModelError("Input.RoomId", "Выберите аудиторию");
        if (Input.Date is null) ModelState.AddModelError("Input.Date", "Укажите дату");
        if (Input.From is null || Input.To is null) ModelState.AddModelError("Input.From", "Укажите время начала и окончания");
        if (!ModelState.IsValid) return Page();

        try
        {
            var slot = TimeSlot.On(Input.Date!.Value, Input.From!.Value, Input.To!.Value);
            var request = new BookingRequest(slot, Input.Organizer, Input.StudyGroup, Input.Purpose, Input.Attendees ?? 0);
            try
            {
                var r = desk.Book(Input.RoomId!.Value, request);
                TempData["Ok"] = $"Аудитория забронирована: {slot}";
                return RedirectToPage("/Reservations/Card", new { id = r.Id });
            }
            catch (BookingConflictException ex)
            {
                ModelState.AddModelError("", ex.Message);
                Alternatives = desk.FreeRooms(slot, Math.Max(1, Input.Attendees ?? 1), false);
                return Page();
            }
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return Page();
        }
        catch (NotFoundException ex)
        {
            ModelState.AddModelError("Input.RoomId", ex.Message);
            return Page();
        }
    }
}

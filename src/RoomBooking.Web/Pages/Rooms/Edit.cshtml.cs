using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoomBooking.Core;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Application;

namespace RoomBooking.Web.Pages.Rooms;

/// <summary>Регистрация новой аудитории (без id) и изменение существующей (с id).</summary>
public class EditModel(RoomRegistry registry) : PageModel
{
    public class Form
    {
        [Display(Name = "Номер аудитории")] public string? Code { get; set; }
        [Display(Name = "Корпус")] public string? Building { get; set; }
        [Display(Name = "Назначение")] public RoomKind Kind { get; set; } = RoomKind.Classroom;
        [Display(Name = "Количество мест")] public int? Seats { get; set; }
        [Display(Name = "Есть проектор")] public bool HasProjector { get; set; }
    }

    [BindProperty] public Form Input { get; set; } = new();
    public int? Id { get; private set; }
    public bool IsNew => Id is null;

    public IActionResult OnGet(int? id)
    {
        Id = id;
        if (id is null) return Page();
        var room = registry.Get(id.Value);
        if (room is null) return NotFound();
        Input = new Form { Code = room.Code, Building = room.Building, Kind = room.Kind, Seats = room.Seats, HasProjector = room.HasProjector };
        return Page();
    }

    public IActionResult OnPost(int? id)
    {
        Id = id;
        if (!ModelState.IsValid) return Page();
        var input = new RoomInput(Input.Code, Input.Building, Input.Kind, Input.Seats, Input.HasProjector);
        try
        {
            var room = id is null ? registry.Register(input) : registry.Update(id.Value, input);
            TempData["Ok"] = id is null ? $"Аудитория {room.Code} зарегистрирована" : $"Сведения об аудитории {room.Code} сохранены";
            return RedirectToPage("/Rooms/Card", new { id = room.Id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return Page();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Core.Rooms;

/// <summary>Назначение аудитории.</summary>
public enum RoomKind
{
    [Display(Name = "Учебная")] Classroom,
    [Display(Name = "Лекционная")] LectureHall,
    [Display(Name = "Компьютерный класс")] ComputerLab,
    [Display(Name = "Лаборатория")] Laboratory,
    [Display(Name = "Актовый зал")] AssemblyHall
}

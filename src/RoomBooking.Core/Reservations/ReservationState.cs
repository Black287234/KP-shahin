using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Core.Reservations;

/// <summary>Состояние бронирования; вычисляется по текущему времени и не хранится в базе данных.</summary>
public enum ReservationState
{
    [Display(Name = "Запланировано")] Planned,
    [Display(Name = "Идёт занятие")] InProgress,
    [Display(Name = "Завершено")] Finished,
    [Display(Name = "Отменено")] Cancelled
}

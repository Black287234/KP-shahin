using RoomBooking.Core.Rooms;

namespace RoomBooking.Core.Reservations;

/// <summary>
/// Доменная служба оформления бронирования. Проверяет заявку по правилам колледжа
/// и по уже существующим бронированиям аудитории; база данных здесь не используется.
/// </summary>
public static class BookingPolicy
{
    /// <summary>Наибольший срок, на который вперёд принимаются заявки.</summary>
    public const int MaxDaysAhead = 60;

    public static Reservation Book(Room room, BookingRequest request, IEnumerable<Reservation> roomReservations, DateTime now)
    {
        var organizer = (request.Organizer ?? "").Trim();
        var group = string.IsNullOrWhiteSpace(request.StudyGroup) ? null : request.StudyGroup.Trim().ToUpperInvariant();
        var purpose = (request.Purpose ?? "").Trim();
        var slot = request.Slot;

        if (room.IsOutOfService)
            throw new DomainException($"Аудитория {room.Code} выведена из эксплуатации: {room.OutOfServiceReason}");
        if (organizer.Length < 3) throw new DomainException("Укажите ответственного (не менее 3 символов)");
        if (organizer.Length > 100) throw new DomainException("ФИО ответственного не должно превышать 100 символов");
        if (group?.Length > 30) throw new DomainException("Название группы не должно превышать 30 символов");
        if (purpose.Length < 3) throw new DomainException("Укажите цель бронирования (не менее 3 символов)");
        if (purpose.Length > 200) throw new DomainException("Цель бронирования не должна превышать 200 символов");
        if (request.Attendees < 1) throw new DomainException("Количество участников должно быть не менее 1");
        if (request.Attendees > room.Seats)
            throw new DomainException($"Участников ({request.Attendees}) больше, чем мест в аудитории {room.Code} ({room.Seats})");
        if (slot.Start <= now) throw new DomainException("Нельзя забронировать аудиторию на прошедшее время");
        if (slot.Date > DateOnly.FromDateTime(now).AddDays(MaxDaysAhead))
            throw new DomainException($"Заявки принимаются не более чем на {MaxDaysAhead} дней вперёд");

        var conflict = FindConflict(roomReservations, slot);
        if (conflict != null)
            throw new BookingConflictException(
                $"Аудитория {room.Code} уже занята {conflict.Start:dd.MM.yyyy} с {conflict.Start:HH\\:mm} до {conflict.End:HH\\:mm} ({conflict.Purpose}, {conflict.Organizer})", conflict);

        return new Reservation(room, slot, organizer, group, purpose, request.Attendees, now);
    }

    /// <summary>Первое действующее (не отменённое) бронирование, пересекающееся с интервалом.</summary>
    public static Reservation? FindConflict(IEnumerable<Reservation> roomReservations, TimeSlot slot) =>
        roomReservations.Where(r => !r.IsCancelled && slot.Overlaps(r.Start, r.End))
                        .OrderBy(r => r.Start)
                        .FirstOrDefault();
}

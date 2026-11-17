namespace RoomBooking.Core.Reservations;

/// <summary>Сведения заявки на бронирование, введённые пользователем.</summary>
public record BookingRequest(TimeSlot Slot, string? Organizer, string? StudyGroup, string? Purpose, int Attendees);

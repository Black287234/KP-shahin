namespace RoomBooking.Core;

/// <summary>
/// Нарушение правила предметной области. Сообщение предназначено для пользователя,
/// поэтому формулируется по-русски и без технических подробностей.
/// </summary>
public class DomainException(string message) : Exception(message);

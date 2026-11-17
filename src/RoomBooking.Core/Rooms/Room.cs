namespace RoomBooking.Core.Rooms;

/// <summary>
/// Учебная аудитория. Свойства изменяются только через методы класса,
/// которые проверяют данные и при нарушении правил выбрасывают DomainException.
/// </summary>
public class Room
{
    public const int MaxSeats = 500;

    public int Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Building { get; private set; } = "";
    public RoomKind Kind { get; private set; }
    public int Seats { get; private set; }
    public bool HasProjector { get; private set; }

    private Room() { }   // для Entity Framework Core

    public Room(string code, string building, RoomKind kind, int seats, bool hasProjector)
    {
        Change(code, building, kind, seats, hasProjector);
    }

    /// <summary>Изменение сведений об аудитории с проверкой данных.</summary>
    public void Change(string code, string building, RoomKind kind, int seats, bool hasProjector)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        building = (building ?? "").Trim();

        if (code.Length == 0) throw new DomainException("Не указан номер аудитории");
        if (code.Length > 20) throw new DomainException("Номер аудитории не должен превышать 20 символов");
        if (building.Length == 0) throw new DomainException("Не указан корпус");
        if (building.Length > 50) throw new DomainException("Название корпуса не должно превышать 50 символов");
        if (!Enum.IsDefined(kind)) throw new DomainException("Неизвестное назначение аудитории");
        if (seats < 1 || seats > MaxSeats) throw new DomainException($"Количество мест должно быть от 1 до {MaxSeats}");

        Code = code;
        Building = building;
        Kind = kind;
        Seats = seats;
        HasProjector = hasProjector;
    }

    /// <summary>Присвоение идентификатора (используется хранилищем этапа 1).</summary>
    internal void AssignId(int id) => Id = id;
}

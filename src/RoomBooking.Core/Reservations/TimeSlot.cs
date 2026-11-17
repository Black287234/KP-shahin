namespace RoomBooking.Core.Reservations;

/// <summary>
/// Интервал времени занятия аудитории в пределах одного дня: [Start; End).
/// Объект-значение: создаётся только корректным, сравнивается по содержимому.
/// </summary>
public readonly record struct TimeSlot
{
    /// <summary>Начало рабочего дня колледжа.</summary>
    public static readonly TimeOnly DayStart = new(8, 0);
    /// <summary>Окончание рабочего дня колледжа.</summary>
    public static readonly TimeOnly DayEnd = new(21, 0);
    public const int MinMinutes = 30;
    public const int MaxMinutes = 360;
    public const int StepMinutes = 5;

    public DateTime Start { get; }
    public DateTime End { get; }

    public TimeSlot(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new DomainException("Время окончания должно быть позже времени начала");
        if (start.Date != end.Date)
            throw new DomainException("Бронирование должно начинаться и заканчиваться в один день");
        if (TimeOnly.FromDateTime(start) < DayStart || TimeOnly.FromDateTime(end) > DayEnd)
            throw new DomainException($"Бронирование возможно с {DayStart:HH\\:mm} до {DayEnd:HH\\:mm}");
        if (start.Minute % StepMinutes != 0 || end.Minute % StepMinutes != 0 || start.Second != 0 || end.Second != 0)
            throw new DomainException($"Время указывается с шагом {StepMinutes} минут");
        var minutes = (end - start).TotalMinutes;
        if (minutes < MinMinutes)
            throw new DomainException($"Продолжительность бронирования — не менее {MinMinutes} минут");
        if (minutes > MaxMinutes)
            throw new DomainException($"Продолжительность бронирования — не более {MaxMinutes / 60} часов");
        Start = start;
        End = end;
    }

    public static TimeSlot On(DateOnly date, TimeOnly from, TimeOnly to) =>
        new(date.ToDateTime(from), date.ToDateTime(to));

    public DateOnly Date => DateOnly.FromDateTime(Start);
    public int Minutes => (int)(End - Start).TotalMinutes;

    /// <summary>
    /// Пересечение полуинтервалов: [a1; a2) и [b1; b2) пересекаются, если a1 &lt; b2 и b1 &lt; a2.
    /// Смежные интервалы (одно заканчивается, когда начинается другое) не пересекаются.
    /// </summary>
    public bool Overlaps(DateTime otherStart, DateTime otherEnd) => Start < otherEnd && otherStart < End;

    public override string ToString() => $"{Start:dd.MM.yyyy} {Start:HH\\:mm}–{End:HH\\:mm}";
}

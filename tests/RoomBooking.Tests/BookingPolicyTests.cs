using RoomBooking.Core;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;
using static RoomBooking.Tests.Moment;

namespace RoomBooking.Tests;

[TestFixture, Description("Доменная служба оформления бронирования")]
public class BookingPolicyTests
{
    private Room _room = null!;
    private List<Reservation> _existing = null!;

    [SetUp]
    public void Prepare()
    {
        _room = new Room("А-112", "Корпус А", RoomKind.ComputerLab, 25, true);
        _existing = [BookingPolicy.Book(_room, Request(Tomorrow_("10:10", "11:40")), [], Now)];
    }

    private Reservation Book(TimeSlot slot, int attendees = 20) =>
        BookingPolicy.Book(_room, Request(slot, attendees), _existing, Now);

    [Test]
    public void Свободный_интервал_бронируется()
    {
        var r = Book(Tomorrow_("12:20", "13:50"));
        Assert.Multiple(() =>
        {
            Assert.That(r.Start, Is.EqualTo(Tomorrow.ToDateTime(new TimeOnly(12, 20))));
            Assert.That(r.StudyGroup, Is.EqualTo("ИСИП-23/1"));
            Assert.That(r.CreatedAt, Is.EqualTo(Now));
            Assert.That(r.StateAt(Now), Is.EqualTo(ReservationState.Planned));
        });
    }

    [Test]
    public void Пересечение_отклоняется_с_указанием_занятого_времени()
    {
        var ex = Assert.Throws<BookingConflictException>(() => Book(Tomorrow_("11:00", "12:00")));
        Assert.That(ex!.Message, Is.EqualTo("Аудитория А-112 уже занята 18.11.2026 с 10:10 до 11:40 (Практическое занятие, Крючков В. В.)"));
        Assert.That(ex.Conflict, Is.SameAs(_existing[0]));
    }

    [Test]
    public void Смежный_интервал_бронируется()
    {
        Assert.DoesNotThrow(() => Book(Tomorrow_("11:40", "12:20")));
        Assert.DoesNotThrow(() => Book(Tomorrow_("09:00", "10:10")));
    }

    [Test]
    public void Отменённое_бронирование_не_занимает_время()
    {
        _existing[0].Cancel("Перенос занятия", Now);
        Assert.DoesNotThrow(() => Book(Tomorrow_("10:10", "11:40")));
    }

    [Test]
    public void Бронирование_другой_даты_не_мешает()
    {
        Assert.DoesNotThrow(() => Book(Slot(Tomorrow.AddDays(1), "10:10", "11:40")));
    }

    [TestCase(26, "больше, чем мест")]
    [TestCase(0, "не менее 1")]
    public void Число_участников_проверяется(int attendees, string expected)
    {
        var ex = Assert.Throws<DomainException>(() => Book(Tomorrow_("14:00", "15:30"), attendees));
        Assert.That(ex!.Message, Does.Contain(expected));
    }

    [Test]
    public void Прошедшее_время_отклоняется()
    {
        var ex = Assert.Throws<DomainException>(() => Book(Slot(Today, "08:00", "09:00")));
        Assert.That(ex!.Message, Does.Contain("прошедшее время"));
    }

    [Test]
    public void Заявка_дальше_60_дней_отклоняется()
    {
        Assert.DoesNotThrow(() => Book(Slot(Today.AddDays(60), "10:00", "11:00")));
        var ex = Assert.Throws<DomainException>(() => Book(Slot(Today.AddDays(61), "10:00", "11:00")));
        Assert.That(ex!.Message, Does.Contain("60 дней"));
    }

    [Test]
    public void Аудитория_вне_эксплуатации_не_бронируется()
    {
        _room.TakeOutOfService("Замена проектора");
        var ex = Assert.Throws<DomainException>(() => Book(Tomorrow_("14:00", "15:30")));
        Assert.That(ex!.Message, Does.Contain("выведена из эксплуатации: Замена проектора"));
    }

    [TestCase("", "Лекция", "ответственного")]
    [TestCase("Ив", "Лекция", "ответственного")]
    [TestCase("Иванов И. И.", " ", "цель")]
    public void Обязательные_сведения_проверяются(string organizer, string purpose, string expected)
    {
        var request = Request(Tomorrow_("14:00", "15:30"), organizer: organizer, purpose: purpose);
        var ex = Assert.Throws<DomainException>(() => BookingPolicy.Book(_room, request, _existing, Now));
        Assert.That(ex!.Message, Does.Contain(expected));
    }
}

[TestFixture, Description("Состояние и отмена бронирования")]
public class ReservationTests
{
    private Reservation _r = null!;

    [SetUp]
    public void Prepare()
    {
        var room = new Room("Б-203", "Корпус Б", RoomKind.Classroom, 28, false);
        _r = BookingPolicy.Book(room, Request(Tomorrow_("10:10", "11:40")), [], Now);
    }

    [Test]
    public void Состояние_зависит_от_текущего_времени()
    {
        var start = _r.Start;
        Assert.Multiple(() =>
        {
            Assert.That(_r.StateAt(start.AddMinutes(-1)), Is.EqualTo(ReservationState.Planned));
            Assert.That(_r.StateAt(start), Is.EqualTo(ReservationState.InProgress));
            Assert.That(_r.StateAt(_r.End.AddMinutes(-1)), Is.EqualTo(ReservationState.InProgress));
            Assert.That(_r.StateAt(_r.End), Is.EqualTo(ReservationState.Finished));
        });
    }

    [Test]
    public void Отмена_сохраняет_причину_и_время()
    {
        _r.Cancel("  Перенос занятия  ", Now);
        Assert.Multiple(() =>
        {
            Assert.That(_r.IsCancelled, Is.True);
            Assert.That(_r.CancelReason, Is.EqualTo("Перенос занятия"));
            Assert.That(_r.CancelledAt, Is.EqualTo(Now));
            Assert.That(_r.StateAt(_r.Start.AddMinutes(10)), Is.EqualTo(ReservationState.Cancelled));
        });
    }

    [Test]
    public void Отмена_без_причины_отклоняется() =>
        Assert.That(Assert.Throws<DomainException>(() => _r.Cancel("", Now))!.Message, Does.Contain("причину"));

    [Test]
    public void Повторная_отмена_отклоняется()
    {
        _r.Cancel("Перенос занятия", Now);
        Assert.That(Assert.Throws<DomainException>(() => _r.Cancel("Ещё раз", Now))!.Message, Does.Contain("уже отменено"));
    }

    [Test]
    public void Начавшееся_занятие_не_отменяется() =>
        Assert.That(Assert.Throws<DomainException>(() => _r.Cancel("Поздняя отмена", _r.Start.AddMinutes(5)))!.Message,
            Does.Contain("ещё не началось"));
}

using RoomBooking.Core;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;
using RoomBooking.Web.Application;
using static RoomBooking.Tests.Moment;

namespace RoomBooking.Tests;

/// <summary>Прикладные сервисы вместе с базой данных SQLite (в оперативной памяти).</summary>
[TestFixture, Description("Сервисы бронирования и каталога с базой данных")]
public class ServiceTests
{
    private MemoryDatabase _base = null!;
    private StoppedClock _clock = null!;
    private Room _a112 = null!, _b203 = null!, _a101 = null!;

    [SetUp]
    public void Prepare()
    {
        _base = new MemoryDatabase();
        _clock = new StoppedClock(Now);
        _a112 = _base.AddRoom("А-112", 25, RoomKind.ComputerLab, projector: true);
        _b203 = _base.AddRoom("Б-203", 28);
        _a101 = _base.AddRoom("А-101", 120, RoomKind.LectureHall, projector: true);
    }

    [TearDown]
    public void Release() => _base.Dispose();

    private Reservation Book(Room room, TimeSlot slot, int attendees = 20)
    {
        using var db = _base.Open();
        return new ReservationDesk(db, _clock).Book(room.Id, Request(slot, attendees));
    }

    [Test]
    public void Бронирование_сохраняется_в_базе()
    {
        var id = Book(_a112, Tomorrow_("10:10", "11:40")).Id;
        using var db = _base.Open();
        var saved = new ReservationDesk(db, _clock).Get(id)!;
        Assert.Multiple(() =>
        {
            Assert.That(saved.Room!.Code, Is.EqualTo("А-112"));
            Assert.That(saved.End, Is.EqualTo(Tomorrow.ToDateTime(new TimeOnly(11, 40))));
            Assert.That(saved.IsCancelled, Is.False);
        });
    }

    [Test]
    public void Пересечение_проверяется_по_данным_базы()
    {
        Book(_a112, Tomorrow_("10:10", "11:40"));
        Assert.Throws<BookingConflictException>(() => Book(_a112, Tomorrow_("11:00", "12:00")));
        Assert.DoesNotThrow(() => Book(_b203, Tomorrow_("11:00", "12:00")), "Другая аудитория в то же время свободна");
    }

    [Test]
    public void Отмена_освобождает_интервал()
    {
        var r = Book(_a112, Tomorrow_("10:10", "11:40"));
        using (var db = _base.Open()) new ReservationDesk(db, _clock).Cancel(r.Id, "Перенос занятия");
        Assert.DoesNotThrow(() => Book(_a112, Tomorrow_("10:10", "11:40")));
    }

    [Test]
    public void Бронирование_несуществующей_аудитории()
    {
        using var db = _base.Open();
        Assert.Throws<NotFoundException>(() => new ReservationDesk(db, _clock).Book(999, Request(Tomorrow_("10:10", "11:40"))));
    }

    [Test]
    public void Поиск_свободных_аудиторий()
    {
        Book(_a112, Tomorrow_("10:10", "11:40"));
        using var db = _base.Open();
        var desk = new ReservationDesk(db, _clock);
        var codes = (TimeSlot slot, int people, bool projector) => desk.FreeRooms(slot, people, projector).Select(r => r.Code).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(codes(Tomorrow_("11:00", "12:00"), 1, false), Is.EqualTo(new[] { "Б-203", "А-101" }), "по возрастанию вместимости");
            Assert.That(codes(Tomorrow_("11:40", "12:20"), 1, false), Does.Contain("А-112"), "смежный интервал свободен");
            Assert.That(codes(Tomorrow_("11:00", "12:00"), 30, false), Is.EqualTo(new[] { "А-101" }));
            Assert.That(codes(Tomorrow_("08:00", "09:00"), 1, true), Is.EqualTo(new[] { "А-112", "А-101" }));
        });
    }

    [Test]
    public void Журнал_отбирает_по_состоянию_и_ответственному()
    {
        var a = Book(_a112, Tomorrow_("10:10", "11:40"));
        Book(_b203, Tomorrow_("12:20", "13:50"));
        using var db = _base.Open();
        var desk = new ReservationDesk(db, _clock);
        desk.Cancel(a.Id, "Перенос занятия");

        Assert.Multiple(() =>
        {
            Assert.That(desk.Find(new ReservationQuery()).Count, Is.EqualTo(2));
            Assert.That(desk.Find(new ReservationQuery(State: ReservationState.Cancelled)).Single().Id, Is.EqualTo(a.Id));
            Assert.That(desk.Find(new ReservationQuery(Organizer: "крючков")).Count, Is.EqualTo(2), "без учёта регистра");
            Assert.That(desk.Find(new ReservationQuery(From: Tomorrow.AddDays(1))), Is.Empty);
        });
    }

    [Test]
    public void Расписание_дня_делится_на_свободные_и_занятые_отрезки()
    {
        Book(_a112, Tomorrow_("10:10", "11:40"));
        Book(_a112, Tomorrow_("11:40", "13:50"));
        using var db = _base.Open();
        var row = new ScheduleBuilder(db).Day(Tomorrow).Single(d => d.Room.Code == "А-112");

        var text = string.Join(" ", row.Blocks.Select(b => $"{b.From:HH\\:mm}-{b.To:HH\\:mm}{(b.IsFree ? "" : "*")}"));
        Assert.That(text, Is.EqualTo("08:00-10:10 10:10-11:40* 11:40-13:50* 13:50-21:00"));
        Assert.That(row.BusyMinutes, Is.EqualTo(220));
        Assert.That(row.LoadPercent, Is.EqualTo(28));   // 220 из 780 минут
    }

    [Test]
    public void Неделя_начинается_с_понедельника()
    {
        Assert.That(ScheduleBuilder.MondayOf(new DateOnly(2026, 11, 22)), Is.EqualTo(new DateOnly(2026, 11, 16)), "воскресенье");
        Assert.That(ScheduleBuilder.MondayOf(new DateOnly(2026, 11, 16)), Is.EqualTo(new DateOnly(2026, 11, 16)), "понедельник");
        using var db = _base.Open();
        Assert.That(new ScheduleBuilder(db).Week(_a112.Id, Tomorrow).Count, Is.EqualTo(6));
    }

    [Test]
    public void Каталог_отбирает_аудитории()
    {
        using var db = _base.Open();
        var registry = new RoomRegistry(db, _clock);
        Assert.Multiple(() =>
        {
            Assert.That(registry.Find(new RoomQuery(Text: "а-1")).Select(r => r.Code), Is.EqualTo(new[] { "А-101", "А-112" }));
            Assert.That(registry.Find(new RoomQuery(MinSeats: 100)).Single().Code, Is.EqualTo("А-101"));
            Assert.That(registry.Find(new RoomQuery(Kind: RoomKind.ComputerLab)).Single().Code, Is.EqualTo("А-112"));
        });
    }
}

/// <summary>Регрессионные тесты для дефектов, найденных при проверке в браузере на этапе 4.</summary>
[TestFixture, Description("Исправленные дефекты")]
public class RegressionTests
{
    private MemoryDatabase _base = null!;
    private StoppedClock _clock = null!;

    [SetUp]
    public void Prepare()
    {
        _base = new MemoryDatabase();
        _clock = new StoppedClock(Moment.Now);
    }

    [TearDown]
    public void Release() => _base.Dispose();

    [Test, Description("Дефект 1: номер A-101 с латинской буквой регистрировался повторно")]
    public void Номер_с_латинскими_двойниками_считается_повтором()
    {
        _base.AddRoom("А-101");
        using var db = _base.Open();
        var ex = Assert.Throws<DomainException>(() =>
            new RoomRegistry(db, _clock).Register(new RoomInput("A-101", "Корпус А", RoomKind.Classroom, 40, false)));
        Assert.That(ex!.Message, Is.EqualTo("Аудитория А-101 уже зарегистрирована в каталоге"));
    }

    [Test, Description("Дефект 2: аудитория выводилась из эксплуатации при запланированных бронированиях")]
    public void Вывод_из_эксплуатации_при_запланированных_бронированиях_запрещён()
    {
        var room = _base.AddRoom("Б-210", 32);
        using var db = _base.Open();
        var desk = new ReservationDesk(db, _clock);
        var r = desk.Book(room.Id, Moment.Request(Moment.Tomorrow_("10:10", "11:40")));
        var registry = new RoomRegistry(db, _clock);

        var ex = Assert.Throws<DomainException>(() => registry.TakeOutOfService(room.Id, "Замена оконных блоков"));
        Assert.That(ex!.Message, Does.Contain("запланированных бронирований: 1 (ближайшее — 18.11.2026 10:10)"));

        desk.Cancel(r.Id, "Перенос занятия");
        Assert.DoesNotThrow(() => registry.TakeOutOfService(room.Id, "Замена оконных блоков"), "после отмены вывод разрешён");
    }

    [Test, Description("Дефект 2: завершённые бронирования выводу не мешают")]
    public void Завершённые_бронирования_не_мешают_выводу()
    {
        var room = _base.AddRoom("Б-210", 32);
        using var db = _base.Open();
        new ReservationDesk(db, _clock).Book(room.Id, Moment.Request(Moment.Tomorrow_("10:10", "11:40")));
        _clock.Now = Moment.Now.AddDays(2);
        Assert.DoesNotThrow(() => new RoomRegistry(db, _clock).TakeOutOfService(room.Id, "Замена оконных блоков"));
    }

    [Test, Description("Дефект 3: вместимость уменьшалась ниже числа участников бронирования")]
    public void Вместимость_не_уменьшается_ниже_участников_бронирования()
    {
        var room = _base.AddRoom("Б-210", 32);
        using var db = _base.Open();
        new ReservationDesk(db, _clock).Book(room.Id, Moment.Request(Moment.Tomorrow_("10:10", "11:40"), attendees: 24));
        var registry = new RoomRegistry(db, _clock);

        var ex = Assert.Throws<DomainException>(() =>
            registry.Update(room.Id, new RoomInput("Б-210", "Корпус Б", RoomKind.Classroom, 10, false)));
        Assert.That(ex!.Message, Is.EqualTo("Нельзя уменьшить число мест до 10: на 18.11.2026 10:10 оформлено бронирование на 24 участников"));
        Assert.DoesNotThrow(() => registry.Update(room.Id, new RoomInput("Б-210", "Корпус Б", RoomKind.Classroom, 24, false)));
    }
}

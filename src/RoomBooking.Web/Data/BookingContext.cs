using Microsoft.EntityFrameworkCore;
using RoomBooking.Core.Reservations;
using RoomBooking.Core.Rooms;

namespace RoomBooking.Web.Data;

/// <summary>Контекст базы данных. Сопоставление классов с таблицами задано через Fluent API.</summary>
public class BookingContext(DbContextOptions<BookingContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Room>(e =>
        {
            e.ToTable("Rooms");
            e.Property(r => r.Code).HasMaxLength(20).IsRequired();
            e.HasIndex(r => r.Code).IsUnique();
            e.Property(r => r.Building).HasMaxLength(50).IsRequired();
            e.Property(r => r.Kind).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.OutOfServiceReason).HasMaxLength(200);
        });

        model.Entity<Reservation>(e =>
        {
            e.ToTable("Reservations");
            e.Property(x => x.Organizer).HasMaxLength(100).IsRequired();
            e.Property(x => x.StudyGroup).HasMaxLength(30);
            e.Property(x => x.Purpose).HasMaxLength(200).IsRequired();
            e.Property(x => x.CancelReason).HasMaxLength(200);
            e.Ignore(x => x.IsCancelled);
            // Аудитория 1 — N бронирований; удаление аудитории с бронированиями запрещено
            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            // Индекс для поиска пересекающихся бронирований аудитории
            e.HasIndex(x => new { x.RoomId, x.Start, x.End });
        });
    }
}

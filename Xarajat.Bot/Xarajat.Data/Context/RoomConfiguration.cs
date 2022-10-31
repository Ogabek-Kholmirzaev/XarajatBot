using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Xarajat.Data.Entities;

namespace Xarajat.Data.Context
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.Property(r => r.Name)
                .HasColumnType("nvarchar(50)")
                .IsRequired();

            builder.Property(r=>r.Key)
                .HasColumnType("nvarchar(20)")
                .IsRequired();

            builder.Property(r => r.Status)
                .HasDefaultValue(RoomStatus.Created)
                .IsRequired();
        }
    }
}

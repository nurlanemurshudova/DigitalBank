using Entities.Concrete.TableModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");

            // Primary Key
            builder.HasKey(m => m.Id);

            // Properties
            builder.Property(m => m.Content)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(m => m.IsRead)
                .HasDefaultValue(false);

            builder.Property(m => m.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            // Indexes
            builder.HasIndex(m => m.SenderId);
            builder.HasIndex(m => m.ReceiverId);
            builder.HasIndex(m => m.IsRead);
            builder.HasIndex(m => m.CreatedDate);

            // Composite Index (Conversation: Sender + Receiver)
            builder.HasIndex(m => new { m.SenderId, m.ReceiverId });
            // Relationships
            builder.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // Vacibdir: SQL Server zəncirvari silməyə icazə vermir

            builder.HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
            // ============================================
            // Relationships (ApplicationUserConfiguration-da təyin olunub)
            // ============================================
        }
    }
}

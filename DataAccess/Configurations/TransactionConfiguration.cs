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
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");

            // Primary Key
            builder.HasKey(t => t.Id);

            // Properties
            builder.Property(t => t.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasConversion<int>(); // Enum-u int olaraq saxla

            builder.Property(t => t.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            // Indexes
            builder.HasIndex(t => t.SenderId);
            builder.HasIndex(t => t.ReceiverId);
            builder.HasIndex(t => t.CreatedDate);
            builder.HasIndex(t => t.Status);

            // Composite Index (Sender + Date)
            builder.HasIndex(t => new { t.SenderId, t.CreatedDate });
            // Relationships
            builder.HasOne(t => t.Sender)
                .WithMany(u => u.SentTransactions)
                .HasForeignKey(t => t.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Receiver)
                .WithMany(u => u.ReceivedTransactions)
                .HasForeignKey(t => t.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
            // ============================================
            // Relationships (ApplicationUserConfiguration-da təyin olunub)
            // ============================================
        }
    }
}

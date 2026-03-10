using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    internal class KycSubmissionConfig : IEntityTypeConfiguration<KycSubmission>
    {
        public void Configure(EntityTypeBuilder<KycSubmission> builder)
        {
            builder.ToTable("KycSubmissions");

            builder.HasKey(k => k.Id);

            builder.Property(k => k.UserId)
                .IsRequired();

            builder.Property(k => k.DocumentType)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(20)");

            builder.Property(k => k.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(k => k.Description)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(k => k.FrontImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(k => k.BackImageUrl)
                .HasMaxLength(500);

            builder.Property(k => k.SelfieUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(k => k.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("varchar(20)")
                .HasDefaultValue(SubmissionStatus.Pending);

            builder.Property(k => k.RejectionReason)
                .HasMaxLength(500);

            builder.Property(k => k.SubmittedAt)
                .IsRequired();

            builder.HasIndex(k => k.UserId);
            builder.HasIndex(k => k.Status);
        }
    }
}
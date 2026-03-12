using BG.Domain.Guarantees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BG.Infrastructure.Persistence.Configurations;

public sealed class GuaranteeRequestDocumentLinkConfiguration : IEntityTypeConfiguration<GuaranteeRequestDocumentLink>
{
    public void Configure(EntityTypeBuilder<GuaranteeRequestDocumentLink> builder)
    {
        builder.ToTable("guarantee_request_documents");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.LinkedByDisplayName)
            .HasMaxLength(256);

        builder.HasOne(link => link.GuaranteeRequest)
            .WithMany(request => request.RequestDocuments)
            .HasForeignKey(link => link.GuaranteeRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.GuaranteeDocument)
            .WithMany(document => document.RequestLinks)
            .HasForeignKey(link => link.GuaranteeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.LinkedByUser)
            .WithMany()
            .HasForeignKey(link => link.LinkedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => new { link.GuaranteeRequestId, link.GuaranteeDocumentId })
            .IsUnique();
    }
}

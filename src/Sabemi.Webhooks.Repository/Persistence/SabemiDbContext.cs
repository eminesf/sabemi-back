using Microsoft.EntityFrameworkCore;
using Sabemi.Webhooks.Model.Entities;

namespace Sabemi.Webhooks.Repository.Persistence;

public class SabemiDbContext(DbContextOptions<SabemiDbContext> options) : DbContext(options)
{
    public DbSet<EventoBrutoLog> EventosBrutos => Set<EventoBrutoLog>();

    public DbSet<StatusContrato> StatusContratos => Set<StatusContrato>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventoBrutoLog>(entity =>
        {
            entity.ToTable("log_eventos_brutos");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IdTransacao).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.IdTransacao).IsUnique();

            entity.Property(e => e.IdContrato).HasMaxLength(200);
            entity.Property(e => e.StatusRecebido).HasMaxLength(50);
            entity.Property(e => e.PayloadBruto).IsRequired();

            entity.Property(e => e.StatusProcessamento)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(e => e.IdContrato);
            entity.HasIndex(e => e.StatusProcessamento);
        });

        modelBuilder.Entity<StatusContrato>(entity =>
        {
            entity.ToTable("status_contrato");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IdContrato).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.IdContrato).IsUnique();
        });
    }
}

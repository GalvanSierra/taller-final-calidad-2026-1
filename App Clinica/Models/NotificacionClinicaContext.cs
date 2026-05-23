using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App_Clinica.Models;

public partial class NotificacionClinicaContext : DbContext
{
    public NotificacionClinicaContext(DbContextOptions<NotificacionClinicaContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (!optionsBuilder.IsConfigured)
    {
        optionsBuilder
            .UseSqlServer("...tu cadena de conexión...")
            .ConfigureWarnings(w => 
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
}


    public virtual DbSet<CitaMedica> CitaMedicas { get; set; }

    public virtual DbSet<Notifiacion> Notifiacions { get; set; }

    public virtual DbSet<TipoUsuario> TipoUsuarios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CitaMedica>(entity =>
        {
            entity.HasKey(e => e.IdCita).HasName("PK__CitaMedi__394B0202EBCE60AE");

            entity.ToTable("CitaMedica");

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.FechaCita).HasColumnType("datetime");

            entity.HasOne(d => d.IdAfiliadoNavigation).WithMany(p => p.CitaMedicaIdAfiliadoNavigations)
                .HasForeignKey(d => d.IdAfiliado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CitaMedic__IdAfi__412EB0B6");

            entity.HasOne(d => d.IdPersonalMedicoNavigation).WithMany(p => p.CitaMedicaIdPersonalMedicoNavigations)
                .HasForeignKey(d => d.IdPersonalMedico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CitaMedic__IdPer__4222D4EF");
        });

        modelBuilder.Entity<Notifiacion>(entity =>
        {
            entity.HasKey(e => e.IdRecordatorio).HasName("PK__Notifiac__2F6E84A6C9AE315E");

            entity.ToTable("Notifiacion");

            entity.Property(e => e.FechaEnvio)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Mensaje).HasMaxLength(500);

            entity.HasOne(d => d.IdCitaNavigation).WithMany(p => p.Notifiacions)
                .HasForeignKey(d => d.IdCita)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notifiaci__IdCit__45F365D3");
        });

        modelBuilder.Entity<TipoUsuario>(entity =>
        {
            entity.HasKey(e => e.IdTipoUsuario).HasName("PK__TipoUsua__CA04062BD5E764D4");

            entity.ToTable("TipoUsuario");

            entity.Property(e => e.NombreTipo).HasMaxLength(20);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__Usuario__5B65BF97C5DD2D1A");

            entity.ToTable("Usuario");

            entity.HasIndex(e => e.Email, "UQ__Usuario__A9D1053428E369F9").IsUnique();

            entity.HasIndex(e => e.NumeroIdentificacion, "UQ__Usuario__FCA68D91F3976B6A").IsUnique();

            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Disponibilidad).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Especialidad).HasMaxLength(100);
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.NumeroIdentificacion).HasMaxLength(20);
            entity.Property(e => e.Telefono).HasMaxLength(15);

            entity.HasOne(d => d.IdTipoUsuarioNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdTipoUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Usuario__IdTipoU__3C69FB99");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

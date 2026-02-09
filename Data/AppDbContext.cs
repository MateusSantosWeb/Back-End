using Microsoft.EntityFrameworkCore;
using WishListAPI.Models;

namespace WishListAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Wish> Wishes { get; set; }
        public DbSet<WishProgress> WishProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da entidade User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.SenhaHash).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasDefaultValue("Namorada");
            });

            // Configuração da entidade Wish
            modelBuilder.Entity<Wish>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titulo).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descricao).HasMaxLength(500);
                entity.Property(e => e.Categoria).IsRequired();
                entity.Property(e => e.Prioridade).IsRequired();
                entity.Property(e => e.Status).HasDefaultValue("Ativo");
                entity.Property(e => e.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                // Relacionamento com User
                entity.HasOne(w => w.Usuario)
                    .WithMany(u => u.Wishes)
                    .HasForeignKey(w => w.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relacionamento com WishProgress (1:1)
                entity.HasOne(w => w.Progress)
                    .WithOne(p => p.Wish)
                    .HasForeignKey<WishProgress>(p => p.WishId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuração da entidade WishProgress
            modelBuilder.Entity<WishProgress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StatusSecreto).HasDefaultValue("Nao_Iniciado");

                // Relacionamento com User (Namorado)
                entity.HasOne(p => p.Namorado)
                    .WithMany(u => u.Progressos)
                    .HasForeignKey(p => p.NamoradoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
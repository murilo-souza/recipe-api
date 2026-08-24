using Microsoft.EntityFrameworkCore;
using RecipeApp.Domain.Common;
using RecipeApp.Domain.Entities;

namespace RecipeApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<PrepareStep> PrepareSteps => Set<PrepareStep>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<GeneralChatMessage> GeneralChatMessages => Set<GeneralChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user");
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(120).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.ProfileImage).HasMaxLength(500);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.ToTable("external_login");
            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
            entity.Property(e => e.Provider).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ProviderUserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PictureUrl).HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ExternalLogins)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("category");
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("recipe");
            entity.Property(r => r.Title).HasMaxLength(150).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.Property(r => r.Image).HasMaxLength(255);

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Recipes)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Category)
                  .WithMany(c => c.Recipes)
                  .HasForeignKey(r => r.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(r => r.UserId);
            entity.Property(r => r.Embedding).HasColumnType("vector(768)");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("ingredient");
            entity.Property(i => i.Description).HasMaxLength(255).IsRequired();

            entity.HasOne(i => i.Recipe)
                  .WithMany(r => r.Ingredients)
                  .HasForeignKey(i => i.RecipeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(i => i.RecipeId);
        });

        modelBuilder.Entity<PrepareStep>(entity =>
        {
            entity.ToTable("prepare_step");
            entity.Property(p => p.Description).HasMaxLength(500).IsRequired();

            entity.HasOne(p => p.Recipe)
                  .WithMany(r => r.PrepareSteps)
                  .HasForeignKey(p => p.RecipeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.RecipeId);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_message");
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.Role).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(c => c.Recipe)
                  .WithMany(r => r.ChatMessages)
                  .HasForeignKey(c => c.RecipeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.RecipeId);
        });

        modelBuilder.Entity<GeneralChatMessage>(entity =>
        {
            entity.ToTable("general_chat_message");
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.Role).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(c => c.User)
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_token");
            entity.Property(r => r.TokenHash).HasMaxLength(255).IsRequired();
            entity.HasIndex(r => r.TokenHash).IsUnique();

            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetCode>(entity =>
        {
            entity.ToTable("password_reset_code");
            entity.Property(p => p.CodeHash).HasMaxLength(255).IsRequired();

            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
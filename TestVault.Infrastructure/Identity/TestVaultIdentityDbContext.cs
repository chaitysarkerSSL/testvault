using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TestVault.Infrastructure.Identity;

/// <summary>
/// EF Core DbContext for authentication only - AspNetUsers/AspNetRoles/etc.
/// (from IdentityDbContext) plus RefreshTokens. Deliberately scoped to
/// exactly these tables: it has no DbSet for test_runs/test_cases/ai_analysis,
/// so EF Core's model/migrations can never touch them - those three tables
/// stay exactly as Phase 2 built them, owned entirely by Dapper
/// (Infrastructure/Repositories) via the separate IDbConnectionFactory.
///
/// Uses the same "DefaultConnection" connection string as the Dapper side
/// (see DependencyInjection.cs) - one physical database, two independent
/// data-access technologies each owning a disjoint set of tables. This is a
/// deliberate, scoped exception to "no EF Core" for this project: Identity's
/// standard implementation is built on it, and reimplementing user/role/claim
/// storage by hand in Dapper would just be re-deriving EF Core's own schema
/// with none of its tooling.
/// </summary>
public class TestVaultIdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public TestVaultIdentityDbContext(DbContextOptions<TestVaultIdentityDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(256);
            entity.Property(rt => rt.UserId).IsRequired();
            entity.Property(rt => rt.CreatedByIp).HasMaxLength(64);
            entity.Property(rt => rt.RevokedByIp).HasMaxLength(64);
            entity.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(256);

            // Looked up by hash on every refresh call, and by user when
            // revoking "every other active token" on a reuse-detected breach.
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => rt.UserId);

            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

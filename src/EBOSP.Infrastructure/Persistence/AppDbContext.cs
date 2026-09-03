using System.Reflection;
using EBOSP.Application.Common;
using EBOSP.Domain.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Outbox;
using EBOSP.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context for the modular monolith (ADR-0001). Module-specific entity sets
/// are added here as each module is implemented, starting with Identity in Phase 2. Also serves
/// as the <see cref="IUnitOfWork"/> implementation - SaveChangesAsync already gives the
/// single-transaction-per-request semantics the abstraction needs (dev guide §9).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserContext currentUserContext)
    : DbContext(options), IUnitOfWork
{
    private static readonly MethodInfo SetTenantFilterMethodInfo = typeof(AppDbContext)
        .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<StockBalance> StockBalances => Set<StockBalance>();

    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();

    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();

    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();

    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();

    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    public DbSet<ReorderRule> ReorderRules => Set<ReorderRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AggregateType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AggregateId).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("branches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasData(IdentitySeedData.Permissions);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasData(IdentitySeedData.Roles);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasData(IdentitySeedData.RolePermissions);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.UserId });
            entity.HasIndex(e => new { e.UserId, e.RoleId, e.BranchId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasOne<Branch>().WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("product_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(18,2)");
            entity.Property(e => e.TaxRatePercent).HasColumnType("numeric(5,2)");
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique();
            entity.HasOne<ProductCategory>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("stock_balances");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId, e.ProductId }).IsUnique();
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
            // Postgres system column, not a mapped CLR property - dev guide §13.4: "concurrent
            // issue operations cannot corrupt balance".
            entity.Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();
        });

        modelBuilder.Entity<StockLedgerEntry>(entity =>
        {
            entity.ToTable("stock_ledger_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.ReferenceType).HasMaxLength(100);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100);
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId, e.ProductId });
            entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("goods_receipts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reference).HasMaxLength(200);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.ToTable("goods_receipt_lines");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.ToTable("stock_adjustments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.ToTable("stock_transfers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(e => e.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(e => e.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("stock_reservations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReorderRule>(entity =>
        {
            entity.ToTable("reorder_rules");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId, e.ProductId }).IsUnique();
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // Tenant isolation is enforced here, not left to each query author (spec §12).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                SetTenantFilterMethodInfo.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
            }
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == currentUserContext.TenantId);
    }

    /// <summary>Translates EF Core's own exception so the Application layer never depends on it directly (dev guide §13.4: "concurrent issue operations cannot corrupt balance").</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
    }
}

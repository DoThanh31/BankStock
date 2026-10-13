using BankStock.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<FinancialReport> FinancialReports => Set<FinancialReport>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b => { b.ToTable("Users"); });
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

        builder.Entity<Bank>(b => { b.HasIndex(x => x.Code).IsUnique(); });

        builder.Entity<Stock>(b =>
        {
            b.HasIndex(x => x.StockCode).IsUnique();
            b.HasOne(x => x.Bank).WithMany(x => x.Stocks).HasForeignKey(x => x.BankId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PriceHistory>(b => { b.HasIndex(x => new { x.StockId, x.TradeDate }).IsUnique(); });
        builder.Entity<FinancialReport>(b => { b.HasIndex(x => new { x.StockId, x.Year, x.Quarter }).IsUnique(); });

        builder.Entity<Watchlist>(b =>
        {
            b.HasIndex(x => new { x.UserId, x.StockId }).IsUnique();
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Wallet>(b =>
        {
            b.HasIndex(x => x.UserId).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("CK_Wallet_Balance", "[Balance] >= 0"));
            b.ToTable(t => t.HasCheckConstraint("CK_Wallet_BlockedBalance", "[BlockedBalance] >= 0"));
        });
        
        builder.Entity<WalletTransaction>(b =>
        {
            b.HasOne(x => x.Wallet).WithMany(x => x.Transactions).HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Order>(b =>
        {
            b.ToTable(t => t.HasCheckConstraint("CK_Order_Quantity", "[Quantity] >= 0"));
            b.ToTable(t => t.HasCheckConstraint("CK_Order_Price", "[Price] >= 0"));
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
        });
        
        builder.Entity<Trade>(b =>
        {
            b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Holding>(b =>
        {
            b.HasIndex(x => new { x.UserId, x.StockId }).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("CK_Holding_Quantity", "[Quantity] >= 0"));
            b.ToTable(t => t.HasCheckConstraint("CK_Holding_BlockedQuantity", "[BlockedQuantity] >= 0"));
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Stock).WithMany().HasForeignKey(x => x.StockId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

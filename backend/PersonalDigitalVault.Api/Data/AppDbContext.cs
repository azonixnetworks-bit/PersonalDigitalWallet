using Microsoft.EntityFrameworkCore;
using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    // =========================================================
    // TABLES
    // =========================================================

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<Folder> Folders =>
        Set<Folder>();

    public DbSet<Document> Documents =>
        Set<Document>();

    public DbSet<Credential> Credentials =>
        Set<Credential>();

    public DbSet<DocumentShare> DocumentShares =>
        Set<DocumentShare>();

    public DbSet<Subscription> Subscriptions =>
        Set<Subscription>();

    public DbSet<GoogleDriveBackupConnection> GoogleDriveBackupConnections =>
        Set<GoogleDriveBackupConnection>();


    // =========================================================
    // MODEL CONFIGURATION
    // =========================================================

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // =====================================================
        // USER
        // =====================================================

        // Email unique-aa irukkanum.
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();


        // Stripe billing customer reference is unique when present.
        modelBuilder.Entity<User>()
            .Property(x => x.StripeCustomerId)
            .HasMaxLength(100);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.StripeCustomerId)
            .IsUnique()
            .HasFilter("[StripeCustomerId] IS NOT NULL");


        // =====================================================
        // PASSWORD RESET TOKEN HASH
        // =====================================================
        //
        // Function:
        // Forgot Password reset token hash
        // fast-aa search panna index.
        //
        // Raw token inga varathu.
        // SHA-256 HEX hash mattum save aagum.
        //
        // SHA-256 HEX = 64 characters.
        //
        modelBuilder.Entity<User>()
            .Property(
                x => x.PasswordResetTokenHash)
            .HasMaxLength(64);


        modelBuilder.Entity<User>()
            .HasIndex(
                x => x.PasswordResetTokenHash);


        // =====================================================
        // USER -> FOLDERS
        // =====================================================
        //
        // User hard delete pannumbodhu
        // automatic cascade delete vendaam.
        //
        modelBuilder.Entity<Folder>()
            .HasOne(
                x => x.User)
            .WithMany(
                x => x.Folders)
            .HasForeignKey(
                x => x.UserId)
            .OnDelete(
                DeleteBehavior.NoAction);


        // =====================================================
        // USER -> DOCUMENTS
        // =====================================================

        modelBuilder.Entity<Document>()
            .HasOne(
                x => x.User)
            .WithMany(
                x => x.Documents)
            .HasForeignKey(
                x => x.UserId)
            .OnDelete(
                DeleteBehavior.NoAction);


        // =====================================================
        // FOLDER -> DOCUMENTS
        // =====================================================
        //
        // Folder delete pannina document
        // delete aaga koodathu.
        //
        // FolderId NULL aagum.
        //
        modelBuilder.Entity<Document>()
            .HasOne(
                x => x.Folder)
            .WithMany(
                x => x.Documents)
            .HasForeignKey(
                x => x.FolderId)
            .OnDelete(
                DeleteBehavior.SetNull);


        // =====================================================
        // USER -> CREDENTIALS
        // =====================================================

        modelBuilder.Entity<Credential>()
            .HasOne(
                x => x.User)
            .WithMany(
                x => x.Credentials)
            .HasForeignKey(
                x => x.UserId)
            .OnDelete(
                DeleteBehavior.NoAction);


        // =====================================================
        // DOCUMENT -> DOCUMENT SHARES
        // =====================================================
        //
        // Original document delete aana
        // corresponding share permission records
        // delete aagalam.
        //
        modelBuilder.Entity<DocumentShare>()
            .HasOne(
                x => x.Document)
            .WithMany(
                x => x.Shares)
            .HasForeignKey(
                x => x.DocumentId)
            .OnDelete(
                DeleteBehavior.Cascade);


        // =====================================================
        // OWNER USER -> SENT DOCUMENT SHARES
        // =====================================================

        modelBuilder.Entity<DocumentShare>()
            .HasOne(
                x => x.OwnerUser)
            .WithMany(
                x => x.SentDocumentShares)
            .HasForeignKey(
                x => x.OwnerUserId)
            .OnDelete(
                DeleteBehavior.NoAction);


        // =====================================================
        // RECIPIENT USER -> RECEIVED DOCUMENT SHARES
        // =====================================================

        modelBuilder.Entity<DocumentShare>()
            .HasOne(
                x => x.RecipientUser)
            .WithMany(
                x => x.ReceivedDocumentShares)
            .HasForeignKey(
                x => x.RecipientUserId)
            .OnDelete(
                DeleteBehavior.NoAction);


        // =====================================================
        // DOCUMENT SHARE TOKEN HASH INDEX
        // =====================================================

        modelBuilder.Entity<DocumentShare>()
            .HasIndex(
                x => x.InvitationTokenHash);


        // =====================================================
        // SAME DOCUMENT + SAME RECIPIENT UNIQUE
        // =====================================================

        modelBuilder.Entity<DocumentShare>()
            .HasIndex(
                x => new
                {
                    x.DocumentId,
                    x.RecipientUserId
                })
            .IsUnique();


        // =====================================================
        // DOCUMENT SHARE PROPERTY LENGTHS
        // =====================================================

        modelBuilder.Entity<DocumentShare>()
            .Property(
                x => x.Status)
            .HasMaxLength(20);


        modelBuilder.Entity<DocumentShare>()
            .Property(
                x => x.InvitationTokenHash)
            .HasMaxLength(128);


        // =====================================================
        // USER -> SUBSCRIPTIONS
        // =====================================================
        //
        // One user-ku multiple historical
        // subscription records irukkalaam.
        //
        // User hard delete-ku automatic
        // cascade delete vendaam.
        //
        modelBuilder.Entity<Subscription>()
            .HasOne(
                x => x.User)
            .WithMany()
            .HasForeignKey(
                x => x.UserId)
            .OnDelete(
                DeleteBehavior.NoAction);


        // =====================================================
        // PAYPAL SUBSCRIPTION ID UNIQUE
        // =====================================================
        //
        // Same PayPal subscription
        // two local records-la duplicate aaga koodathu.
        //
        modelBuilder.Entity<Subscription>()
            .HasIndex(
                x => x.PayPalSubscriptionId)
            .IsUnique()
            .HasFilter("[PayPalSubscriptionId] IS NOT NULL");


        // =====================================================
        // USER + CREATED DATE INDEX
        // =====================================================
        //
        // Latest user subscription
        // fast-aa retrieve panna.
        //
        modelBuilder.Entity<Subscription>()
            .HasIndex(
                x => new
                {
                    x.UserId,
                    x.CreatedAt
                });


        // =====================================================
        // PAYPAL PLAN ID INDEX
        // =====================================================

        modelBuilder.Entity<Subscription>()
            .HasIndex(
                x => x.PayPalPlanId);


        // Stripe subscription ID is globally unique when present.
        modelBuilder.Entity<Subscription>()
            .HasIndex(x => x.StripeSubscriptionId)
            .IsUnique()
            .HasFilter("[StripeSubscriptionId] IS NOT NULL");

        modelBuilder.Entity<Subscription>()
            .HasIndex(x => x.StripePriceId);


        // =====================================================
        // SUBSCRIPTION PROPERTY LENGTHS
        // =====================================================

        modelBuilder.Entity<Subscription>()
            .Property(
                x => x.Provider)
            .HasMaxLength(30);


        modelBuilder.Entity<Subscription>()
            .Property(
                x => x.PayPalSubscriptionId)
            .HasMaxLength(100);


        modelBuilder.Entity<Subscription>()
            .Property(
                x => x.PayPalPlanId)
            .HasMaxLength(100);


        modelBuilder.Entity<Subscription>()
            .Property(x => x.StripeSubscriptionId)
            .HasMaxLength(100);

        modelBuilder.Entity<Subscription>()
            .Property(x => x.StripePriceId)
            .HasMaxLength(100);


        modelBuilder.Entity<Subscription>()
            .Property(
                x => x.PlanName)
            .HasMaxLength(100);


        modelBuilder.Entity<Subscription>()
            .Property(
                x => x.Status)
            .HasMaxLength(40);


        // =====================================================
        // GOOGLE DRIVE BACKUP CONNECTION
        // =====================================================
        // One Google Drive backup connection per PDV user.
        modelBuilder.Entity<GoogleDriveBackupConnection>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<GoogleDriveBackupConnection>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<GoogleDriveBackupConnection>()
            .Property(x => x.RefreshTokenEncrypted)
            .HasMaxLength(4096);

        modelBuilder.Entity<GoogleDriveBackupConnection>()
            .Property(x => x.AccountEmailEncrypted)
            .HasMaxLength(2048);

        modelBuilder.Entity<GoogleDriveBackupConnection>()
            .Property(x => x.AutoBackupFrequency)
            .HasMaxLength(20);
    }
}

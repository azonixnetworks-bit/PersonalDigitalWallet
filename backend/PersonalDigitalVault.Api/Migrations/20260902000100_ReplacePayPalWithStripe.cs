using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PersonalDigitalVault.Api.Data;

#nullable disable

namespace PersonalDigitalVault.Api.Migrations;

// Compatibility placeholder.
//
// A previous Stripe-only draft used this migration id to remove PayPal columns.
// The final dual-provider design keeps PayPal and Stripe in parallel, so this
// migration intentionally performs no schema changes.
//
// If this migration was never applied, EF records this no-op and then applies
// 20260902000200_AddStripeSubscriptionSupport.
//
// If the old destructive version was already applied to a database before this
// corrected package, restore that database from backup before continuing.
[DbContext(typeof(AppDbContext))]
[Migration("20260902000100_ReplacePayPalWithStripe")]
public partial class ReplacePayPalWithStripe : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty.
    }
}

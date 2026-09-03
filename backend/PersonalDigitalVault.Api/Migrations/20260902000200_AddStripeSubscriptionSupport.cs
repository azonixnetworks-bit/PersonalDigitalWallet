using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PersonalDigitalVault.Api.Data;

#nullable disable

namespace PersonalDigitalVault.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260902000200_AddStripeSubscriptionSupport")]
public partial class AddStripeSubscriptionSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // PayPal stays fully supported. Its provider-specific identifiers become
        // nullable so Stripe rows do not need fake PayPal values.
        migrationBuilder.DropIndex(
            name: "IX_Subscriptions_PayPalSubscriptionId",
            table: "Subscriptions");

        migrationBuilder.AlterColumn<string>(
            name: "PayPalSubscriptionId",
            table: "Subscriptions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<string>(
            name: "PayPalPlanId",
            table: "Subscriptions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100);

        migrationBuilder.AddColumn<bool>(
            name: "CancelAtPeriodEnd",
            table: "Subscriptions",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "StripePriceId",
            table: "Subscriptions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StripeSubscriptionId",
            table: "Subscriptions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StripeCustomerId",
            table: "Users",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_PayPalSubscriptionId",
            table: "Subscriptions",
            column: "PayPalSubscriptionId",
            unique: true,
            filter: "[PayPalSubscriptionId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_StripePriceId",
            table: "Subscriptions",
            column: "StripePriceId");

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_StripeSubscriptionId",
            table: "Subscriptions",
            column: "StripeSubscriptionId",
            unique: true,
            filter: "[StripeSubscriptionId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Users_StripeCustomerId",
            table: "Users",
            column: "StripeCustomerId",
            unique: true,
            filter: "[StripeCustomerId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Subscriptions_StripePriceId",
            table: "Subscriptions");

        migrationBuilder.DropIndex(
            name: "IX_Subscriptions_StripeSubscriptionId",
            table: "Subscriptions");

        migrationBuilder.DropIndex(
            name: "IX_Users_StripeCustomerId",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Subscriptions_PayPalSubscriptionId",
            table: "Subscriptions");

        // Old schema cannot represent Stripe-only rows because PayPal IDs were
        // required. Rollback intentionally removes Stripe provider rows only.
        migrationBuilder.Sql(
            "DELETE FROM [Subscriptions] WHERE [Provider] = 'Stripe';");

        migrationBuilder.DropColumn(
            name: "CancelAtPeriodEnd",
            table: "Subscriptions");

        migrationBuilder.DropColumn(
            name: "StripePriceId",
            table: "Subscriptions");

        migrationBuilder.DropColumn(
            name: "StripeSubscriptionId",
            table: "Subscriptions");

        migrationBuilder.DropColumn(
            name: "StripeCustomerId",
            table: "Users");

        migrationBuilder.AlterColumn<string>(
            name: "PayPalSubscriptionId",
            table: "Subscriptions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "PayPalPlanId",
            table: "Subscriptions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_PayPalSubscriptionId",
            table: "Subscriptions",
            column: "PayPalSubscriptionId",
            unique: true);
    }
}

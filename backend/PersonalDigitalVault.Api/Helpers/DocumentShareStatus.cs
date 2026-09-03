namespace PersonalDigitalVault.Api.Helpers;

public static class DocumentShareStatus
{
    // =====================================================
    // DOCUMENT SHARE STATUS VALUES
    // =====================================================
    //
    // Pending:
    // Owner share pannitaar.
    // Recipient innum accept pannala.
    //
    public const string Pending =
        "Pending";


    // Accepted:
    // Recipient login + authentication mudichu
    // share invitation accept pannitaar.
    //
    public const string Accepted =
        "Accepted";


    // Revoked:
    // Owner share access-a remove pannitaar.
    //
    public const string Revoked =
        "Revoked";
}
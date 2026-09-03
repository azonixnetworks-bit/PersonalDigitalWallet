using PersonalDigitalVault.Api.DTOs.Share;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IDocumentShareService
{
    // =====================================================
    // SHARE DOCUMENT
    // =====================================================
    //
    // Owner specific registered user-ku
    // document share pannum.
    //
    Task<DocumentShareDto>
        ShareDocumentAsync(
            int documentId,
            ShareDocumentRequestDto request);


    // =====================================================
    // ACCEPT EMAIL INVITATION
    // =====================================================
    //
    // Recipient login pannitu
    // invitation accept pannum.
    //
    Task<bool>
        AcceptShareAsync(
            AcceptShareRequestDto request);


    // =====================================================
    // SHARED WITH ME
    // =====================================================

    Task<List<SharedDocumentDto>>
        GetSharedWithMeAsync();


    // =====================================================
    // OWNER SHARE LIST
    // =====================================================
    //
    // Specific document yaara yaara user-ku
    // share pannirukkom.
    //
    Task<List<DocumentShareDto>>
        GetDocumentSharesAsync(
            int documentId);


    // =====================================================
    // REVOKE SHARE
    // =====================================================

    Task<bool>
        RevokeShareAsync(
            int documentId,
            int shareId);
}
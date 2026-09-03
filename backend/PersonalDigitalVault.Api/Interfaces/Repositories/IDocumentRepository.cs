using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Interfaces.Repositories;

public interface IDocumentRepository
{
    // =====================================================
    // CURRENT USER DOCUMENTS
    // =====================================================
    //
    // Logged-in user own documents mattum return pannum.
    //
    Task<List<Document>> GetByUserAsync(
        int userId);


    // =====================================================
    // OWNED DOCUMENT
    // =====================================================
    //
    // INPUT:
    //
    // id
    // userId
    //
    // OUTPUT:
    //
    // Document owner match aana document.
    //
    // Upload update delete etc-ku use pannuvom.
    //
    Task<Document?> GetOwnedAsync(
        int id,
        int userId);


    // =====================================================
    // GET DOCUMENT BY ID
    // =====================================================
    //
    // NEW METHOD
    //
    // IMPORTANT:
    //
    // Indha method ownership check pannaadhu.
    //
    // Shared file download-ku
    // document first load pannitu
    // Service layer-la:
    //
    // Owner?
    // OR
    // Accepted Share?
    //
    // check pannuvom.
    //
    Task<Document?> GetByIdAsync(int id);


    // =====================================================
    // ADD DOCUMENT
    // =====================================================

    Task AddAsync(
        Document document);


    // =====================================================
    // UPDATE DOCUMENT
    // =====================================================

    Task UpdateAsync(
        Document document);


    // =====================================================
    // DELETE DOCUMENT
    // =====================================================

    Task DeleteAsync(
        Document document);


    // =====================================================
    // ADMIN TOTAL DOCUMENT COUNT
    // =====================================================

    Task<int> CountAsync();


    // =====================================================
    // ADMIN UPLOAD METADATA
    // =====================================================

    Task<List<Document>>
        GetAllForAdminAsync();
}
using Microsoft.EntityFrameworkCore;
using PersonalDigitalVault.Api.Data;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;

namespace PersonalDigitalVault.Api.Repositories;

public class DocumentRepository : IDocumentRepository
{
    // =========================================================
    // DATABASE
    // =========================================================

    private readonly AppDbContext _db;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // AppDbContext repository-kulla inject pannum.
    //
    // Input:
    // AppDbContext db
    //
    // Reason:
    // Documents table read/write panna.
    //
    // Output:
    // _db variable-la context save aagum.
    public DocumentRepository(
        AppDbContext db)
    {
        _db = db;
    }


    // =========================================================
    // USER DOCUMENT LIST
    // =========================================================

    // Function:
    // Logged-in user-ku belong aagura documents mattum
    // return pannum.
    //
    // Input:
    // userId = current logged-in user id.
    //
    // Reason:
    // Cross-user document metadata leakage prevent panna.
    //
    // Output:
    // Current user own documents list.
    //
    // Security:
    // WHERE UserId == current user.
    public Task<List<Document>>
        GetByUserAsync(
            int userId)
    {
        return _db.Documents
            .AsNoTracking()

            // Dashboard can safely show the owned folder name as metadata.
            .Include(
                document =>
                    document.Folder)

            .Where(
                document =>
                    document.UserId == userId)

            .OrderByDescending(
                document =>
                    document.CreatedAt)

            .ToListAsync();
    }


    // =========================================================
    // OWNED DOCUMENT
    // =========================================================

    // Function:
    // Specific document current user-ku belong aagutha
    // check panni load pannum.
    //
    // Input:
    // id     = document id
    // userId = current logged-in user id
    //
    // Reason:
    // Details / Rename / Delete owner-only panna.
    //
    // Output:
    // Match aana Document.
    // Illaina null.
    //
    // Security:
    // DocumentId mattum check pannaama UserId-um check pannum.
    //
    // IMPORTANT:
    // Inga AsNoTracking use panna maatom.
    //
    // Reason:
    // Indha document later Update/Delete operations-ku
    // tracked entity-a use aagalaam.
    public Task<Document?>
        GetOwnedAsync(
            int id,
            int userId)
    {
        return _db.Documents
            .FirstOrDefaultAsync(
                document =>
                    document.Id == id &&
                    document.UserId == userId);
    }


    // =========================================================
    // GET DOCUMENT BY ID
    // =========================================================

    // Function:
    // Document Id base panni metadata load pannum.
    //
    // Input:
    // id = document id.
    //
    // Reason:
    // Shared recipient document owner illa.
    //
    // Secure download flow:
    //
    // Document
    //      ↓
    // Owner?
    //      ↓
    // No
    //      ↓
    // Accepted Share?
    //
    // nu DocumentService decide panna first document metadata
    // retrieve panna vendum.
    //
    // Output:
    // Document / null.
    //
    // SECURITY WARNING:
    //
    // Indha method authorization pannaathu.
    //
    // Controller direct-a indha method use panna koodathu.
    //
    // Permission check DocumentService-la:
    //
    // Owner
    // OR
    // Accepted secure recipient
    //
    // nu check aaganum.
    public Task<Document?>
        GetByIdAsync(
            int id)
    {
        return _db.Documents
            .AsNoTracking()

            .FirstOrDefaultAsync(
                document =>
                    document.Id == id);
    }


    // =========================================================
    // ADD DOCUMENT
    // =========================================================

    // Function:
    // New document metadata database-la save pannum.
    //
    // Input:
    // Document entity.
    //
    // Reason:
    // Encrypted physical file-related metadata SQL-la save panna.
    //
    // Output:
    // Database insert complete.
    //
    // Security:
    // Actual file bytes SQL-la save panna maatom.
    // Metadata mattum save aagum.
    public async Task AddAsync(
        Document document)
    {
        _db.Documents.Add(
            document);

        await _db.SaveChangesAsync();
    }


    // =========================================================
    // UPDATE DOCUMENT
    // =========================================================

    // Function:
    // Existing document metadata update pannum.
    //
    // Input:
    // Document entity.
    //
    // Reason:
    // Example:
    // Owner filename rename.
    //
    // Output:
    // Database update complete.
    //
    // Security:
    // Ownership validation repository UpdateAsync-la illa.
    //
    // Calling service GetOwnedAsync use panni
    // ownership verify pannirukkanum.
    public async Task UpdateAsync(
        Document document)
    {
        _db.Documents.Update(
            document);

        await _db.SaveChangesAsync();
    }


    // =========================================================
    // DELETE DOCUMENT
    // =========================================================

    // Function:
    // Document metadata database-la delete pannum.
    //
    // Input:
    // Document entity.
    //
    // Reason:
    // Owner requested document deletion.
    //
    // Output:
    // Database delete complete.
    //
    // Security:
    // Ownership DocumentService-la first verify aaganum.
    //
    // Related DocumentShare rows database relationship rule
    // base panni handle aagum.
    public async Task DeleteAsync(
        Document document)
    {
        _db.Documents.Remove(
            document);

        await _db.SaveChangesAsync();
    }


    // =========================================================
    // TOTAL DOCUMENT COUNT
    // =========================================================

    // Function:
    // Total document metadata record count return pannum.
    //
    // Input:
    // None.
    //
    // Reason:
    // Admin dashboard statistics.
    //
    // Output:
    // Total document count.
    //
    // Security:
    // Document contents/decryption inga nadakkaathu.
    public Task<int> CountAsync()
    {
        return _db.Documents
            .CountAsync();
    }


    // =========================================================
    // ADMIN UPLOAD ACTIVITY
    // =========================================================

    // Function:
    // Admin dashboard-ku document upload metadata return pannum.
    //
    // Input:
    // None.
    //
    // Output:
    // Documents + uploader User relation.
    //
    // Example metadata:
    // User name
    // Original filename
    // File size
    // Upload date
    //
    // Security:
    // Admin-ku:
    //
    // Storage file read panna maatom.
    // AES decrypt panna maatom.
    // Document download access kudukka maatom.
    // Credential information inga illa.
    public Task<List<Document>>
        GetAllForAdminAsync()
    {
        return _db.Documents
            .AsNoTracking()

            .Include(
                document =>
                    document.User)

            .OrderByDescending(
                document =>
                    document.CreatedAt)

            .ToListAsync();
    }
}
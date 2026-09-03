using PersonalDigitalVault.Api.DTOs.Document;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Helpers;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;
using PersonalDigitalVault.Api.Storage;

namespace PersonalDigitalVault.Api.Services;

public class DocumentService
    : IDocumentService
{
    // =====================================================
    // DEPENDENCIES
    // =====================================================

    private readonly
        IDocumentRepository
        _documentRepository;


    private readonly
        IFolderRepository
        _folderRepository;


    private readonly
        IDocumentShareRepository
        _shareRepository;


    private readonly
        CurrentUserService
        _currentUser;


    private readonly
        AesEncryption
        _aes;


    private readonly
        FileHashService
        _hash;


    private readonly
        FileStorageService
        _storage;


    // =====================================================
    // R7.9 — SUBSCRIPTION ENTITLEMENTS
    // =====================================================
    //
    // Function:
    //
    // Free / Premium plan identify pannum.
    //
    // Upload-ku:
    //
    // Document count
    // Storage quota
    //
    // backend-la enforce pannum.
    //
    // IMPORTANT:
    // Frontend hide/show security illa.
    // Actual rule inga service layer-la enforce aagum.
    private readonly
        ISubscriptionEntitlementService
        _subscriptionEntitlementService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public DocumentService(
        IDocumentRepository documentRepository,
        IFolderRepository folderRepository,
        IDocumentShareRepository shareRepository,
        CurrentUserService currentUser,
        AesEncryption aes,
        FileHashService hash,
        FileStorageService storage,

        // R7.9
        ISubscriptionEntitlementService
            subscriptionEntitlementService)
    {
        _documentRepository =
            documentRepository;

        _folderRepository =
            folderRepository;

        _shareRepository =
            shareRepository;

        _currentUser =
            currentUser;

        _aes =
            aes;

        _hash =
            hash;

        _storage =
            storage;

        _subscriptionEntitlementService =
            subscriptionEntitlementService;
    }


    // =====================================================
    // DOCUMENT -> DTO
    // =====================================================

    private static DocumentDto Map(
        Document document)
    {
        return new DocumentDto
        {
            Id =
                document.Id,

            FileName =
                document.OriginalFileName,

            ContentType =
                document.ContentType,

            FileSize =
                document.FileSize,

            FileHash =
                document.FileHash,

            FolderId =
                document.FolderId,

            CreatedAt =
                document.CreatedAt
        };
    }


    // =====================================================
    // UPLOAD DOCUMENT
    // =====================================================

    public async Task<DocumentDto>
        UploadAsync(
            UploadDocumentDto dto)
    {
        // =================================================
        // FILE VALIDATION
        // =================================================
        //
        // Existing:
        //
        // extension
        // content
        // 10 MB security limit
        //
        // first validate pannuvom.

        FileValidator.Validate(
            dto.File);


        // =================================================
        // R7.9 — PLAN ENTITLEMENT CHECK
        // =================================================
        //
        // Example Free:
        //
        // 20 documents max
        // 50 MB total
        //
        // Premium:
        //
        // 200 documents
        // 500 MB total
        //
        // Frontend bypass pannalum
        // backend check nadakkum.

        string? blockReason =
            await _subscriptionEntitlementService
                .GetUploadBlockReasonAsync(
                    dto.File.Length);


        if (!string.IsNullOrWhiteSpace(
            blockReason))
        {
            throw new ArgumentException(
                blockReason);
        }


        // =================================================
        // FOLDER OWNERSHIP CHECK
        // =================================================

        if (dto.FolderId.HasValue)
        {
            var folder =
                await _folderRepository
                    .GetOwnedAsync(
                        dto.FolderId.Value,
                        _currentUser.UserId);


            if (folder == null)
            {
                throw new ArgumentException(
                    "Selected folder does not belong to the current user."
                );
            }
        }


        // =================================================
        // READ FILE
        // =================================================

        using var memoryStream =
            new MemoryStream();


        await dto.File
            .CopyToAsync(
                memoryStream);


        var plainBytes =
            memoryStream.ToArray();


        // =================================================
        // RANDOM STORAGE NAME
        // =================================================

        var storedFileName =
            $"{Guid.NewGuid():N}.vault";


        // =================================================
        // ENCRYPT
        // =================================================

        var encryptedBytes =
            _aes.EncryptBytes(
                plainBytes);


        // =================================================
        // SAVE ENCRYPTED FILE
        // =================================================

        var storagePath =
            await _storage
                .SaveAsync(
                    storedFileName,
                    encryptedBytes);


        // =================================================
        // CREATE DOCUMENT METADATA
        // =================================================

        var document =
            new Document
            {
                OriginalFileName =
                    Path.GetFileName(
                        dto.File.FileName),

                StoredFileName =
                    storedFileName,

                ContentType =
                    dto.File.ContentType,

                FileSize =
                    dto.File.Length,

                // Plain file integrity hash.
                FileHash =
                    _hash.Sha256(
                        plainBytes),

                StoragePath =
                    storagePath,

                FolderId =
                    dto.FolderId,

                UserId =
                    _currentUser.UserId
            };


        // =================================================
        // SAVE DATABASE
        // =================================================

        await _documentRepository
            .AddAsync(
                document);


        return Map(
            document);
    }


    // =====================================================
    // GET CURRENT USER DOCUMENTS
    // =====================================================

    public async Task<List<DocumentDto>>
        GetAllAsync()
    {
        var documents =
            await _documentRepository
                .GetByUserAsync(
                    _currentUser.UserId);


        return documents
            .Select(
                Map)
            .ToList();
    }


    // =====================================================
    // GET DOCUMENT DETAILS
    // =====================================================

    public async Task<DocumentDto>
        GetAsync(
            int id)
    {
        var document =
            await _documentRepository
                .GetOwnedAsync(
                    id,
                    _currentUser.UserId);


        if (document == null)
        {
            throw new KeyNotFoundException(
                "Document not found."
            );
        }


        return Map(
            document);
    }


    // =====================================================
    // SECURE DOCUMENT DOWNLOAD
    // =====================================================
    //
    // OWNER
    //
    // OR
    //
    // ACCEPTED SECURE RECIPIENT
    //
    // mattum download panna mudiyum.

    public async Task<(
        byte[] Data,
        string ContentType,
        string FileName)>
        DownloadAsync(
            int id)
    {
        // =================================================
        // DOCUMENT LOAD
        // =================================================

        var document =
            await _documentRepository
                .GetByIdAsync(
                    id);


        if (document == null)
        {
            throw new KeyNotFoundException(
                "Document not found."
            );
        }


        var currentUserId =
            _currentUser.UserId;


        // =================================================
        // OWNER CHECK
        // =================================================

        var isOwner =
            document.UserId ==
            currentUserId;


        // =================================================
        // SHARED ACCESS CHECK
        // =================================================

        var hasSharedAccess =
            false;


        if (!isOwner)
        {
            hasSharedAccess =
                await _shareRepository
                    .HasAcceptedAccessAsync(
                        document.Id,
                        currentUserId);
        }


        // =================================================
        // AUTHORIZATION FAILURE
        // =================================================

        if (!isOwner &&
            !hasSharedAccess)
        {
            throw new KeyNotFoundException(
                "Document not found."
            );
        }


        // =================================================
        // READ ENCRYPTED FILE
        // =================================================

        var encryptedBytes =
            await _storage
                .ReadAsync(
                    document.StoragePath);


        // =================================================
        // DECRYPT
        // =================================================

        var plainBytes =
            _aes.DecryptBytes(
                encryptedBytes);


        // =================================================
        // SHA-256 INTEGRITY CHECK
        // =================================================

        var currentHash =
            _hash.Sha256(
                plainBytes);


        if (!string.Equals(
            currentHash,
            document.FileHash,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "File integrity check failed."
            );
        }


        // =================================================
        // SUCCESS
        // =================================================

        return (
            plainBytes,
            document.ContentType,
            document.OriginalFileName
        );
    }


    // =====================================================
    // UPDATE DOCUMENT
    // =====================================================

    public async Task<DocumentDto>
        UpdateAsync(
            int id,
            UpdateDocumentDto dto)
    {
        var document =
            await _documentRepository
                .GetOwnedAsync(
                    id,
                    _currentUser.UserId);


        if (document == null)
        {
            throw new KeyNotFoundException(
                "Document not found."
            );
        }


        if (string.IsNullOrWhiteSpace(
            dto.FileName))
        {
            throw new ArgumentException(
                "File name is required."
            );
        }


        var safeFileName =
            Path.GetFileName(
                dto.FileName.Trim());


        if (string.IsNullOrWhiteSpace(
            safeFileName))
        {
            throw new ArgumentException(
                "Invalid file name."
            );
        }


        document.OriginalFileName =
            safeFileName;


        document.UpdatedAt =
            DateTime.UtcNow;


        await _documentRepository
            .UpdateAsync(
                document);


        return Map(
            document);
    }


    // =====================================================
    // DELETE DOCUMENT
    // =====================================================

    public async Task DeleteAsync(
        int id)
    {
        var document =
            await _documentRepository
                .GetOwnedAsync(
                    id,
                    _currentUser.UserId);


        if (document == null)
        {
            throw new KeyNotFoundException(
                "Document not found."
            );
        }


        // =================================================
        // DELETE ENCRYPTED PHYSICAL FILE
        // =================================================

        _storage.Delete(
            document.StoragePath);


        // =================================================
        // DELETE DATABASE METADATA
        // =================================================

        await _documentRepository
            .DeleteAsync(
                document);
    }
}
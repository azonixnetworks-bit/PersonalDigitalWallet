using Microsoft.EntityFrameworkCore;
using PersonalDigitalVault.Api.Data;
using PersonalDigitalVault.Api.DTOs.Backup;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Security;
using PersonalDigitalVault.Api.Storage;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace PersonalDigitalVault.Api.Services;

public sealed class VaultBackupPackageService
{
    private const int PackageVersion = 1;
    private readonly AppDbContext _db;
    private readonly AesEncryption _aes;
    private readonly FileStorageService _storage;

    public VaultBackupPackageService(
        AppDbContext db,
        AesEncryption aes,
        FileStorageService storage)
    {
        _db = db;
        _aes = aes;
        _storage = storage;
    }

    public async Task<VaultBackupPackage> CreateAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        List<Folder> folders = await _db.Folders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        List<Document> documents = await _db.Documents
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        List<Credential> credentials = await _db.Credentials
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        string tempPath = Path.Combine(
            Path.GetTempPath(),
            $"pdv-backup-{Guid.NewGuid():N}.pdvbackup");

        try
        {
            await using (var file = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: false))
            {
                var manifest = new BackupManifest(
                    PackageVersion,
                    userId,
                    DateTime.UtcNow,
                    folders.Count,
                    documents.Count,
                    credentials.Count);

                await WriteEncryptedJsonEntryAsync(
                    archive,
                    "manifest.pdvenc",
                    manifest,
                    cancellationToken);

                await WriteEncryptedJsonEntryAsync(
                    archive,
                    "folders.pdvenc",
                    folders.Select(x => new FolderBackupRecord(
                        x.Id,
                        x.Name,
                        x.CreatedAt)).ToArray(),
                    cancellationToken);

                await WriteEncryptedJsonEntryAsync(
                    archive,
                    "documents.pdvenc",
                    documents.Select(x => new DocumentBackupRecord(
                        x.Id,
                        x.OriginalFileName,
                        x.ContentType,
                        x.FileSize,
                        x.FileHash,
                        x.FolderId,
                        x.CreatedAt,
                        x.UpdatedAt)).ToArray(),
                    cancellationToken);

                await WriteEncryptedJsonEntryAsync(
                    archive,
                    "credentials.pdvenc",
                    credentials.Select(x => new CredentialBackupRecord(
                        x.Id,
                        x.Title,
                        x.UsernameEncrypted,
                        x.PasswordEncrypted,
                        x.Website,
                        x.NotesEncrypted,
                        x.CreatedAt,
                        x.UpdatedAt)).ToArray(),
                    cancellationToken);

                foreach (Document document in documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    byte[] encryptedDocument = await _storage.ReadAsync(document.StoragePath);
                    ZipArchiveEntry entry = archive.CreateEntry(
                        $"documents/{document.Id}.vault",
                        CompressionLevel.NoCompression);

                    await using Stream entryStream = entry.Open();
                    await entryStream.WriteAsync(encryptedDocument, cancellationToken);
                }
            }

            string hash = await ComputeSha256Async(tempPath, cancellationToken);
            var info = new FileInfo(tempPath);
            string fileName = $"PDV-Backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.pdvbackup";

            return new VaultBackupPackage(tempPath, fileName, hash, info.Length);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    public async Task<RestoreBackupResultDto> RestoreAsync(
        int userId,
        string packagePath,
        string? expectedHash,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath))
        {
            throw new InvalidOperationException("Backup package is missing.");
        }

        string actualHash = await ComputeSha256Async(packagePath, cancellationToken);
        if (!string.IsNullOrWhiteSpace(expectedHash) &&
            !string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new CryptographicException("Backup integrity verification failed.");
        }

        await using var file = new FileStream(
            packagePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: false);

        BackupManifest manifest = await ReadEncryptedJsonEntryAsync<BackupManifest>(
            archive,
            "manifest.pdvenc",
            cancellationToken);

        if (manifest.Version != PackageVersion)
        {
            throw new InvalidOperationException("This PDV backup version is not supported.");
        }

        if (manifest.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("This backup belongs to a different PDV account.");
        }

        FolderBackupRecord[] folders = await ReadEncryptedJsonEntryAsync<FolderBackupRecord[]>(
            archive,
            "folders.pdvenc",
            cancellationToken);
        DocumentBackupRecord[] documents = await ReadEncryptedJsonEntryAsync<DocumentBackupRecord[]>(
            archive,
            "documents.pdvenc",
            cancellationToken);
        CredentialBackupRecord[] credentials = await ReadEncryptedJsonEntryAsync<CredentialBackupRecord[]>(
            archive,
            "credentials.pdvenc",
            cancellationToken);

        if (folders.Length != manifest.FolderCount ||
            documents.Length != manifest.DocumentCount ||
            credentials.Length != manifest.CredentialCount)
        {
            throw new CryptographicException("Backup package metadata is incomplete or corrupted.");
        }

        var savedPhysicalFiles = new List<string>();
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var folderMap = new Dictionary<int, int>();
            int foldersRestored = 0;
            int documentsRestored = 0;
            int credentialsRestored = 0;
            int skipped = 0;

            List<Folder> existingFolders = await _db.Folders
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (FolderBackupRecord backupFolder in folders)
            {
                Folder? existing = existingFolders.FirstOrDefault(x =>
                    string.Equals(x.Name, backupFolder.Name, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    folderMap[backupFolder.Id] = existing.Id;
                    skipped++;
                    continue;
                }

                var folder = new Folder
                {
                    UserId = userId,
                    Name = backupFolder.Name,
                    CreatedAt = backupFolder.CreatedAt
                };

                _db.Folders.Add(folder);
                await _db.SaveChangesAsync(cancellationToken);
                folderMap[backupFolder.Id] = folder.Id;
                existingFolders.Add(folder);
                foldersRestored++;
            }

            List<Document> existingDocuments = await _db.Documents
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (DocumentBackupRecord backupDocument in documents)
            {
                bool alreadyExists = existingDocuments.Any(x =>
                    string.Equals(x.FileHash, backupDocument.FileHash, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.OriginalFileName, backupDocument.OriginalFileName, StringComparison.OrdinalIgnoreCase));

                if (alreadyExists)
                {
                    skipped++;
                    continue;
                }

                ZipArchiveEntry? documentEntry = archive.GetEntry($"documents/{backupDocument.Id}.vault");
                if (documentEntry is null)
                {
                    throw new CryptographicException("Backup package is missing an encrypted document.");
                }

                byte[] encryptedBytes;
                await using (Stream documentStream = documentEntry.Open())
                using (var memory = new MemoryStream())
                {
                    await documentStream.CopyToAsync(memory, cancellationToken);
                    encryptedBytes = memory.ToArray();
                }

                // Existing vault files are AES-GCM authenticated. Decrypt only to verify
                // integrity, then discard plaintext immediately. Plaintext is never written.
                byte[] verifiedPlaintext = _aes.DecryptBytes(encryptedBytes);
                CryptographicOperations.ZeroMemory(verifiedPlaintext);

                string storedFileName = $"{Guid.NewGuid():N}.vault";
                string storagePath = await _storage.SaveAsync(storedFileName, encryptedBytes);
                savedPhysicalFiles.Add(storagePath);

                int? folderId = null;
                if (backupDocument.FolderId.HasValue &&
                    folderMap.TryGetValue(backupDocument.FolderId.Value, out int mappedFolderId))
                {
                    folderId = mappedFolderId;
                }

                var document = new Document
                {
                    UserId = userId,
                    OriginalFileName = backupDocument.OriginalFileName,
                    StoredFileName = storedFileName,
                    ContentType = backupDocument.ContentType,
                    FileSize = backupDocument.FileSize,
                    FileHash = backupDocument.FileHash,
                    StoragePath = storagePath,
                    FolderId = folderId,
                    CreatedAt = backupDocument.CreatedAt,
                    UpdatedAt = backupDocument.UpdatedAt
                };

                _db.Documents.Add(document);
                existingDocuments.Add(document);
                documentsRestored++;
            }

            List<Credential> existingCredentials = await _db.Credentials
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (CredentialBackupRecord backupCredential in credentials)
            {
                bool alreadyExists = existingCredentials.Any(x =>
                    string.Equals(x.Title, backupCredential.Title, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Website ?? string.Empty, backupCredential.Website ?? string.Empty, StringComparison.OrdinalIgnoreCase));

                if (alreadyExists)
                {
                    skipped++;
                    continue;
                }

                // Validate backed-up ciphertext before persisting it again.
                _ = _aes.DecryptString(backupCredential.UsernameEncrypted);
                _ = _aes.DecryptString(backupCredential.PasswordEncrypted);
                if (!string.IsNullOrWhiteSpace(backupCredential.NotesEncrypted))
                {
                    _ = _aes.DecryptString(backupCredential.NotesEncrypted);
                }

                var credential = new Credential
                {
                    UserId = userId,
                    Title = backupCredential.Title,
                    UsernameEncrypted = backupCredential.UsernameEncrypted,
                    PasswordEncrypted = backupCredential.PasswordEncrypted,
                    Website = backupCredential.Website,
                    NotesEncrypted = backupCredential.NotesEncrypted,
                    CreatedAt = backupCredential.CreatedAt,
                    UpdatedAt = backupCredential.UpdatedAt
                };

                _db.Credentials.Add(credential);
                existingCredentials.Add(credential);
                credentialsRestored++;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RestoreBackupResultDto
            {
                FoldersRestored = foldersRestored,
                DocumentsRestored = documentsRestored,
                CredentialsRestored = credentialsRestored,
                ItemsSkipped = skipped,
                Message = "Backup verified and restored successfully. Existing items were preserved."
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            foreach (string path in savedPhysicalFiles)
            {
                try
                {
                    _storage.Delete(path);
                }
                catch
                {
                    // Database rollback is authoritative. Cleanup failure must not
                    // replace the original restore failure.
                }
            }

            throw;
        }
    }

    public static void DeleteTemporaryPackage(string path) => TryDelete(path);

    private async Task WriteEncryptedJsonEntryAsync<T>(
        ZipArchive archive,
        string entryName,
        T value,
        CancellationToken cancellationToken)
    {
        byte[] plainJson = JsonSerializer.SerializeToUtf8Bytes(value);
        byte[] encrypted = _aes.EncryptBytes(plainJson);
        CryptographicOperations.ZeroMemory(plainJson);

        try
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
            await using Stream stream = entry.Open();
            await stream.WriteAsync(encrypted, cancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encrypted);
        }
    }

    private async Task<T> ReadEncryptedJsonEntryAsync<T>(
        ZipArchive archive,
        string entryName,
        CancellationToken cancellationToken)
    {
        ZipArchiveEntry entry = archive.GetEntry(entryName)
            ?? throw new CryptographicException("Backup package is incomplete.");

        if (entry.Length <= 0 || entry.Length > 50 * 1024 * 1024)
        {
            throw new CryptographicException("Backup metadata is invalid.");
        }

        byte[] encrypted;
        await using (Stream stream = entry.Open())
        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory, cancellationToken);
            encrypted = memory.ToArray();
        }

        byte[] plain = _aes.DecryptBytes(encrypted);
        try
        {
            return JsonSerializer.Deserialize<T>(plain)
                ?? throw new CryptographicException("Backup metadata is invalid.");
        }
        catch (JsonException)
        {
            throw new CryptographicException("Backup metadata is invalid.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
            CryptographicOperations.ZeroMemory(encrypted);
        }
    }

    private static async Task<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using SHA256 sha = SHA256.Create();
        byte[] hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temporary-file cleanup is best-effort.
        }
    }

    private sealed record BackupManifest(
        int Version,
        int OwnerUserId,
        DateTime CreatedAt,
        int FolderCount,
        int DocumentCount,
        int CredentialCount);

    private sealed record FolderBackupRecord(
        int Id,
        string Name,
        DateTime CreatedAt);

    private sealed record DocumentBackupRecord(
        int Id,
        string OriginalFileName,
        string ContentType,
        long FileSize,
        string FileHash,
        int? FolderId,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    private sealed record CredentialBackupRecord(
        int Id,
        string Title,
        string UsernameEncrypted,
        string PasswordEncrypted,
        string? Website,
        string? NotesEncrypted,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
}

public sealed record VaultBackupPackage(
    string LocalPath,
    string FileName,
    string IntegrityHash,
    long Size);

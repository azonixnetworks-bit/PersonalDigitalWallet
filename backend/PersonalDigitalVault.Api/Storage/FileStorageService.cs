using Microsoft.AspNetCore.Hosting;

namespace PersonalDigitalVault.Api.Storage;

public class FileStorageService
{
    // =========================================================
    // SECURE STORAGE ROOT
    // =========================================================

    // Encrypted .vault files save aagura main folder.
    //
    // Example:
    // backend/SecureStorage/
    //
    // Security:
    // Indha folder wwwroot-kulla irukka koodathu.
    private readonly string _root;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Storage root path configuration-la irundhu read pannum.
    //
    // Input:
    // IConfiguration = appsettings configuration.
    // IWebHostEnvironment = application folder information.
    //
    // Reason:
    // SecureStorage exact absolute path determine panna.
    //
    // Output:
    // _root-la safe full path store aagum.
    //
    // Security:
    // SecureStorage wwwroot-kulla irundha application startup
    // fail aagum.
    public FileStorageService(
        IConfiguration config,
        IWebHostEnvironment environment)
    {
        string configuredRoot =
            config["Storage:RootPath"] ?? "../SecureStorage";


        // -----------------------------------------------------
        // RELATIVE / ABSOLUTE PATH HANDLE
        // -----------------------------------------------------

        // Absolute path configuration irundha direct full path.
        if (Path.IsPathFullyQualified(configuredRoot))
        {
            _root = Path.GetFullPath(configuredRoot);
        }
        else
        {
            // Relative path current working directory mela
            // depend aagama application ContentRootPath mela
            // calculate pannrom.
            _root = Path.GetFullPath(
                configuredRoot,
                environment.ContentRootPath);
        }


        // -----------------------------------------------------
        // WWWROOT SECURITY CHECK
        // -----------------------------------------------------

        string webRootPath;

        if (!string.IsNullOrWhiteSpace(environment.WebRootPath))
        {
            webRootPath =
                Path.GetFullPath(environment.WebRootPath);
        }
        else
        {
            webRootPath =
                Path.GetFullPath(
                    Path.Combine(
                        environment.ContentRootPath,
                        "wwwroot"));
        }


        // SecureStorage wwwroot-kulla irundha
        // browser/static file middleware moolama expose aagura
        // risk irukku.
        if (IsSameOrInside(_root, webRootPath))
        {
            throw new InvalidOperationException(
                "Secure storage cannot be inside wwwroot.");
        }


        // Storage folder illana create pannum.
        Directory.CreateDirectory(_root);
    }


    // =========================================================
    // SAVE ENCRYPTED FILE
    // =========================================================

    // Function:
    // AES encrypted bytes-a SecureStorage-la save pannum.
    //
    // Input:
    // storedFileName = random generated .vault filename.
    // encrypted      = AES encrypted file bytes.
    //
    // Example:
    // 4ad31c0d-xxxx-xxxx-xxxx.vault
    //
    // Reason:
    // Actual user filename use pannaama random storage filename
    // use panni private file securely save panna.
    //
    // Output:
    // Saved file-oda absolute path return pannum.
    //
    // Security:
    // Path traversal block pannum.
    // Absolute path input block pannum.
    // Folder path input block pannum.
    // Existing file overwrite panna allow pannaathu.
    public async Task<string> SaveAsync(
        string storedFileName,
        byte[] encrypted)
    {
        if (encrypted == null || encrypted.Length == 0)
        {
            throw new ArgumentException(
                "Encrypted file data is required.",
                nameof(encrypted));
        }


        // Safe final file path calculate pannrom.
        string safePath =
            GetSafeSavePath(storedFileName);


        // -----------------------------------------------------
        // CREATE NEW FILE ONLY
        // -----------------------------------------------------

        // FileMode.CreateNew:
        //
        // File already irundha overwrite pannaathu.
        // IOException throw pannum.
        //
        // Random GUID filename normally duplicate aagaathu.
        using var stream = new FileStream(
            safePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);


        await stream.WriteAsync(
            encrypted,
            0,
            encrypted.Length);


        return safePath;
    }


    // =========================================================
    // READ ENCRYPTED FILE
    // =========================================================

    // Function:
    // SecureStorage-la irukkura encrypted .vault file read pannum.
    //
    // Input:
    // Database-la stored file path.
    //
    // Reason:
    // Document download time encrypted file bytes venum.
    //
    // Output:
    // Encrypted byte[] return pannum.
    //
    // Security:
    // Path SecureStorage-kulla irukka check pannitu thaan
    // file read pannum.
    public async Task<byte[]> ReadAsync(string path)
    {
        string safePath =
            GetSafeStoredPath(path);


        // Physical file missing-na safe exception.
        if (!File.Exists(safePath))
        {
            throw new FileNotFoundException(
                "Stored file was not found.");
        }


        return await File.ReadAllBytesAsync(safePath);
    }


    // =========================================================
    // DELETE ENCRYPTED FILE
    // =========================================================

    // Function:
    // SecureStorage-la irukkura encrypted file delete pannum.
    //
    // Input:
    // Database-la stored file path.
    //
    // Reason:
    // Document delete operation-ku physical .vault file
    // remove panna.
    //
    // Output:
    // Return value illa.
    //
    // Security:
    // SecureStorage veliya irukkura path delete panna mudiyathu.
    //
    // IMPORTANT:
    // Delete fail aana exception swallow panna maatom.
    // DocumentService later database consistency handle pannum.
    public void Delete(string path)
    {
        string safePath =
            GetSafeStoredPath(path);


        // Existing project behavior preserve pannrom:
        // File missing-na simply return.
        if (!File.Exists(safePath))
        {
            return;
        }


        File.Delete(safePath);
    }


    // =========================================================
    // SAFE SAVE PATH
    // =========================================================

    // Function:
    // Save panna vara stored filename safe-aa check pannum.
    //
    // Input:
    // storedFileName
    //
    // Reason:
    // ../
    // C:\
    // subfolder/file
    //
    // maari malicious paths block panna.
    //
    // Output:
    // SecureStorage-kulla irukkura absolute safe path.
    private string GetSafeSavePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            throw new InvalidOperationException(
                "Invalid stored file name.");
        }


        // Absolute/rooted path accept panna koodathu.
        if (Path.IsPathRooted(storedFileName))
        {
            throw new InvalidOperationException(
                "Invalid stored file name.");
        }


        // Directory separator irukka koodathu.
        //
        // Forward slash + backslash rendu explicit-a check
        // pannrom because application Windows/Linux-la
        // run aagalaam.
        if (storedFileName.Contains('/') ||
            storedFileName.Contains('\\'))
        {
            throw new InvalidOperationException(
                "Invalid stored file name.");
        }


        // Filename mattum thaan allow.
        if (Path.GetFileName(storedFileName) != storedFileName)
        {
            throw new InvalidOperationException(
                "Invalid stored file name.");
        }


        // OS invalid filename characters block.
        if (storedFileName.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException(
                "Invalid stored file name.");
        }


        // Project encrypted storage files .vault extension
        // mattum use panna vendum.
        if (!string.Equals(
                Path.GetExtension(storedFileName),
                ".vault",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid stored file name.");
        }


        // Final absolute path calculate pannrom.
        string fullPath =
            Path.GetFullPath(
                storedFileName,
                _root);


        // Final check:
        // Resolved path root-kulla thaan irukkanum.
        EnsureInsideRoot(fullPath);


        return fullPath;
    }


    // =========================================================
    // SAFE EXISTING STORAGE PATH
    // =========================================================

    // Function:
    // Read/Delete-ku varra existing DB path safe-aa check pannum.
    //
    // Input:
    // Absolute or relative stored file path.
    //
    // Reason:
    // Existing database compatibility preserve pannitu
    // root containment enforce panna.
    //
    // Output:
    // Safe absolute path.
    private string GetSafeStoredPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                "Invalid storage path.");
        }


        string fullPath;


        try
        {
            // Database old records absolute path store pannirundha
            // compatibility maintain pannrom.
            if (Path.IsPathFullyQualified(path))
            {
                fullPath =
                    Path.GetFullPath(path);
            }
            else
            {
                // Relative path irundha SecureStorage root-ku
                // relative-a resolve pannrom.
                fullPath =
                    Path.GetFullPath(
                        path,
                        _root);
            }
        }
        catch
        {
            throw new InvalidOperationException(
                "Invalid storage path.");
        }


        EnsureInsideRoot(fullPath);


        // Stored physical files .vault mattum thaan.
        if (!string.Equals(
                Path.GetExtension(fullPath),
                ".vault",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid storage path.");
        }


        return fullPath;
    }


    // =========================================================
    // ROOT CONTAINMENT CHECK
    // =========================================================

    // Function:
    // Final resolved file path SecureStorage root-kulla
    // irukkutha check pannum.
    //
    // Input:
    // fullPath
    //
    // Reason:
    // ../ path traversal root veliya escape panna koodathu.
    //
    // Output:
    // Safe-na method continue.
    // Unsafe-na exception.
    private void EnsureInsideRoot(string fullPath)
    {
        string relativePath =
            Path.GetRelativePath(
                _root,
                fullPath);


        // Example unsafe result:
        //
        // ..
        // ../secret.txt
        // ..\secret.txt
        //
        // Different drive/root path-um reject pannrom.
        if (relativePath == "." ||
            relativePath == ".." ||
            relativePath.StartsWith(
                ".." + Path.DirectorySeparatorChar) ||
            relativePath.StartsWith(
                ".." + Path.AltDirectorySeparatorChar) ||
            Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException(
                "Storage path is outside the secure storage folder.");
        }
    }


    // =========================================================
    // DIRECTORY CONTAINMENT CHECK
    // =========================================================

    // Function:
    // candidate folder parent folder-oda same-aa
    // illa athukulla irukkutha check pannum.
    //
    // Input:
    // candidate = SecureStorage path
    // parent    = wwwroot path
    //
    // Reason:
    // SecureStorage wwwroot-kulla configure aagakoodathu.
    //
    // Output:
    // true  = same / inside
    // false = outside
    private bool IsSameOrInside(
        string candidate,
        string parent)
    {
        string relativePath =
            Path.GetRelativePath(
                parent,
                candidate);


        if (relativePath == ".")
        {
            return true;
        }


        if (relativePath == "..")
        {
            return false;
        }


        if (relativePath.StartsWith(
                ".." + Path.DirectorySeparatorChar) ||
            relativePath.StartsWith(
                ".." + Path.AltDirectorySeparatorChar))
        {
            return false;
        }


        if (Path.IsPathRooted(relativePath))
        {
            return false;
        }


        return true;
    }
}
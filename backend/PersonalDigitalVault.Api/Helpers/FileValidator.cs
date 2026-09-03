using System.IO.Compression;

namespace PersonalDigitalVault.Api.Helpers;

public static class FileValidator
{
    // =========================================================
    // ALLOWED FILE TYPES
    // =========================================================

    // Project-la allow pannura file extensions.
    //
    // Security:
    // Unknown / executable file types upload aaga koodathu.
    private static readonly string[] AllowedExtensions =
    {
        ".pdf",
        ".doc",
        ".docx",
        ".jpg",
        ".jpeg",
        ".png"
    };


    // =========================================================
    // MAX FILE SIZE
    // =========================================================

    // Maximum upload size = 10 MB.
    //
    // Reason:
    // Very large files server memory/storage abuse panna
    // mudiyama limit pannrom.
    private const long MaxBytes =
        10 * 1024 * 1024;


    // =========================================================
    // MAIN VALIDATION METHOD
    // =========================================================

    // Function:
    // Uploaded file safe-aa irukka basic security checks pannum.
    //
    // Input:
    // IFormFile file
    //
    // Checks:
    // 1. File exists
    // 2. File empty illa
    // 3. Size <= 10 MB
    // 4. Filename safe
    // 5. Extension allowed
    // 6. Actual file signature extension-oda match aagutha
    //
    // Output:
    // Valid-na return value illa.
    // Invalid-na ArgumentException throw pannum.
    public static void Validate(IFormFile file)
    {
        // -----------------------------------------------------
        // FILE EXISTS CHECK
        // -----------------------------------------------------

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException(
                "Please select a file.");
        }


        // -----------------------------------------------------
        // FILE SIZE CHECK
        // -----------------------------------------------------

        if (file.Length > MaxBytes)
        {
            throw new ArgumentException(
                "File is larger than 10 MB.");
        }


        // -----------------------------------------------------
        // ORIGINAL FILE NAME CHECK
        // -----------------------------------------------------

        ValidateFileName(file.FileName);


        // -----------------------------------------------------
        // EXTENSION CHECK
        // -----------------------------------------------------

        string extension =
            Path.GetExtension(file.FileName)
                .ToLowerInvariant();


        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                "Unsupported file type.");
        }


        // -----------------------------------------------------
        // REAL FILE CONTENT CHECK
        // -----------------------------------------------------

        // Extension mattum trust panna koodathu.
        //
        // Example:
        //
        // malicious.exe
        //      ↓ rename
        // malicious.pdf
        //
        // Actual bytes PDF signature illa-na reject pannuvom.
        ValidateFileSignature(
            file,
            extension);
    }


    // =========================================================
    // FILE NAME VALIDATION
    // =========================================================

    // Function:
    // Original uploaded filename suspicious-aa irukka check pannum.
    //
    // Input:
    // Browser/client anuppura original filename.
    //
    // Reason:
    // ../
    // folder/file
    // control characters
    //
    // maari malicious names avoid panna.
    //
    // Output:
    // Valid-na continue.
    // Invalid-na exception.
    private static void ValidateFileName(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "Invalid file name.");
        }


        // Extremely long filename avoid pannrom.
        if (fileName.Length > 255)
        {
            throw new ArgumentException(
                "Invalid file name.");
        }


        // Directory/path separators original filename-la
        // allow panna maatom.
        //
        // Uploaded file-ku filename mattum venum.
        if (fileName.Contains('/') ||
            fileName.Contains('\\'))
        {
            throw new ArgumentException(
                "Invalid file name.");
        }


        // Null/control characters block pannrom.
        foreach (char character in fileName)
        {
            if (char.IsControl(character))
            {
                throw new ArgumentException(
                    "Invalid file name.");
            }
        }


        // "." and ".." filename-a allow panna koodathu.
        if (fileName == "." ||
            fileName == "..")
        {
            throw new ArgumentException(
                "Invalid file name.");
        }
    }


    // =========================================================
    // FILE SIGNATURE VALIDATION
    // =========================================================

    // Function:
    // File extension-oda actual bytes match aagutha check pannum.
    //
    // Input:
    // file      = uploaded file
    // extension = .pdf/.doc/.docx etc.
    //
    // Reason:
    // Extension spoofing reduce panna.
    //
    // Output:
    // Valid-na continue.
    // Invalid-na exception.
    //
    // Security:
    // HTTP Content-Type header user/client control panna mudiyum.
    // Athanala ContentType mattum trust pannaama
    // actual file bytes inspect pannrom.
    private static void ValidateFileSignature(
        IFormFile file,
        string extension)
    {
        bool valid;

        using Stream stream =
            file.OpenReadStream();


        switch (extension)
        {
            // -------------------------------------------------
            // PDF
            // -------------------------------------------------

            // PDF files normally:
            //
            // %PDF-
            //
            // bytes-oda start aagum.
            case ".pdf":
                valid = HasSignature(
                    stream,
                    new byte[]
                    {
                        0x25, // %
                        0x50, // P
                        0x44, // D
                        0x46, // F
                        0x2D  // -
                    });

                break;


            // -------------------------------------------------
            // JPEG
            // -------------------------------------------------

            // JPEG start:
            //
            // FF D8 FF
            case ".jpg":
            case ".jpeg":
                valid = HasSignature(
                    stream,
                    new byte[]
                    {
                        0xFF,
                        0xD8,
                        0xFF
                    });

                break;


            // -------------------------------------------------
            // PNG
            // -------------------------------------------------

            // PNG standard 8-byte signature.
            case ".png":
                valid = HasSignature(
                    stream,
                    new byte[]
                    {
                        0x89,
                        0x50,
                        0x4E,
                        0x47,
                        0x0D,
                        0x0A,
                        0x1A,
                        0x0A
                    });

                break;


            // -------------------------------------------------
            // OLD WORD .DOC
            // -------------------------------------------------

            // Legacy Microsoft Office compound file signature:
            //
            // D0 CF 11 E0 A1 B1 1A E1
            case ".doc":
                valid = HasSignature(
                    stream,
                    new byte[]
                    {
                        0xD0,
                        0xCF,
                        0x11,
                        0xE0,
                        0xA1,
                        0xB1,
                        0x1A,
                        0xE1
                    });

                break;


            // -------------------------------------------------
            // WORD .DOCX
            // -------------------------------------------------

            // DOCX normal ZIP container.
            //
            // ZIP header mattum check panna pothathu,
            // because ordinary .zip file-um PK header use pannum.
            //
            // Athanala internal Word entries-um check pannuvom.
            case ".docx":
                valid =
                    IsValidDocx(stream);

                break;


            default:
                valid = false;
                break;
        }


        if (!valid)
        {
            throw new ArgumentException(
                "File content does not match the selected file type.");
        }
    }


    // =========================================================
    // BASIC SIGNATURE CHECK
    // =========================================================

    // Function:
    // Stream first bytes expected signature-oda match aagutha
    // compare pannum.
    //
    // Input:
    // stream
    // expectedSignature
    //
    // Output:
    // true  = signature match
    // false = mismatch
    private static bool HasSignature(
        Stream stream,
        byte[] expectedSignature)
    {
        if (stream == null ||
            expectedSignature == null)
        {
            return false;
        }


        // Stream beginning-ku move pannrom.
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }


        byte[] actualBytes =
            new byte[expectedSignature.Length];


        int totalRead = 0;


        while (totalRead < actualBytes.Length)
        {
            int read =
                stream.Read(
                    actualBytes,
                    totalRead,
                    actualBytes.Length - totalRead);


            // File expected header vida short-aa irukku.
            if (read == 0)
            {
                return false;
            }


            totalRead += read;
        }


        // Byte-by-byte compare.
        for (int i = 0;
             i < expectedSignature.Length;
             i++)
        {
            if (actualBytes[i] !=
                expectedSignature[i])
            {
                return false;
            }
        }


        return true;
    }


    // =========================================================
    // DOCX VALIDATION
    // =========================================================

    // Function:
    // Uploaded .docx actual Word document structure-aa
    // irukka basic check pannum.
    //
    // Input:
    // Uploaded file stream.
    //
    // Reason:
    // Simple ZIP file-a .docx rename pannina
    // accept panna koodathu.
    //
    // Output:
    // true  = basic DOCX structure exists
    // false = invalid/corrupted/not DOCX
    private static bool IsValidDocx(
        Stream stream)
    {
        try
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }


            // First ZIP signature check.
            //
            // Most normal DOCX files:
            // PK 03 04
            if (!HasSignature(
                    stream,
                    new byte[]
                    {
                        0x50,
                        0x4B,
                        0x03,
                        0x04
                    }))
            {
                return false;
            }


            if (stream.CanSeek)
            {
                stream.Position = 0;
            }


            // DOCX is an Open XML ZIP package.
            using var archive =
                new ZipArchive(
                    stream,
                    ZipArchiveMode.Read,
                    leaveOpen: true);


            // Standard DOCX structure-la
            // indha entries irukkanum.
            bool hasContentTypes =
                archive.GetEntry(
                    "[Content_Types].xml") != null;


            bool hasWordDocument =
                archive.GetEntry(
                    "word/document.xml") != null;


            return
                hasContentTypes &&
                hasWordDocument;
        }
        catch (InvalidDataException)
        {
            // Broken/fake ZIP/DOCX.
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
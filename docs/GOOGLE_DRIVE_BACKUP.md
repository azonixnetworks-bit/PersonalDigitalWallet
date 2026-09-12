# Google Drive Encrypted Backup

PDV can create encrypted, owner-bound vault backups in the connected user's Google Drive `appDataFolder`.

## Security contract

- Google Drive receives no plaintext document content or plaintext credential values.
- Existing document payloads remain in their authenticated AES-GCM `.vault` form.
- Folder, document and credential metadata is AES encrypted inside the `.pdvbackup` package.
- The package SHA-256 value is stored as Drive app metadata and is verified before restore.
- OAuth refresh tokens and the optional Google account email are encrypted at rest in PDV.
- Short-lived Google access tokens are never persisted.
- Restore requires the authenticated PDV owner plus a fresh TOTP code. A consumed TOTP time-step cannot be reused.
- No API accepts another user's id for backup or restore operations.
- The PDV Admin role has no endpoint for opening another user's backup content.
- The Google OAuth state is HMAC signed and expires after 10 minutes.
- The Google Drive permission is limited to `https://www.googleapis.com/auth/drive.appdata` plus OpenID/email identity scopes.

## Google Cloud setup

1. Create or select a Google Cloud project.
2. Enable the Google Drive API.
3. Configure the OAuth consent screen.
4. Create an OAuth 2.0 Client ID of type **Web application**.
5. Add the exact PDV API callback URL as an Authorized redirect URI, for example:

   `https://localhost:5001/api/backup/google/callback`

6. Store the values outside source control using .NET User Secrets, environment variables, or the production secret manager.

Example development commands (replace values locally; never commit real secrets):

```powershell
dotnet user-secrets set "GoogleDrive:ClientId" "<google-client-id>"
dotnet user-secrets set "GoogleDrive:ClientSecret" "<google-client-secret>"
dotnet user-secrets set "GoogleDrive:RedirectUri" "https://localhost:5001/api/backup/google/callback"
dotnet user-secrets set "GoogleDrive:FrontendRedirectUri" "http://localhost:4200/backup"
dotnet user-secrets set "GoogleDrive:StateSigningKey" "<dedicated-random-secret-at-least-32-bytes>"
```

`GoogleDrive:StateSigningKey` must be a dedicated secret. Do not reuse the JWT key, MFA token key, AES vault key, Stripe secret, or Google client secret.

## Database migration

Apply the migration after pulling this feature:

```powershell
dotnet ef database update --project backend/PersonalDigitalVault.Api/PersonalDigitalVault.Api.csproj
```

The migration creates one `GoogleDriveBackupConnections` row per connected PDV user. The table stores only encrypted OAuth/account metadata and scheduling timestamps.

## Backup lifecycle

1. User opens **Backup** and chooses **Connect Google Drive**.
2. PDV creates a short-lived signed OAuth state and redirects to Google.
3. Google returns an authorization code to the PDV API callback.
4. PDV validates state, exchanges the code server-side, encrypts the refresh token, and redirects back to `/backup` without placing any token/code in the frontend URL.
5. **Backup Now** creates a `.pdvbackup` package in temporary server storage, uploads it with a resumable Drive upload, then deletes the temporary local package.
6. Automatic backup may be Daily, Weekly or Monthly. The server worker checks due schedules hourly, so the browser does not need to remain open.
7. Restore downloads the selected PDV-created app-data file to temporary storage, verifies SHA-256, validates encrypted metadata and owner binding, requires fresh TOTP, restores only missing items, and removes the temporary package.

## Restore behavior

Restore is non-destructive:

- existing folders with the same name are reused;
- existing documents with the same file hash and filename are skipped;
- existing credentials with the same title and website are skipped;
- missing folders, documents and credentials are restored;
- database restore work runs inside a transaction;
- physical files written during a failed restore are cleaned up best-effort.

## Encryption-key disaster recovery

Google Drive backup intentionally does **not** contain the PDV AES master key. A backup therefore protects vault data only if the production secret-management backup also preserves the exact `Security:AesKeyBase64` value used to encrypt that vault.

Losing or rotating that AES key without a controlled re-encryption process makes previously encrypted vault data and Google Drive backups unreadable. Keep the AES key in the approved production secret manager and back up the secret manager independently.

## Operational notes

- If Google revokes the refresh token, PDV will ask the user to reconnect Drive.
- Disconnect removes the encrypted local OAuth connection and attempts Google token revocation. Existing app-data backups are not silently deleted by PDV.
- Automatic backup failures are retried later rather than in a tight loop.
- Do not log authorization codes, access tokens, refresh tokens, backup plaintext, credential plaintext, or TOTP values.

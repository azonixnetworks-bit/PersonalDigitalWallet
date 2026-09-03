# Document Upload — Folder Selection + Encryption Progress

## Behaviour

- The Documents upload page loads the signed-in user's folders from `GET /api/folders`.
- Default destination is **Root Vault**. In this mode no `FolderId` is sent.
- If folders exist, the user can select **Inside a Folder** and choose one folder.
- The backend already validates that the selected folder belongs to the current user.
- If a selected folder is invalid/not owned by the current user, upload is rejected by the backend.
- No database migration is required for this change.

## Progress model

The browser can measure network upload bytes but cannot observe the internal percentage of the server-side AES operation through the existing single HTTP request.

For truthful UI reporting:

1. 0–90% = actual browser upload progress mapped into the secure-upload stage.
2. 90% = upload body reached the API; UI switches to an animated **Encrypting document...** state while server validation, AES encryption, encrypted `.vault` storage, hashing and metadata persistence complete.
3. 100% = only after the API returns success, meaning encryption/storage completed successfully.

The UI never reports "File encrypted" before the server confirms success.

## Changed files

- `backend/PersonalDigitalVault.Api/wwwroot/html/documents.html`
- `backend/PersonalDigitalVault.Api/wwwroot/css/vault.css`
- `backend/PersonalDigitalVault.Api/wwwroot/js/api/apiClient.js`
- `backend/PersonalDigitalVault.Api/wwwroot/js/api/documentApi.js`
- `backend/PersonalDigitalVault.Api/wwwroot/js/pages/documents.js`

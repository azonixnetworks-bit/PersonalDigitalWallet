import {
    api,
    apiUpload
} from './apiClient.js';


// =========================================================
// DOCUMENT API
// =========================================================
//
// PURPOSE:
//
// Documents page direct fetch() repeat pannaama
// common apiClient moolama backend call pannum.
//
// Flow:
//
// documents.js
//      ↓
// documentApi.js
//      ↓
// apiClient.js
//      ↓
// Authorization: Bearer FINAL_JWT
//      ↓
// DocumentController
//
// Security:
//
// Final JWT automatically apiClient attach pannum.
//
// StoragePath / StoredFileName / encrypted bytes
// frontend-ku varakoodathu.
// =========================================================

export const documentApi =
{
    // =====================================================
    // GET ALL MY DOCUMENTS
    // =====================================================
    //
    // API:
    // GET /api/documents
    //
    // Output:
    // Current user's own DocumentDto[].
    all:
        () =>
            api(
                '/documents'),


    // =====================================================
    // GET ONE DOCUMENT METADATA
    // =====================================================
    //
    // API:
    // GET /api/documents/{id}
    //
    // Input:
    // Document Id.
    //
    // Output:
    // DocumentDto.
    //
    // Security:
    // Backend owner check pannum.
    get:
        id =>
            api(
                `/documents/${id}`),


    // =====================================================
    // UPLOAD DOCUMENT
    // =====================================================
    //
    // API:
    // POST /api/documents/upload
    //
    // Input:
    // FormData
    //
    // file
    // folderId optional
    //
    // IMPORTANT:
    // Content-Type manually set panna koodathu.
    // Browser multipart boundary set pannum.
    upload:
        (formData, callbacks = {}) =>
            apiUpload(
                '/documents/upload',
                formData,
                callbacks),


    // =====================================================
    // RENAME DOCUMENT
    // =====================================================
    //
    // API:
    // PUT /api/documents/{id}
    //
    // Input:
    //
    // {
    //     fileName: "Passport-New.pdf"
    // }
    //
    // Output:
    // Updated DocumentDto.
    rename:
        (id, data) =>
            api(
                `/documents/${id}`,
                {
                    method:
                        'PUT',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // DELETE DOCUMENT
    // =====================================================
    //
    // API:
    // DELETE /api/documents/{id}
    //
    // Output:
    // null because backend returns 204.
    remove:
        id =>
            api(
                `/documents/${id}`,
                {
                    method:
                        'DELETE'
                }),


    // =====================================================
    // DOWNLOAD DOCUMENT
    // =====================================================
    //
    // API:
    // GET /api/documents/{id}/download
    //
    // Flow:
    //
    // Backend authorization
    //      ↓
    // Read encrypted .vault
    //      ↓
    // AES decrypt
    //      ↓
    // SHA-256 verify
    //      ↓
    // File response
    //
    // Input:
    // id
    // safe fileName
    //
    // Output:
    // Browser download.
    //
    // IMPORTANT:
    // Old code direct fetch() use pannuchu.
    //
    // New code common apiClient use pannum.
    //
    // So:
    // JWT handling
    // 401 handling
    // session cleanup
    //
    // ellam same common flow-la nadakkum.
    download:
        async (
            id,
            fileName) => {
            const blob =
                await api(
                    `/documents/${id}/download`);


            if (!(blob instanceof Blob)) {
                throw new Error(
                    'The document could not be downloaded.');
            }


            // Browser temporary object URL.
            const url =
                URL.createObjectURL(
                    blob);


            const link =
                document.createElement(
                    'a');


            link.href =
                url;


            link.download =
                fileName ||
                'document';


            // DOM-la temporary-a add pannitu
            // browser download trigger pannuvom.
            document.body
                .appendChild(
                    link);


            link.click();


            link.remove();


            // Temporary browser memory cleanup.
            window.setTimeout(
                () => {
                    URL.revokeObjectURL(
                        url);
                },
                1000);
        }
};
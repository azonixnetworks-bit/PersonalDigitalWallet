import { api } from './apiClient.js';
import { API_BASE_URL } from '../config.js';
import { tokenManager } from '../auth/tokenManager.js';


// =========================================================
// DOCUMENT SHARE API
// =========================================================
//
// PURPOSE:
//
// Secure document sharing related API calls
// ellathayum ore file-la maintain pannuvom.
//
// FEATURES:
//
// ✅ Share document
// ✅ Accept email invitation
// ✅ Shared With Me
// ✅ Owner share list
// ✅ Revoke share
// ✅ Secure shared document download
//
// SECURITY:
//
// Raw invitation token:
//
// ❌ console.log panna koodathu
// ❌ localStorage-la save panna koodathu
//
// share-invitation.js mattum
// sessionStorage-la temporary-aa handle pannum.
//
// Download:
// Final JWT Authorization header use pannum.
//
// =========================================================


export const documentShareApi = {


    // =====================================================
    // SHARE DOCUMENT
    // =====================================================
    //
    // API:
    //
    // POST /api/documents/{documentId}/share
    //
    // INPUT:
    //
    // documentId
    // recipientEmail
    //
    // Example:
    //
    // documentId = 10
    // recipientEmail = user@example.com
    //
    // Backend:
    //
    // Current user document owner-aa?
    //      ↓
    // Recipient registered-aa?
    //      ↓
    // Recipient active-aa?
    //      ↓
    // Email verified-aa?
    //      ↓
    // TOTP enabled-aa?
    //      ↓
    // Admin illa?
    //      ↓
    // Secure invitation create
    //
    // OUTPUT:
    //
    // DocumentShareDto
    //
    // IMPORTANT:
    //
    // Raw invitation token response-la vara koodathu.
    //
    share(documentId, recipientEmail) {

        // ---------------------------------------------
        // DOCUMENT ID VALIDATION
        // ---------------------------------------------

        const safeDocumentId =
            Number(documentId);


        if (
            !Number.isInteger(safeDocumentId)
            ||
            safeDocumentId <= 0
        ) {

            throw new Error(
                'Invalid document.'
            );
        }


        // ---------------------------------------------
        // EMAIL VALIDATION
        // ---------------------------------------------

        const safeRecipientEmail =
            String(
                recipientEmail ?? ''
            )
                .trim()
                .toLowerCase();


        if (!safeRecipientEmail) {

            throw new Error(
                'Recipient email is required.'
            );
        }


        return api(
            `/documents/${safeDocumentId}/share`,
            {
                method: 'POST',

                body: JSON.stringify({
                    recipientEmail:
                        safeRecipientEmail
                })
            }
        );
    },


    // =====================================================
    // ACCEPT EMAIL INVITATION
    // =====================================================
    //
    // API:
    //
    // POST /api/shares/accept
    //
    // INPUT:
    //
    // Raw email invitation token.
    //
    // IMPORTANT:
    //
    // Current backend DTO:
    //
    // AcceptShareRequestDto
    //
    // property:
    //
    // Token
    //
    // JSON body:
    //
    // {
    //     token: "RAW_TOKEN"
    // }
    //
    // invitationToken nu send panna koodathu.
    //
    // OUTPUT:
    //
    // Backend successful response.
    //
    accept(token) {

        const safeToken =
            String(
                token ?? ''
            )
                .trim();


        if (!safeToken) {

            throw new Error(
                'Secure invitation token is missing.'
            );
        }


        // IMPORTANT:
        //
        // Raw token-a:
        //
        // console.log(token)
        //
        // panna koodathu.

        return api(
            '/shares/accept',
            {
                method: 'POST',

                body: JSON.stringify({
                    token:
                        safeToken
                })
            }
        );
    },


    // =====================================================
    // SHARED WITH ME
    // =====================================================
    //
    // API:
    //
    // GET /api/shares/shared-with-me
    //
    // PURPOSE:
    //
    // Current logged-in recipient-ku
    // currently valid Accepted shares mattum retrieve pannum.
    //
    // OUTPUT:
    //
    // [
    //     {
    //         shareId,
    //         documentId,
    //         fileName,
    //         contentType,
    //         fileSize,
    //         sharedBy,
    //         sharedByEmail,
    //         status,
    //         sharedAt,
    //         acceptedAt
    //     }
    // ]
    //
    sharedWithMe() {

        return api(
            '/shares/shared-with-me'
        );
    },


    // =====================================================
    // OWNER SHARE LIST
    // =====================================================
    //
    // API:
    //
    // GET /api/documents/{documentId}/shares
    //
    // PURPOSE:
    //
    // Document owner:
    //
    // Pending
    // Accepted
    // Revoked
    //
    // share status பார்க்க use pannuvom.
    //
    // OUTPUT:
    //
    // DocumentShareDto[]
    //
    getDocumentShares(documentId) {

        const safeDocumentId =
            Number(documentId);


        if (
            !Number.isInteger(safeDocumentId)
            ||
            safeDocumentId <= 0
        ) {

            throw new Error(
                'Invalid document.'
            );
        }


        return api(
            `/documents/${safeDocumentId}/shares`
        );
    },


    // =====================================================
    // REVOKE SHARE
    // =====================================================
    //
    // API:
    //
    // DELETE
    // /api/documents/{documentId}/shares/{shareId}
    //
    // PURPOSE:
    //
    // Owner:
    //
    // Pending invitation
    //
    // OR
    //
    // Accepted share access
    //
    // revoke panna.
    //
    // After revoke:
    //
    // Recipient shared download work aaga koodathu.
    //
    revoke(documentId, shareId) {

        const safeDocumentId =
            Number(documentId);


        const safeShareId =
            Number(shareId);


        // ---------------------------------------------
        // DOCUMENT ID
        // ---------------------------------------------

        if (
            !Number.isInteger(safeDocumentId)
            ||
            safeDocumentId <= 0
        ) {

            throw new Error(
                'Invalid document.'
            );
        }


        // ---------------------------------------------
        // SHARE ID
        // ---------------------------------------------

        if (
            !Number.isInteger(safeShareId)
            ||
            safeShareId <= 0
        ) {

            throw new Error(
                'Invalid document share.'
            );
        }


        return api(
            `/documents/${safeDocumentId}/shares/${safeShareId}`,
            {
                method: 'DELETE'
            }
        );
    },


    // =====================================================
    // DOWNLOAD SHARED DOCUMENT
    // =====================================================
    //
    // API:
    //
    // GET /api/documents/{documentId}/download
    //
    // IMPORTANT:
    //
    // apiClient normal JSON response handle pannum.
    //
    // Aana document download response:
    //
    // binary file / Blob.
    //
    // Athanala direct fetch use pannuvom.
    //
    // BACKEND AUTHORIZATION:
    //
    // Document Owner
    //
    // OR
    //
    // Accepted secure recipient
    //
    // mattum download panna mudiyum.
    //
    // Frontend documentId guess panninaalum
    // backend authorization bypass panna mudiyadhu.
    //
    // OUTPUT:
    //
    // Blob
    //
    async download(documentId) {

        // ---------------------------------------------
        // DOCUMENT ID VALIDATION
        // ---------------------------------------------

        const safeDocumentId =
            Number(documentId);


        if (
            !Number.isInteger(safeDocumentId)
            ||
            safeDocumentId <= 0
        ) {

            throw new Error(
                'Invalid document.'
            );
        }


        // ---------------------------------------------
        // GET FINAL JWT
        // ---------------------------------------------

        const token =
            tokenManager.get();


        if (!token) {

            throw new Error(
                'You must login before downloading this file.'
            );
        }


        // ---------------------------------------------
        // DOWNLOAD REQUEST
        // ---------------------------------------------
        //
        // IMPORTANT:
        //
        // Token URL query-la poda maatom.
        //
        // Correct:
        //
        // Authorization: Bearer JWT
        //
        // Wrong:
        //
        // ?token=JWT
        //
        const response =
            await fetch(
                `${API_BASE_URL}/documents/${safeDocumentId}/download`,
                {
                    method: 'GET',

                    headers: {

                        Authorization:
                            `Bearer ${token}`
                    },

                    // Sensitive download request
                    // browser cache-la unnecessary-aa
                    // retain aagaama avoid panna.
                    cache:
                        'no-store',

                    // Referrer information unnecessary-aa
                    // share panna vendaam.
                    referrerPolicy:
                        'no-referrer'
                }
            );


        // ---------------------------------------------
        // ERROR
        // ---------------------------------------------

        if (!response.ok) {

            let errorMessage =
                `Download failed (${response.status}).`;


            try {

                const errorData =
                    await response.json();


                errorMessage =
                    errorData.message
                    ||
                    errorData.error
                    ||
                    errorMessage;
            }
            catch {

                // Backend JSON error return pannala na
                // generic safe message use pannuvom.
            }


            throw new Error(
                errorMessage
            );
        }


        // ---------------------------------------------
        // FILE RESPONSE
        // ---------------------------------------------

        const blob =
            await response.blob();


        return blob;
    }
};
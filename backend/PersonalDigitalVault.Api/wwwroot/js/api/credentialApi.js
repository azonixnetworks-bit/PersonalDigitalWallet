import {
    api
} from './apiClient.js';


// =========================================================
// CREDENTIAL API
// =========================================================
//
// PURPOSE:
//
// Credential page direct fetch() pannaama
// common apiClient moolama backend call pannum.
//
// Flow:
//
// credentials.js
//      ↓
// credentialApi.js
//      ↓
// apiClient.js
//      ↓
// FINAL JWT
//      ↓
// CredentialController
//
// SECURITY:
//
// Password / Notes frontend-lendhu send pannalum
// backend AES encrypt pannitu dhaan SQL-la save pannum.
//
// UserId frontend send panna maatom.
// =========================================================

export const credentialApi =
{
    // =====================================================
    // GET ALL MY CREDENTIALS
    // =====================================================
    //
    // API:
    // GET /api/credentials
    //
    // Output:
    // Current user's credentials.
    //
    // IMPORTANT:
    // Password default list-la masked.
    all:
        () =>
            api(
                '/credentials'),


    // =====================================================
    // GET ONE / REVEAL
    // =====================================================
    //
    // API:
    // GET /api/credentials/{id}
    //
    // Output:
    // Owner-authorized credential.
    //
    // Current backend behavior:
    // specific record password decrypt pannum.
    get:
        id =>
            api(
                `/credentials/${id}`),


    // =====================================================
    // CREATE
    // =====================================================
    //
    // API:
    // POST /api/credentials
    create:
        data =>
            api(
                '/credentials',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // UPDATE
    // =====================================================
    //
    // API:
    // PUT /api/credentials/{id}
    //
    // Backend sensitive values
    // re-encrypt pannum.
    update:
        (id, data) =>
            api(
                `/credentials/${id}`,
                {
                    method:
                        'PUT',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // DELETE
    // =====================================================
    //
    // API:
    // DELETE /api/credentials/{id}
    remove:
        id =>
            api(
                `/credentials/${id}`,
                {
                    method:
                        'DELETE'
                })
};
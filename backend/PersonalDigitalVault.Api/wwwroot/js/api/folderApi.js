import {
    api
} from './apiClient.js';


// =========================================================
// FOLDER API
// =========================================================
//
// PURPOSE:
//
// Folder page direct fetch() use pannaama
// common apiClient moolama backend API call pannum.
//
// Flow:
//
// folders.js
//     ↓
// folderApi.js
//     ↓
// apiClient.js
//     ↓
// Authorization: Bearer FINAL_JWT
//     ↓
// FolderController
//
// Security:
//
// Frontend UserId send panna maatom.
//
// Backend JWT current UserId use panni
// folder ownership check pannum.
// =========================================================

export const folderApi =
{
    // =====================================================
    // GET ALL MY FOLDERS
    // =====================================================
    //
    // API:
    // GET /api/folders
    //
    // Input:
    // None.
    //
    // Output:
    // Current logged-in user's FolderDto[].
    all:
        () =>
            api(
                '/folders'),


    // =====================================================
    // CREATE FOLDER
    // =====================================================
    //
    // API:
    // POST /api/folders
    //
    // Input:
    //
    // {
    //     name: "Certificates"
    // }
    //
    // Output:
    // Created FolderDto.
    create:
        data =>
            api(
                '/folders',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // UPDATE / RENAME FOLDER
    // =====================================================
    //
    // API:
    // PUT /api/folders/{id}
    //
    // Input:
    //
    // id
    //
    // {
    //     name: "Identity Documents"
    // }
    //
    // Output:
    // Updated FolderDto.
    update:
        (id, data) =>
            api(
                `/folders/${id}`,
                {
                    method:
                        'PUT',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // DELETE FOLDER
    // =====================================================
    //
    // API:
    // DELETE /api/folders/{id}
    //
    // Input:
    // Folder Id.
    //
    // Output:
    // null because backend returns 204 No Content.
    remove:
        id =>
            api(
                `/folders/${id}`,
                {
                    method:
                        'DELETE'
                })
};
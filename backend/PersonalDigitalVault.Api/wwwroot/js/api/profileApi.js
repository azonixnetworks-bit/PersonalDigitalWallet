import {
    api
} from './apiClient.js';


// =========================================================
// PROFILE API
// =========================================================
//
// PURPOSE:
//
// Profile page direct fetch() use pannaama
// common apiClient moolama backend call pannum.
//
// Flow:
//
// profile.js
//    ↓
// profileApi.js
//    ↓
// apiClient.js
//    ↓
// Authorization: Bearer FINAL_JWT
//    ↓
// ProfileController
//
// =========================================================

export const profileApi =
{
    // =====================================================
    // GET CURRENT USER PROFILE
    // =====================================================
    //
    // API:
    // GET /api/profile
    //
    // Input:
    // None.
    //
    // Authorization:
    // Final JWT automatically apiClient attach pannum.
    //
    // Output:
    // Current logged-in user ProfileDto.
    //
    // Security:
    // UserId frontend-lendhu send panna maatom.
    // Backend JWT current user identity use pannum.
    get:
        () =>
            api(
                '/profile'),


    // =====================================================
    // UPDATE CURRENT USER PROFILE
    // =====================================================
    //
    // API:
    // PUT /api/profile
    //
    // Input:
    //
    // {
    //     fullName: "..."
    // }
    //
    // Output:
    // Updated ProfileDto.
    //
    // Security:
    // Email / UserId / Role frontend update request-la
    // send panna maatom.
    update:
        data =>
            api(
                '/profile',
                {
                    method:
                        'PUT',

                    body:
                        JSON.stringify(
                            data)
                })
};
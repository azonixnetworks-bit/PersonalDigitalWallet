import {
    api
} from './apiClient.js';


// =========================================================
// SEARCH API
// =========================================================
//
// PURPOSE:
//
// Search page direct fetch() use pannaama
// common apiClient moolama backend search API call pannum.
//
// Flow:
//
// search.js
//      ↓
// searchApi.js
//      ↓
// apiClient.js
//      ↓
// FINAL JWT
//      ↓
// SearchController
//
// SECURITY:
//
// Search backend current logged-in UserId use pannum.
//
// Frontend UserId send panna maatom.
//
// Credential:
// title metadata mattum search result-la varanum.
//
// Password / username / notes reveal panna koodathu.
// =========================================================

export const searchApi =
{
    // =====================================================
    // SEARCH CURRENT USER VAULT
    // =====================================================
    //
    // API:
    // GET /api/search?keyword=passport
    //
    // Input:
    // keyword string.
    //
    // Output:
    //
    // [
    //     {
    //         type: "Document",
    //         id: 12,
    //         title: "Passport.pdf"
    //     }
    // ]
    search:
        keyword => {
            const safeKeyword =
                String(
                    keyword ?? '')
                    .trim();


            return api(
                `/search?keyword=${encodeURIComponent(
                    safeKeyword
                )}`);
        }
};
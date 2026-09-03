import {
    tokenManager
} from './tokenManager.js';


// =========================================================
// AUTH GUARD
// =========================================================
//
// PURPOSE:
//
// Protected frontend page open pannumbodhu
// valid final JWT browser-la irukkaa check pannum.
//
// IMPORTANT:
//
// Idhu frontend convenience check mattum.
//
// Actual authentication / authorization backend
// JWT middleware + Controller policies decide pannum.
//
// OUTPUT:
//
// true
// → usable final JWT irukku.
//
// false
// → token missing / expired / invalid.
//   login page-ku redirect pannuvom.
//
// =========================================================


// =========================================================
// REQUIRE AUTH
// =========================================================
export function requireAuth() {

    // =====================================================
    // GET FINAL JWT
    // =====================================================

    const token =
        tokenManager.get();


    // =====================================================
    // TOKEN MISSING
    // =====================================================

    if (!token) {

        tokenManager.clear();


        window.location.replace(
            '/html/login.html?reason=unauthorized'
        );


        return false;
    }


    // =====================================================
    // TOKEN EXPIRED / INVALID
    // =====================================================
    //
    // tokenManager.isExpired():
    //
    // JWT payload read pannum.
    // exp claim check pannum.
    //
    // Backend still final authority.
    if (tokenManager.isExpired()) {

        tokenManager.clear();


        window.location.replace(
            '/html/login.html?reason=expired'
        );


        return false;
    }


    // =====================================================
    // AUTHENTICATED
    // =====================================================

    return true;
}
import {
    tokenManager
} from '../auth/tokenManager.js';

import {
    documentShareApi
} from '../api/documentShareApi.js';


// =========================================================
// SECURE SHARE INVITATION PAGE
// =========================================================
//
// EMAIL LINK:
//
// share-invitation.html#token=SECRET
//
// FLOW:
//
// Read URL fragment
//      ↓
// Store raw token in sessionStorage
//      ↓
// Immediately remove fragment from address bar
//      ↓
// Final JWT available?
//      ├── NO → Login
//      │         ↓
//      │       Password
//      │         ↓
//      │       TOTP
//      │         ↓
//      │       Final JWT
//      │         ↓
//      └──────── Return here
//                ↓
// POST /api/shares/accept
//                ↓
// Shared With Me
//
// =========================================================


// =========================================================
// STORAGE KEYS
// =========================================================

const PENDING_SHARE_TOKEN_KEY =
    'pdv_pending_share_token';


const POST_LOGIN_REDIRECT_KEY =
    'pdv_post_login_redirect';


const SHARE_SUCCESS_KEY =
    'pdv_share_success';


// =========================================================
// PAGE PATH
// =========================================================

const INVITATION_PAGE =
    '/html/share-invitation.html';


const LOGIN_PAGE =
    '/html/login.html';


const SHARED_WITH_ME_PAGE =
    '/html/shared-with-me.html';


// =========================================================
// ELEMENTS
// =========================================================

const statusElement =
    document.getElementById(
        'shareStatus');


const loginAgainButton =
    document.getElementById(
        'loginAgainButton');


// =========================================================
// PAGE START
// =========================================================

initializeInvitation();


// =========================================================
// INITIALIZE
// =========================================================

async function initializeInvitation() {

    // =====================================================
    // 1. CAPTURE TOKEN FROM URL FRAGMENT
    // =====================================================

    captureTokenFromFragment();


    // =====================================================
    // 2. GET PENDING TOKEN
    // =====================================================

    const pendingToken =
        sessionStorage.getItem(
            PENDING_SHARE_TOKEN_KEY);


    if (!pendingToken ||
        pendingToken.trim() === '') {

        showStatus(
            'This secure invitation is missing or is no longer available.');

        showLoginAgain(
            false);

        return;
    }


    // =====================================================
    // 3. FINAL JWT CHECK
    // =====================================================

    if (!hasUsableFinalToken()) {

        // After password + TOTP,
        // login flow indha page-ku thirumba varanum.
        sessionStorage.setItem(
            POST_LOGIN_REDIRECT_KEY,
            INVITATION_PAGE);


        window.location.href =
            `${LOGIN_PAGE}?reason=share`;

        return;
    }


    // =====================================================
    // 4. ACCEPT SHARE
    // =====================================================

    await acceptPendingShare(
        pendingToken);
}


// =========================================================
// CAPTURE TOKEN FROM HASH
// =========================================================
//
// Example:
//
// #token=abc123
//
// URL fragment HTTP server request-la normally
// send aagaathu.
//
// Token read pannina udane address bar-lendhu
// remove pannuvom.
function captureTokenFromFragment() {

    const rawHash =
        window.location.hash;


    if (!rawHash ||
        rawHash === '#') {

        return;
    }


    try {
        const params =
            new URLSearchParams(
                rawHash.substring(1));


        const token =
            params.get(
                'token');


        if (token &&
            token.trim() !== '') {

            sessionStorage.setItem(
                PENDING_SHARE_TOKEN_KEY,
                token.trim());
        }
    }
    finally {
        // =============================================
        // REMOVE RAW TOKEN FROM ADDRESS BAR
        // =============================================

        const cleanUrl =
            window.location.pathname +
            window.location.search;


        window.history
            .replaceState(
                null,
                document.title,
                cleanUrl);
    }
}


// =========================================================
// FINAL TOKEN CHECK
// =========================================================
//
// Invitation token access JWT illa.
//
// tokenManager-la final access JWT mattum irukkanum.
function hasUsableFinalToken() {

    if (!tokenManager.has()) {
        return false;
    }


    if (tokenManager.isExpired()) {

        tokenManager.clear();

        return false;
    }


    return true;
}


// =========================================================
// ACCEPT PENDING SHARE
// =========================================================

async function acceptPendingShare(
    rawToken) {

    showStatus(
        'Verifying your account and accepting the secure invitation...');


    showLoginAgain(
        false);


    try {
        // =============================================
        // POST /api/shares/accept
        // =============================================

        await documentShareApi
            .accept(
                rawToken);


        // =============================================
        // SUCCESS
        // =============================================
        //
        // Raw token is single-use now.
        //
        // Browser copy immediately remove pannuvom.
        sessionStorage.removeItem(
            PENDING_SHARE_TOKEN_KEY);


        sessionStorage.removeItem(
            POST_LOGIN_REDIRECT_KEY);


        sessionStorage.setItem(
            SHARE_SUCCESS_KEY,
            'Document invitation accepted successfully.');


        window.location.href =
            SHARED_WITH_ME_PAGE;
    }
    catch {

        // =============================================
        // IMPORTANT
        // =============================================
        //
        // Failure reasons backend intentionally generic:
        //
        // wrong recipient
        // expired
        // revoked
        // already used
        // owner disabled
        // document unavailable
        //
        // ellathayum frontend distinguish panna try
        // panna koodathu.
        //
        // Raw token immediately delete panna maatom,
        // because wrong account logged-in irundhaal
        // correct recipient login panna chance venum.

        showStatus(
            'This invitation could not be accepted. It may be expired, revoked, already used, or belong to another account.');


        showLoginAgain(
            true);
    }
}


// =========================================================
// LOGIN WITH CORRECT ACCOUNT
// =========================================================

if (loginAgainButton) {

    loginAgainButton.addEventListener(
        'click',
        () => {
            // =============================================
            // PRESERVE SHARE TOKEN
            // =============================================
            //
            // pdv_pending_share_token remove panna maatom.


            // =============================================
            // CLEAR CURRENT FINAL JWT
            // =============================================

            tokenManager.clear();


            // =============================================
            // RETURN TARGET
            // =============================================

            sessionStorage.setItem(
                POST_LOGIN_REDIRECT_KEY,
                INVITATION_PAGE);


            window.location.href =
                `${LOGIN_PAGE}?reason=share`;
        });
}


// =========================================================
// STATUS
// =========================================================

function showStatus(
    text) {

    if (!statusElement) {
        return;
    }


    // Raw token / HTML inject panna maatom.
    statusElement.textContent =
        text;
}


// =========================================================
// LOGIN BUTTON
// =========================================================

function showLoginAgain(
    visible) {

    if (!loginAgainButton) {
        return;
    }


    loginAgainButton.hidden =
        !visible;
}
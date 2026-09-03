import {
    authApi
} from '../api/authApi.js';

import {
    tokenManager
} from '../auth/tokenManager.js';


// =========================================================
// LOGIN PAGE
// =========================================================
//
// NORMAL USER:
//
// Email + Password
//       ↓
// /api/auth/login
//       ↓
// requiresTotp = true
// challengeToken = TEMPORARY
// token = null
//       ↓
// verify-login-otp.html
//       ↓
// TOTP
//       ↓
// FINAL JWT
//
//
// ADMIN:
//
// Email + Password
//       ↓
// /api/auth/login
//       ↓
// requiresTotp = false
// token = FINAL JWT
//       ↓
// admin.html
//
// =========================================================


// =========================================================
// SESSION STORAGE KEYS
// =========================================================

// Temporary MFA challenge token.
//
// IMPORTANT:
// localStorage-la store panna maatom.
//
// 5 minute temporary token.
// Current tab/session-ku mattum.
const MFA_CHALLENGE_KEY =
    'pdv_login_mfa_challenge';


// Login display/helper email.
//
// Password store panna maatom.
const LOGIN_EMAIL_KEY =
    'pdv_login_email';


// Secure share invitation flow
// already use pannura redirect key.
const POST_LOGIN_REDIRECT_KEY =
    'pdv_post_login_redirect';


// =========================================================
// PAGE ELEMENTS
// =========================================================

const form =
    document.querySelector(
        'form');


const message =
    document.getElementById(
        'message');


if (!form) {
    throw new Error(
        'Login form was not found.');
}


// =========================================================
// OPTIONAL REASON MESSAGE
// =========================================================

showPageReason();


// =========================================================
// LOGIN SUBMIT
// =========================================================

form.addEventListener(
    'submit',
    async event => {
        event.preventDefault();


        showMessage(
            '');


        const email =
            form.email.value
                .trim()
                .toLowerCase();


        const password =
            form.password.value;


        if (!email ||
            !password) {

            showMessage(
                'Enter your email and password.');

            return;
        }


        const submitButton =
            form.querySelector(
                'button[type="submit"], button');


        if (submitButton) {
            submitButton.disabled =
                true;
        }


        try {
            // =============================================
            // PASSWORD STEP
            // =============================================

            const result =
                await authApi
                    .login(
                        {
                            email,
                            password
                        });


            // =============================================
            // NORMAL USER — MFA REQUIRED
            // =============================================

            if (result?.requiresTotp ===
                true) {

                // Final access token must NOT exist yet.
                if (!result.challengeToken) {
                    throw new Error(
                        'The login security challenge could not be created.');
                }


                // Old final JWT irundha remove pannuvom.
                //
                // Temporary challenge token-ai
                // tokenManager-la save panna koodathu.
                tokenManager.clear();


                sessionStorage.setItem(
                    MFA_CHALLENGE_KEY,
                    result.challengeToken);


                sessionStorage.setItem(
                    LOGIN_EMAIL_KEY,
                    result.email || email);


                window.location.href =
                    '/html/verify-login-otp.html';


                return;
            }


            // =============================================
            // ADMIN — DIRECT FINAL JWT
            // =============================================
            //
            // Current project behavior intentionally preserve.

            if (
                result?.requiresTotp === false
                &&
                typeof result.token === 'string'
                &&
                result.token.trim() !== ''
            ) {
                tokenManager.set(
                    result.token);


                // Temporary MFA values unnecessary.
                sessionStorage.removeItem(
                    MFA_CHALLENGE_KEY);


                sessionStorage.removeItem(
                    LOGIN_EMAIL_KEY);


                // =========================================
                // SHARE REDIRECT?
                // =========================================
                //
                // Admin normally share recipient aaga
                // koodathu.
                //
                // But generic post-login redirect key
                // irundhaal direct arbitrary URL trust
                // panna maatom.
                //
                // Admin direct admin dashboard.
                if (
                    String(result.role)
                        .toLowerCase() ===
                    'admin'
                ) {
                    window.location.href =
                        '/html/admin.html';

                    return;
                }


                goAfterLogin();

                return;
            }


            // =============================================
            // INVALID RESPONSE
            // =============================================

            throw new Error(
                'Login could not be completed.');
        }
        catch (error) {
            showMessage(
                error.message ||
                'Login failed.');
        }
        finally {
            if (submitButton) {
                submitButton.disabled =
                    false;
            }
        }
    });


// =========================================================
// POST LOGIN REDIRECT
// =========================================================
//
// Normal final JWT login piragu,
// share invitation flow இருந்தா அதுக்கு return pannalaam.
//
// Security:
// sessionStorage value arbitrary external URL-a
// navigate panna koodathu.
//
// Only local application path allow pannuvom.
function goAfterLogin() {
    const redirect =
        sessionStorage.getItem(
            POST_LOGIN_REDIRECT_KEY);


    if (isSafeLocalPath(
        redirect)) {

        window.location.href =
            redirect;

        return;
    }


    sessionStorage.removeItem(
        POST_LOGIN_REDIRECT_KEY);


    window.location.href =
        '/html/dashboard.html';
}


// =========================================================
// SAFE LOCAL PATH CHECK
// =========================================================

function isSafeLocalPath(
    value) {

    if (!value ||
        typeof value !== 'string') {
        return false;
    }


    // Must start single application root slash.
    //
    // Reject:
    // //evil.com
    // https://...
    // javascript:...
    return value.startsWith('/')
        &&
        !value.startsWith('//')
        &&
        !value.includes('\\');
}


// =========================================================
// PAGE REASON MESSAGE
// =========================================================

function showPageReason() {
    const params =
        new URLSearchParams(
            window.location.search);


    const reason =
        params.get(
            'reason');


    if (reason === 'expired') {
        showMessage(
            'Your session expired. Please login again.');

        return;
    }


    if (reason === 'unauthorized') {
        showMessage(
            'Please login again to continue.');

        return;
    }


    if (reason === 'share') {
        showMessage(
            'Login with the account that received the secure document invitation.');
    }
}


// =========================================================
// SHOW MESSAGE
// =========================================================
//
// Security:
// Error HTML render panna maatom.
// textContent mattum.
function showMessage(
    text) {

    if (!message) {
        return;
    }


    message.textContent =
        text;
}
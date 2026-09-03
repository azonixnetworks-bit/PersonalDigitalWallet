import {
    authApi
} from '../api/authApi.js';

import {
    tokenManager
} from '../auth/tokenManager.js';


// =========================================================
// LOGIN TOTP VERIFICATION PAGE
// =========================================================
//
// FLOW:
//
// Password login success
//        ↓
// Temporary challengeToken
//        ↓
// sessionStorage
//        ↓
// User enters authenticator code
//        ↓
// POST /api/auth/verify-login-totp
//        ↓
// FINAL JWT
//        ↓
// tokenManager.set()
//        ↓
// Dashboard / Share Invitation
//
// SECURITY:
//
// Temporary challenge token:
//
// ❌ localStorage
// ❌ tokenManager
//
// use panna maatom.
//
// FINAL JWT mattum tokenManager-ku pogum.
// =========================================================


// =========================================================
// SESSION STORAGE KEYS
// =========================================================

const MFA_CHALLENGE_KEY =
    'pdv_login_mfa_challenge';


const LOGIN_EMAIL_KEY =
    'pdv_login_email';


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


const codeInput =
    form?.querySelector(
        '[name="code"], [name="otp"]');


// Optional email display.
const emailDisplay =
    document.querySelector(
        '#emailDisplay, [data-login-email]');


// =========================================================
// GET MFA CHALLENGE
// =========================================================

const challengeToken =
    sessionStorage.getItem(
        MFA_CHALLENGE_KEY);


const loginEmail =
    sessionStorage.getItem(
        LOGIN_EMAIL_KEY);


// =========================================================
// PAGE INITIALIZATION
// =========================================================

if (emailDisplay &&
    loginEmail) {

    emailDisplay.textContent =
        loginEmail;
}


// User direct URL open pannina / challenge expired
// browser state missing-na login page-ku thirumba.
if (!challengeToken ||
    challengeToken.trim() === '') {

    window.location.href =
        '/html/login.html';
}


// =========================================================
// VERIFY LOGIN TOTP
// =========================================================

if (form) {
    form.addEventListener(
        'submit',
        async event => {
            event.preventDefault();


            showMessage(
                '');


            const currentChallenge =
                sessionStorage.getItem(
                    MFA_CHALLENGE_KEY);


            if (!currentChallenge) {
                showMessage(
                    'Your login verification session has expired.');

                return;
            }


            const code =
                codeInput?.value
                    ?.trim()
                || '';


            // =============================================
            // FRONTEND FORMAT VALIDATION
            // =============================================

            if (!/^\d{6}$/.test(
                code)) {

                showMessage(
                    'Enter the 6-digit authenticator code.');

                return;
            }


            const submitButton =
                form.querySelector(
                    'button[type="submit"], button');


            setButtonDisabled(
                submitButton,
                true);


            try {
                // =========================================
                // VERIFY LOGIN MFA
                // =========================================

                const result =
                    await authApi
                        .verifyLoginTotp(
                            {
                                challengeToken:
                                    currentChallenge.trim(),

                                code
                            });


                // =========================================
                // FINAL JWT REQUIRED
                // =========================================

                if (
                    !result
                    ||
                    typeof result.token !==
                    'string'
                    ||
                    result.token.trim() === ''
                ) {
                    throw new Error(
                        'Login verification could not be completed.');
                }


                // =========================================
                // STORE FINAL ACCESS JWT
                // =========================================
                //
                // Password + TOTP rendu success
                // aana piragu dhaan inga varuvom.
                tokenManager.set(
                    result.token);


                // =========================================
                // REMOVE TEMPORARY MFA DATA
                // =========================================

                sessionStorage.removeItem(
                    MFA_CHALLENGE_KEY);


                sessionStorage.removeItem(
                    LOGIN_EMAIL_KEY);


                if (codeInput) {
                    codeInput.value =
                        '';
                }


                // =========================================
                // ROLE CHECK
                // =========================================
                //
                // Normal flow-la indha page User-ku mattum.
                //
                // Still response role safe-a inspect pannuvom.
                if (
                    String(result.role)
                        .toLowerCase() ===
                    'admin'
                ) {
                    window.location.href =
                        '/html/admin.html';

                    return;
                }


                // =========================================
                // NEXT PAGE
                // =========================================

                goAfterLogin();
            }
            catch (error) {
                // Wrong code-na challenge token immediately
                // delete panna maatom.
                //
                // User valid current TOTP retry panna mudiyum
                // until backend challenge expires/rate-limit.
                showMessage(
                    error.message ||
                    'The authenticator code is invalid or expired.');
            }
            finally {
                setButtonDisabled(
                    submitButton,
                    false);
            }
        });
}


// =========================================================
// POST LOGIN REDIRECT
// =========================================================
//
// Secure document invitation flow:
//
// login
//   ↓
// TOTP
//   ↓
// final JWT
//   ↓
// return share-invitation page.
//
// Otherwise dashboard.
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


    // Unsafe/invalid redirect remove.
    sessionStorage.removeItem(
        POST_LOGIN_REDIRECT_KEY);


    window.location.href =
        '/html/dashboard.html';
}


// =========================================================
// SAFE LOCAL REDIRECT
// =========================================================
//
// Allow:
//
// /html/share-invitation.html
// /html/dashboard.html
//
// Block:
//
// https://evil.com
// //evil.com
// javascript:...
// \evil
function isSafeLocalPath(
    value) {

    if (!value ||
        typeof value !== 'string') {
        return false;
    }


    return value.startsWith('/')
        &&
        !value.startsWith('//')
        &&
        !value.includes('\\');
}


// =========================================================
// SHOW MESSAGE
// =========================================================

function showMessage(
    text) {

    if (!message) {
        return;
    }


    // XSS safety:
    // innerHTML use panna maatom.
    message.textContent =
        text;
}


// =========================================================
// BUTTON HELPER
// =========================================================

function setButtonDisabled(
    button,
    disabled) {

    if (!button) {
        return;
    }


    button.disabled =
        disabled;
}
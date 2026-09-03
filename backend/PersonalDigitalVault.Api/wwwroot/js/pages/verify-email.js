import {
    authApi
} from '../api/authApi.js';


// =========================================================
// VERIFY EMAIL PAGE
// =========================================================
//
// FLOW:
//
// register.js
//    ↓
// pdv_registration_email
//    ↓
// User enters 6-digit Email OTP
//    ↓
// POST /api/auth/verify-email
//    ↓
// setupToken
//    ↓
// sessionStorage
//    ↓
// setup-totp.html
//
// SECURITY:
//
// OTP localStorage-la save panna maatom.
//
// setupToken final JWT illa.
//
// setupToken sessionStorage-la mattum temporarily
// store pannuvom.
// =========================================================


// =========================================================
// SESSION STORAGE KEYS
// =========================================================

// Register page-lendhu vandha email.
const REGISTRATION_EMAIL_KEY =
    'pdv_registration_email';


// Email verification success piragu
// backend return pannura temporary TOTP setup token.
const TOTP_SETUP_TOKEN_KEY =
    'pdv_totp_setup_token';


// Previous setup QR response irundhaal
// fresh email verification flow start aagumbodhu
// clear panna use pannuvom.
const TOTP_SETUP_RESPONSE_KEY =
    'pdv_totp_setup_response';


// =========================================================
// GET PAGE ELEMENTS
// =========================================================

const form =
    document.querySelector(
        'form');


const message =
    document.getElementById(
        'message');


// OTP input.
//
// Existing HTML exact id different-a irundhaalum
// name="otp" or name="code" support pannuvom.
const otpInput =
    form?.querySelector(
        '[name="otp"], [name="code"]');


// Optional email input.
//
// Some page versions email display/input
// vachirukkalaam.
//
// Illa-na sessionStorage email use pannuvom.
const emailInput =
    form?.querySelector(
        '[name="email"]');


// Optional resend button.
//
// Existing page-la indha button illa-na
// script normal verification mattum work aagum.
const resendButton =
    document.querySelector(
        '#resendButton, [data-action="resend-otp"], button[name="resend"]');


// =========================================================
// PAGE INITIALIZATION
// =========================================================

const registrationEmail =
    getRegistrationEmail();


if (!registrationEmail) {
    // Registration flow data missing.
    //
    // User direct-a verify-email page open pannina
    // registration page-ku send pannuvom.
    window.location.href =
        '/html/register.html';
}


// Email input HTML-la irundhaal
// automatically fill pannuvom.
if (emailInput &&
    registrationEmail) {

    emailInput.value =
        registrationEmail;

    // Registration email user manually
    // change panna vendaam.
    emailInput.readOnly =
        true;
}


// =========================================================
// VERIFY EMAIL FORM SUBMIT
// =========================================================

if (form) {
    form.addEventListener(
        'submit',
        async event => {
            event.preventDefault();


            showMessage(
                '');


            const email =
                getRegistrationEmail();


            const otp =
                otpInput?.value
                    ?.trim()
                || '';


            // =============================================
            // BASIC FRONTEND VALIDATION
            // =============================================

            if (!email) {
                showMessage(
                    'Registration email was not found.');

                return;
            }


            // Email OTP exactly 6 digits.
            if (!/^\d{6}$/.test(
                otp)) {

                showMessage(
                    'Enter the 6-digit verification code.');

                return;
            }


            const submitButton =
                form.querySelector(
                    'button[type="submit"], button:not([name="resend"])');


            setButtonDisabled(
                submitButton,
                true);


            try {
                // =========================================
                // VERIFY EMAIL API
                // =========================================

                const result =
                    await authApi
                        .verifyEmail(
                            {
                                email,
                                otp
                            });


                // =========================================
                // VERIFY RESPONSE
                // =========================================

                if (
                    !result
                    ||
                    result.emailVerified !== true
                    ||
                    typeof result.setupToken !==
                    'string'
                    ||
                    result.setupToken.trim() === ''
                ) {
                    throw new Error(
                        'Email verification could not be completed.');
                }


                // =========================================
                // STORE TEMPORARY SETUP TOKEN
                // =========================================
                //
                // IMPORTANT:
                //
                // tokenManager.set() use panna koodathu.
                //
                // Idhu final access JWT illa.
                sessionStorage.setItem(
                    TOTP_SETUP_TOKEN_KEY,
                    result.setupToken.trim());


                // Old cached QR details remove pannuvom.
                sessionStorage.removeItem(
                    TOTP_SETUP_RESPONSE_KEY);


                // =========================================
                // REMOVE OTP FROM UI
                // =========================================

                if (otpInput) {
                    otpInput.value =
                        '';
                }


                // =========================================
                // GO TO AUTHENTICATOR SETUP
                // =========================================

                window.location.href =
                    '/html/setup-totp.html';
            }
            catch (error) {
                showMessage(
                    error.message ||
                    'The verification code is invalid or expired.');
            }
            finally {
                setButtonDisabled(
                    submitButton,
                    false);
            }
        });
}


// =========================================================
// RESEND OTP
// =========================================================

if (resendButton) {
    resendButton.addEventListener(
        'click',
        async event => {
            event.preventDefault();


            const email =
                getRegistrationEmail();


            if (!email) {
                showMessage(
                    'Registration email was not found.');

                return;
            }


            setButtonDisabled(
                resendButton,
                true);


            try {
                // Backend intentionally gives generic response.
                //
                // Account exists-aa illayaa frontend
                // reveal panna koodathu.
                const result =
                    await authApi
                        .resendEmailOtp(
                            {
                                email
                            });


                showMessage(
                    result?.message ||
                    'If the account is eligible, a new verification code will be sent.');
            }
            catch (error) {
                showMessage(
                    error.message ||
                    'The verification code could not be resent.');
            }
            finally {
                setButtonDisabled(
                    resendButton,
                    false);
            }
        });
}


// =========================================================
// GET REGISTRATION EMAIL
// =========================================================
//
// Function:
// sessionStorage-lendhu email retrieve pannum.
//
// Output:
// normalized email
// or empty string.
function getRegistrationEmail() {
    const storedEmail =
        sessionStorage.getItem(
            REGISTRATION_EMAIL_KEY);


    if (!storedEmail) {
        return '';
    }


    return storedEmail
        .trim()
        .toLowerCase();
}


// =========================================================
// SHOW MESSAGE
// =========================================================
//
// Security:
// innerHTML use panna maatom.
function showMessage(
    text) {

    if (!message) {
        return;
    }


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
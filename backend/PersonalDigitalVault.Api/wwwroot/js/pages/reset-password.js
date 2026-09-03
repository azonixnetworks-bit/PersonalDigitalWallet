import {
    authApi
} from '../api/authApi.js';


// =========================================================
// RESET PASSWORD PAGE
// =========================================================
//
// EMAIL LINK:
//
// /html/reset-password.html#token=ABC...
//
// Browser:
//
// window.location.hash
//       ↓
// token read
//       ↓
// URL immediately clean
//       ↓
// token memory-la mattum
//       ↓
// POST /api/auth/reset-password
//
// IMPORTANT:
//
// Reset token:
// localStorage ❌
// sessionStorage ❌
//
// JavaScript memory-la mattum irukkum.
//
// =========================================================


// =========================================================
// PAGE ELEMENTS
// =========================================================

const form =
    document.getElementById(
        'resetPasswordForm');


const message =
    document.getElementById(
        'message');


const submitButton =
    document.getElementById(
        'resetPasswordButton');


// =========================================================
// SAFETY CHECK
// =========================================================

if (!form) {
    throw new Error(
        'Reset password form was not found.');
}


// =========================================================
// READ RESET TOKEN
// =========================================================
//
// let use pannrom because reset success piragu
// memory-lendhu clear pannuvom.
//
let resetToken =
    getResetTokenFromUrl();


// =========================================================
// REMOVE TOKEN FROM ADDRESS BAR
// =========================================================
//
// Token read pannina udane URL fragment remove.
//
// Before:
//
// reset-password.html#token=SECRET
//
// After:
//
// reset-password.html
//
// Browser storage-la token save panna maatom.
//
if (resetToken) {

    window.history.replaceState(
        null,
        '',
        '/html/reset-password.html');
}


// =========================================================
// TOKEN MISSING
// =========================================================

if (!resetToken) {

    showMessage(
        'This password reset link is missing or invalid. Request a new reset link.',
        'error');


    disableForm();
}


// =========================================================
// FORM SUBMIT
// =========================================================

form.addEventListener(
    'submit',
    async event => {

        event.preventDefault();


        showMessage(
            '',
            'error');


        // =================================================
        // TOKEN CHECK
        // =================================================

        if (!resetToken) {

            showMessage(
                'This password reset link is invalid. Request a new reset link.',
                'error');

            return;
        }


        // =================================================
        // PASSWORD INPUT
        // =================================================

        const newPassword =
            form.newPassword.value;


        const confirmPassword =
            form.confirmPassword.value;


        // =================================================
        // BASIC VALIDATION
        // =================================================

        if (!newPassword ||
            !confirmPassword) {

            showMessage(
                'Enter and confirm your new password.',
                'error');

            return;
        }


        if (newPassword.length < 8) {

            showMessage(
                'Password must contain at least 8 characters.',
                'error');

            return;
        }


        if (newPassword !==
            confirmPassword) {

            showMessage(
                'Passwords do not match.',
                'error');

            return;
        }


        // =================================================
        // DISABLE BUTTON
        // =================================================

        if (submitButton) {

            submitButton.disabled =
                true;

            submitButton.textContent =
                'Resetting...';
        }


        try {

            // =============================================
            // RESET API
            // =============================================

            const result =
                await authApi
                    .resetPassword(
                        {
                            token:
                                resetToken,

                            newPassword,

                            confirmPassword
                        });


            // =============================================
            // TOKEN SINGLE USE
            // =============================================
            //
            // Backend token clear pannum.
            //
            // Frontend memory-lendhum remove pannuvom.
            //
            resetToken =
                '';


            // Password fields clear.
            form.reset();


            // Prevent second submit.
            disableForm();


            showMessage(
                result?.message ||
                'Password reset successfully. Redirecting to login...',
                'success');


            // =============================================
            // NORMAL LOGIN
            // =============================================
            //
            // Automatic JWT issue panna maatom.
            //
            // User:
            //
            // New password
            //      ↓
            // Existing TOTP
            //      ↓
            // Dashboard
            //
            window.setTimeout(
                () => {

                    window.location.href =
                        '/html/login.html';

                },
                2200);
        }
        catch (error) {

            showMessage(
                error.message ||
                'Password reset could not be completed.',
                'error');


            if (submitButton) {

                submitButton.disabled =
                    false;

                submitButton.textContent =
                    'Reset password';
            }
        }
    });


// =========================================================
// GET RESET TOKEN FROM URL
// =========================================================
//
// Input:
//
// #token=ABC...
//
// Output:
//
// ABC...
//
// Token URL-decoded by URLSearchParams.
//
function getResetTokenFromUrl() {

    const hash =
        window.location.hash;


    if (!hash ||
        hash.length <= 1) {

        return '';
    }


    const params =
        new URLSearchParams(
            hash.substring(1));


    const token =
        params.get(
            'token');


    if (!token ||
        typeof token !== 'string') {

        return '';
    }


    return token.trim();
}


// =========================================================
// DISABLE FORM
// =========================================================
//
// Invalid / successful reset-ku
// further submit prevent panna.
//
function disableForm() {

    const inputs =
        form.querySelectorAll(
            'input');


    inputs.forEach(
        input => {

            input.disabled =
                true;
        });


    if (submitButton) {

        submitButton.disabled =
            true;
    }
}


// =========================================================
// SHOW MESSAGE
// =========================================================
//
// Security:
// innerHTML use panna maatom.
//
function showMessage(
    text,
    type) {

    if (!message) {
        return;
    }


    message.textContent =
        text;


    message.classList.remove(
        'error',
        'success');


    if (type === 'success') {

        message.classList.add(
            'success');

        return;
    }


    message.classList.add(
        'error');
}
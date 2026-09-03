import {
    authApi
} from '../api/authApi.js';


// =========================================================
// FORGOT PASSWORD PAGE
// =========================================================
//
// FLOW:
//
// Email
//   ↓
// POST /api/auth/forgot-password
//   ↓
// Backend generic response
//   ↓
// Eligible account-na reset email
//
// IMPORTANT:
//
// Frontend account exists / not exists
// identify panna koodathu.
//
// Password/token edhuvum browser storage-la
// save panna maatom.
//
// =========================================================


// =========================================================
// PAGE ELEMENTS
// =========================================================

const form =
    document.getElementById(
        'forgotPasswordForm');


const message =
    document.getElementById(
        'message');


const submitButton =
    document.getElementById(
        'forgotPasswordButton');


// =========================================================
// SAFETY CHECK
// =========================================================

if (!form) {
    throw new Error(
        'Forgot password form was not found.');
}


// =========================================================
// SUBMIT
// =========================================================

form.addEventListener(
    'submit',
    async event => {

        event.preventDefault();


        showMessage(
            '',
            'error');


        // =================================================
        // EMAIL
        // =================================================

        const email =
            form.email.value
                .trim()
                .toLowerCase();


        if (!email) {

            showMessage(
                'Enter your email address.',
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
                'Sending...';
        }


        try {

            // =============================================
            // API CALL
            // =============================================

            const result =
                await authApi
                    .forgotPassword(
                        {
                            email
                        });


            // =============================================
            // GENERIC SUCCESS
            // =============================================
            //
            // Backend response account enumeration
            // prevent panna generic-aa irukkum.
            //
            showMessage(
                result?.message ||
                'If the account is eligible, a password reset link will be sent.',
                'success');


            // Password/email browser storage-la
            // save panna maatom.
        }
        catch (error) {

            showMessage(
                error.message ||
                'Password reset request could not be completed.',
                'error');
        }
        finally {

            if (submitButton) {

                submitButton.disabled =
                    false;

                submitButton.textContent =
                    'Send reset link';
            }
        }
    });


// =========================================================
// SHOW MESSAGE
// =========================================================
//
// Function:
// Error / success message safe-aa display pannum.
//
// Security:
// innerHTML use panna maatom.
// textContent mattum.
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
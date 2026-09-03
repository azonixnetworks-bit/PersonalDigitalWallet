import {
    authApi
} from '../api/authApi.js';


// =========================================================
// REGISTER PAGE
// =========================================================
//
// FLOW:
//
// FullName + Email + Password
//          ↓
// POST /api/auth/register
//          ↓
// Generic success response
//          ↓
// email sessionStorage
//          ↓
// verify-email.html
//
// IMPORTANT:
//
// Registration success-aana udane
// final JWT save panna maatom.
//
// =========================================================


// =========================================================
// STORAGE KEY
// =========================================================
//
// Email verification next page-ku email venum.
//
// Password save panna maatom.
// OTP save panna maatom.
const REGISTRATION_EMAIL_KEY =
    'pdv_registration_email';


// =========================================================
// PAGE ELEMENTS
// =========================================================

const form =
    document.querySelector(
        'form');


const message =
    document.getElementById(
        'message');


// =========================================================
// SAFETY CHECK
// =========================================================

if (!form) {
    throw new Error(
        'Registration form was not found.');
}


// =========================================================
// REGISTER SUBMIT
// =========================================================

form.addEventListener(
    'submit',
    async event => {
        event.preventDefault();


        // Previous message clear.
        if (message) {
            message.textContent =
                '';
        }


        // =================================================
        // READ INPUT
        // =================================================

        const fullName =
            form.fullName.value
                .trim();


        const email =
            form.email.value
                .trim()
                .toLowerCase();


        const password =
            form.password.value;


        // =================================================
        // BASIC FRONTEND VALIDATION
        // =================================================
        //
        // Backend validation still final authority.

        if (!fullName ||
            !email ||
            !password) {

            showMessage(
                'Please complete all required fields.');

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
            // REGISTER API
            // =============================================

            const result =
                await authApi
                    .register(
                        {
                            fullName,
                            email,
                            password
                        });


            // =============================================
            // IMPORTANT ANTI-ENUMERATION FLOW
            // =============================================
            //
            // Backend register endpoint generic response
            // return pannum.
            //
            // result.email normalized email-a irukkum.
            //
            // Existing account-aa / new account-aa
            // frontend decide panna koodathu.

            const registrationEmail =
                typeof result?.email ===
                    'string'
                    &&
                    result.email.trim() !== ''
                    ?
                    result.email
                        .trim()
                        .toLowerCase()
                    :
                    email;


            // Password store panna maatom.
            sessionStorage.setItem(
                REGISTRATION_EMAIL_KEY,
                registrationEmail);


            // =============================================
            // GO TO EMAIL OTP PAGE
            // =============================================

            window.location.href =
                '/html/verify-email.html';
        }
        catch (error) {
            showMessage(
                error.message ||
                'Registration could not be completed.');
        }
        finally {
            if (submitButton) {
                submitButton.disabled =
                    false;
            }
        }
    });


// =========================================================
// SHOW MESSAGE
// =========================================================
//
// Function:
// Safe textContent use panni error show pannum.
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
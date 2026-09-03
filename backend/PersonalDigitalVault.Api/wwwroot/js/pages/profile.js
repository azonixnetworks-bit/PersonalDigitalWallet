import {
    requireAuth
} from '../auth/authGuard.js';

import {
    profileApi
} from '../api/profileApi.js';


// =========================================================
// PROFILE PAGE
// =========================================================
//
// FLOW:
//
// Open Profile Page
//      ↓
// Check FINAL JWT
//      ↓
// GET /api/profile
//      ↓
// Fill Name + Email
//      ↓
// User edits Name
//      ↓
// PUT /api/profile
//      ↓
// Updated profile display
//
// SECURITY:
//
// ❌ UserId input illa
// ❌ Role edit panna mudiyadhu
// ❌ Email edit panna mudiyadhu
//
// Current authenticated user profile mattum.
// =========================================================


// =========================================================
// AUTHENTICATION CHECK
// =========================================================

const authenticated =
    requireAuth();


// =========================================================
// PAGE ELEMENTS
// =========================================================

const form =
    document.querySelector(
        'form');


const message =
    document.getElementById(
        'message');


const fullNameInput =
    form?.querySelector(
        '[name="fullName"]');


const emailInput =
    form?.querySelector(
        '[name="email"]');


const saveButton =
    form?.querySelector(
        'button[type="submit"], button');


// =========================================================
// PAGE START
// =========================================================

if (authenticated) {
    initializeProfilePage();
}


// =========================================================
// INITIALIZE PROFILE PAGE
// =========================================================
//
// Function:
// Backend-lendhu current authenticated user's
// profile load pannum.
//
// Input:
// None.
//
// Output:
// Form fields populate aagum.
async function initializeProfilePage() {
    // Required HTML elements missing-na
    // silent wrong behavior avoid pannuvom.
    if (!form ||
        !fullNameInput ||
        !emailInput) {

        showMessage(
            'Profile page could not be loaded.');

        return;
    }


    setFormDisabled(
        true);


    showMessage(
        'Loading profile...');


    try {
        // =================================================
        // GET CURRENT USER PROFILE
        // =================================================

        const profile =
            await profileApi
                .get();


        // =================================================
        // RESPONSE CHECK
        // =================================================

        if (!profile) {
            throw new Error(
                'Profile information was not returned.');
        }


        // =================================================
        // FILL FORM
        // =================================================

        fullNameInput.value =
            profile.fullName || '';


        emailInput.value =
            profile.email || '';


        // Email current simple project-la
        // profile page-lendhu change panna koodathu.
        emailInput.disabled =
            true;


        showMessage(
            '');
    }
    catch (error) {
        // apiClient 401-na token clear +
        // login redirect handle pannum.
        showMessage(
            error.message ||
            'Profile could not be loaded.');
    }
    finally {
        setFormDisabled(
            false);


        // Email always read-only/disabled.
        if (emailInput) {
            emailInput.disabled =
                true;
        }
    }
}


// =========================================================
// UPDATE PROFILE
// =========================================================

if (form) {
    form.addEventListener(
        'submit',
        async event => {
            event.preventDefault();


            // =============================================
            // AUTH CHECK
            // =============================================

            if (!requireAuth()) {
                return;
            }


            // =============================================
            // READ FULL NAME
            // =============================================

            const fullName =
                fullNameInput?.value
                    ?.trim()
                || '';


            // =============================================
            // BASIC VALIDATION
            // =============================================
            //
            // Backend validation still final authority.

            if (!fullName) {
                showMessage(
                    'Full name is required.');

                fullNameInput?.focus();

                return;
            }


            // =============================================
            // PREVENT DOUBLE SUBMIT
            // =============================================

            setButtonDisabled(
                saveButton,
                true);


            showMessage(
                'Saving profile...');


            try {
                // =========================================
                // PUT /api/profile
                // =========================================
                //
                // IMPORTANT:
                //
                // UserId
                // Email
                // Role
                //
                // request-la send panna maatom.
                const updatedProfile =
                    await profileApi
                        .update(
                            {
                                fullName
                            });


                // =========================================
                // SERVER RESPONSE -> UI
                // =========================================
                //
                // Client typed value mattum trust
                // pannaama backend response use pannuvom.

                if (updatedProfile) {
                    fullNameInput.value =
                        updatedProfile.fullName
                        || fullName;


                    if (updatedProfile.email) {
                        emailInput.value =
                            updatedProfile.email;
                    }
                }


                showMessage(
                    'Profile updated successfully.');
            }
            catch (error) {
                showMessage(
                    error.message ||
                    'Profile could not be updated.');
            }
            finally {
                setButtonDisabled(
                    saveButton,
                    false);
            }
        });
}


// =========================================================
// DISABLE / ENABLE FORM
// =========================================================
//
// Function:
// Initial loading time-la accidental edit/save
// prevent pannum.
//
// Input:
// true / false.
//
// Output:
// Form controls enable/disable.
function setFormDisabled(
    disabled) {

    if (!form) {
        return;
    }


    const controls =
        form.querySelectorAll(
            'input, button, select, textarea');


    controls.forEach(
        control => {
            control.disabled =
                disabled;
        });
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


// =========================================================
// SHOW MESSAGE
// =========================================================
//
// Function:
// User-ku success/error/status message show pannum.
//
// Security:
// innerHTML use panna maatom.
// textContent mattum.
function showMessage(
    text) {

    if (!message) {
        return;
    }


    message.textContent =
        text;
}
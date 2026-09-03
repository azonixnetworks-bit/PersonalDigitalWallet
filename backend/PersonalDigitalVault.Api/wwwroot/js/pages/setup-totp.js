import {
    authApi
} from '../api/authApi.js';


// =========================================================
// TOTP / AUTHENTICATOR SETUP PAGE
// =========================================================
//
// FLOW:
//
// pdv_totp_setup_token
//        ↓
// POST /api/auth/setup-totp
//        ↓
// QR Code
// Manual Secret
//        ↓
// User scans Google/Microsoft Authenticator
//        ↓
// Enter 6-digit code
//        ↓
// POST /api/auth/verify-totp-setup
//        ↓
// TOTP enabled
//        ↓
// Login page
//
// =========================================================


// =========================================================
// SESSION STORAGE KEYS
// =========================================================

const TOTP_SETUP_TOKEN_KEY =
    'pdv_totp_setup_token';


const TOTP_SETUP_RESPONSE_KEY =
    'pdv_totp_setup_response';


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


// Authenticator code input.
const codeInput =
    form?.querySelector(
        '[name="code"], [name="otp"]');


// Existing HTML-la possible QR image ids.
let qrImage =
    document.querySelector(
        '#qrCode, #qrCodeImage, [data-totp-qr]');


// Existing HTML-la possible secret display.
let secretDisplay =
    document.querySelector(
        '#secretKey, #manualSecret, [data-totp-secret]');


// =========================================================
// GET SETUP TOKEN
// =========================================================

const setupToken =
    sessionStorage.getItem(
        TOTP_SETUP_TOKEN_KEY);


// Setup token missing-na direct access / expired frontend flow.
if (!setupToken ||
    setupToken.trim() === '') {

    showMessage(
        'Authenticator setup session is missing. Please register again.');

    disableForm();


    // User-ku message kaatta small delay-ku dependency
    // create panna vendaam.
    //
    // Login/Register link HTML-la irundhaal use pannalaam.
}
else {
    initializeTotpSetup();
}


// =========================================================
// INITIALIZE TOTP SETUP
// =========================================================
//
// Function:
// First cached response check.
//
// Cache irundha:
// API repeat pannaama QR render.
//
// Cache illa:
// setup API call panni response cache + render.
async function initializeTotpSetup() {
    // =====================================================
    // TRY CACHED RESPONSE
    // =====================================================

    const cached =
        readCachedSetup();


    if (cached) {
        renderSetup(
            cached);

        return;
    }


    try {
        // =================================================
        // CALL SETUP API
        // =================================================

        const result =
            await authApi
                .setupTotp(
                    {
                        setupToken:
                            setupToken.trim()
                    });


        // =================================================
        // RESPONSE VALIDATION
        // =================================================

        if (
            !result
            ||
            typeof result.secretKey !==
            'string'
            ||
            result.secretKey.trim() === ''
            ||
            typeof result.qrCodeDataUrl !==
            'string'
            ||
            result.qrCodeDataUrl.trim() === ''
        ) {
            throw new Error(
                'Authenticator setup information could not be generated.');
        }


        // =================================================
        // CACHE TEMPORARILY
        // =================================================
        //
        // This contains the manual TOTP secret.
        //
        // So sessionStorage only.
        //
        // Setup success piragu immediately remove pannuvom.
        sessionStorage.setItem(
            TOTP_SETUP_RESPONSE_KEY,
            JSON.stringify(
                {
                    secretKey:
                        result.secretKey,

                    otpAuthUri:
                        result.otpAuthUri || '',

                    qrCodeDataUrl:
                        result.qrCodeDataUrl
                }));


        renderSetup(
            result);
    }
    catch (error) {
        showMessage(
            error.message ||
            'Authenticator setup could not be loaded.');

        disableForm();
    }
}


// =========================================================
// VERIFY AUTHENTICATOR CODE
// =========================================================

if (form) {
    form.addEventListener(
        'submit',
        async event => {
            event.preventDefault();


            showMessage(
                '');


            const token =
                sessionStorage.getItem(
                    TOTP_SETUP_TOKEN_KEY);


            if (!token) {
                showMessage(
                    'Authenticator setup session has expired.');

                return;
            }


            const code =
                codeInput?.value
                    ?.trim()
                || '';


            // =============================================
            // CODE VALIDATION
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
                // VERIFY SETUP
                // =========================================

                await authApi
                    .verifyTotpSetup(
                        {
                            setupToken:
                                token.trim(),

                            code
                        });


                // =========================================
                // CLEAR SENSITIVE TEMPORARY DATA
                // =========================================
                //
                // Setup token no longer required.
                //
                // Cached secret/QR must also be removed.
                sessionStorage.removeItem(
                    TOTP_SETUP_TOKEN_KEY);


                sessionStorage.removeItem(
                    TOTP_SETUP_RESPONSE_KEY);


                sessionStorage.removeItem(
                    REGISTRATION_EMAIL_KEY);


                if (codeInput) {
                    codeInput.value =
                        '';
                }


                // =========================================
                // GO TO LOGIN
                // =========================================

                window.location.href =
                    '/html/login.html';
            }
            catch (error) {
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
// RENDER SETUP INFORMATION
// =========================================================
//
// Function:
// QR image + manual secret user-ku show pannum.
//
// Existing HTML elements irundha use pannum.
//
// Illa-na simple safe elements dynamically create pannum.
function renderSetup(
    result) {

    // =====================================================
    // QR IMAGE
    // =====================================================

    if (!qrImage) {
        qrImage =
            document.createElement(
                'img');


        qrImage.id =
            'qrCode';


        qrImage.alt =
            'Authenticator QR Code';


        qrImage.style.maxWidth =
            '220px';


        qrImage.style.display =
            'block';


        qrImage.style.margin =
            '16px auto';


        if (form) {
            form.parentElement
                ?.insertBefore(
                    qrImage,
                    form);
        }
    }


    if (qrImage) {
        qrImage.src =
            result.qrCodeDataUrl;

        qrImage.alt =
            'Scan this QR code using your authenticator app';
    }


    // =====================================================
    // MANUAL SECRET
    // =====================================================

    if (!secretDisplay) {
        const wrapper =
            document.createElement(
                'p');


        wrapper.textContent =
            'Manual setup key: ';


        secretDisplay =
            document.createElement(
                'code');


        secretDisplay.id =
            'secretKey';


        wrapper.appendChild(
            secretDisplay);


        if (form) {
            form.parentElement
                ?.insertBefore(
                    wrapper,
                    form);
        }
    }


    if (secretDisplay) {
        // textContent only.
        secretDisplay.textContent =
            result.secretKey;
    }
}


// =========================================================
// READ CACHED SETUP
// =========================================================

function readCachedSetup() {
    const value =
        sessionStorage.getItem(
            TOTP_SETUP_RESPONSE_KEY);


    if (!value) {
        return null;
    }


    try {
        const parsed =
            JSON.parse(
                value);


        if (
            typeof parsed.secretKey !==
            'string'
            ||
            parsed.secretKey.trim() === ''
            ||
            typeof parsed.qrCodeDataUrl !==
            'string'
            ||
            parsed.qrCodeDataUrl.trim() === ''
        ) {
            sessionStorage.removeItem(
                TOTP_SETUP_RESPONSE_KEY);

            return null;
        }


        return parsed;
    }
    catch {
        sessionStorage.removeItem(
            TOTP_SETUP_RESPONSE_KEY);

        return null;
    }
}


// =========================================================
// DISABLE FORM
// =========================================================

function disableForm() {
    if (!form) {
        return;
    }


    const elements =
        form.querySelectorAll(
            'input, button, select, textarea');


    elements.forEach(
        element => {
            element.disabled =
                true;
        });
}


// =========================================================
// MESSAGE
// =========================================================

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
import {
    requireAuth
} from '../auth/authGuard.js';

import {
    credentialApi
} from '../api/credentialApi.js';


// =========================================================
// CREDENTIAL PAGE
// =========================================================
//
// FEATURES:
//
// ✅ Create
// ✅ Masked List
// ✅ Reveal specific credential
// ✅ Edit
// ✅ Delete
//
// SECURITY:
//
// Default list:
//      Password masked.
//
// Reveal:
//      GET one specific owner record.
//
// Sensitive data:
//
// ❌ console.log panna maatom
// ❌ localStorage save panna maatom
// ❌ sessionStorage save panna maatom
//
// Password input operation mudinja
// immediately clear pannuvom.
//
// =========================================================


// =========================================================
// AUTH CHECK
// =========================================================

const authenticated =
    requireAuth();


// =========================================================
// PAGE ELEMENTS
// =========================================================

const form =
    document.getElementById(
        'credentialForm')
    ||
    document.querySelector(
        'form');


const formTitle =
    document.getElementById(
        'formTitle');


const titleInput =
    form?.querySelector(
        '[name="title"]');


const usernameInput =
    form?.querySelector(
        '[name="username"]');


const passwordInput =
    form?.querySelector(
        '[name="password"]');


const websiteInput =
    form?.querySelector(
        '[name="website"]');


const notesInput =
    form?.querySelector(
        '[name="notes"]');


const saveButton =
    document.getElementById(
        'saveButton')
    ||
    form?.querySelector(
        'button[type="submit"]');


const cancelEditButton =
    document.getElementById(
        'cancelEditButton');


const message =
    document.getElementById(
        'message');


const list =
    document.getElementById(
        'list');


const detailsSection =
    document.getElementById(
        'credentialDetails');


const detailsContent =
    document.getElementById(
        'credentialDetailsContent');


const hideCredentialButton =
    document.getElementById(
        'hideCredentialButton');


// =========================================================
// EDIT STATE
// =========================================================
//
// null:
// Create mode.
//
// number:
// Existing credential edit mode.
//
// IMPORTANT:
// Decrypted credential object global-aa store panna maatom.
//
// Edit fields DOM-la temporary-a mattum irukkum.
let editingCredentialId =
    null;


// =========================================================
// START PAGE
// =========================================================

if (authenticated) {
    initializeCredentialPage();
}


// =========================================================
// INITIALIZE
// =========================================================

async function initializeCredentialPage() {
    if (!form ||
        !titleInput ||
        !usernameInput ||
        !passwordInput ||
        !list) {

        showMessage(
            'Credential page could not be loaded.');

        return;
    }


    await loadCredentials();
}


// =========================================================
// LOAD CREDENTIALS
// =========================================================
//
// API:
// GET /api/credentials
//
// IMPORTANT:
//
// Backend password masked response mattum.
//
// Example:
// ********
async function loadCredentials() {
    showMessage(
        'Loading credentials...');


    try {
        const credentials =
            await credentialApi
                .all();


        const rows =
            Array.isArray(
                credentials)
                ?
                credentials
                :
                [];


        renderCredentials(
            rows);


        showMessage(
            '');
    }
    catch (error) {
        list.replaceChildren();


        showMessage(
            error.message ||
            'Credentials could not be loaded.');
    }
}


// =========================================================
// CREATE / UPDATE SUBMIT
// =========================================================

if (form) {
    form.addEventListener(
        'submit',
        async event => {
            event.preventDefault();


            if (!requireAuth()) {
                return;
            }


            // =============================================
            // READ VALUES
            // =============================================

            const title =
                titleInput.value
                    .trim();


            const username =
                usernameInput.value
                    .trim();


            const password =
                passwordInput.value;


            const website =
                websiteInput?.value
                    ?.trim()
                || '';


            const notes =
                notesInput?.value
                    ?.trim()
                || '';


            // =============================================
            // VALIDATION
            // =============================================

            if (!title) {
                showMessage(
                    'Credential title is required.');

                titleInput.focus();

                return;
            }


            if (!username) {
                showMessage(
                    'Username is required.');

                usernameInput.focus();

                return;
            }


            if (!password) {
                showMessage(
                    'Password is required.');

                passwordInput.focus();

                return;
            }


            // Website optional.
            //
            // User enter pannina valid HTTP/HTTPS URL-a
            // irukkanum.
            if (website &&
                !isSafeWebsiteUrl(
                    website)) {

                showMessage(
                    'Website must be a valid http:// or https:// address.');

                websiteInput?.focus();

                return;
            }


            // =============================================
            // REQUEST DTO
            // =============================================
            //
            // IMPORTANT:
            // Credential payload console-la log panna koodathu.

            const data =
            {
                title,
                username,
                password,
                website:
                    website || null,

                notes:
                    notes || null
            };


            setButtonDisabled(
                saveButton,
                true);


            try {
                // =========================================
                // CREATE MODE
                // =========================================

                if (editingCredentialId ===
                    null) {

                    showMessage(
                        'Saving credential securely...');


                    await credentialApi
                        .create(
                            data);


                    showMessage(
                        'Credential saved successfully.');
                }

                // =========================================
                // EDIT MODE
                // =========================================

                else {
                    showMessage(
                        'Updating credential securely...');


                    await credentialApi
                        .update(
                            editingCredentialId,
                            data);


                    showMessage(
                        'Credential updated successfully.');
                }


                // =========================================
                // CLEAR SENSITIVE FORM VALUES
                // =========================================

                resetForm();


                hideCredentialDetails();


                // =========================================
                // REFRESH MASKED LIST
                // =========================================

                await loadCredentials();
            }
            catch (error) {
                showMessage(
                    error.message ||
                    'Credential could not be saved.');
            }
            finally {
                setButtonDisabled(
                    saveButton,
                    false);
            }
        });
}


// =========================================================
// RENDER CREDENTIAL LIST
// =========================================================
//
// SECURITY:
//
// Default list-la decrypted password never render
// panna maatom.
//
// Backend response password field already masked.
//
// Still frontend defense-in-depth:
// password value trust pannitu display pannaama
// fixed "********" show pannuvom.
function renderCredentials(
    credentials) {

    list.replaceChildren();


    // =====================================================
    // EMPTY STATE
    // =====================================================

    if (credentials.length === 0) {
        const empty =
            document.createElement(
                'div');


        empty.className =
            'card vault-item';


        empty.textContent =
            'No credentials saved yet.';


        list.appendChild(
            empty);


        return;
    }


    // =====================================================
    // CREDENTIAL CARDS
    // =====================================================

    credentials.forEach(
        credential => {
            list.appendChild(
                createCredentialCard(
                    credential));
        });
}


// =========================================================
// CREATE CREDENTIAL CARD
// =========================================================

function createCredentialCard(
    credential) {

    const card =
        document.createElement(
            'div');


    card.className =
        'card vault-item';


    // =====================================================
    // INFORMATION
    // =====================================================

    const information =
        document.createElement(
            'div');


    const title =
        document.createElement(
            'strong');


    title.textContent =
        credential?.title ||
        'Untitled credential';


    const username =
        document.createElement(
            'div');


    username.textContent =
        credential?.username ||
        '-';


    const password =
        document.createElement(
            'div');


    // IMPORTANT:
    // Default list always fixed masked value.
    password.textContent =
        'Password: ********';


    information.appendChild(
        title);


    information.appendChild(
        username);


    information.appendChild(
        password);


    // =====================================================
    // SAFE WEBSITE
    // =====================================================

    if (credential?.website) {
        const website =
            createSafeWebsiteLink(
                credential.website);


        information.appendChild(
            website);
    }


    // =====================================================
    // ACTION BUTTONS
    // =====================================================

    const actions =
        document.createElement(
            'div');


    actions.className =
        'vault-actions';


    // Reveal
    const revealButton =
        createButton(
            'Reveal');


    revealButton.addEventListener(
        'click',
        async () => {
            await revealCredential(
                credential,
                revealButton);
        });


    // Edit
    const editButton =
        createButton(
            'Edit');


    editButton.addEventListener(
        'click',
        async () => {
            await editCredential(
                credential,
                editButton);
        });


    // Delete
    const deleteButton =
        createButton(
            'Delete');


    deleteButton.addEventListener(
        'click',
        async () => {
            await deleteCredential(
                credential,
                deleteButton);
        });


    actions.appendChild(
        revealButton);


    actions.appendChild(
        editButton);


    actions.appendChild(
        deleteButton);


    card.appendChild(
        information);


    card.appendChild(
        actions);


    return card;
}


// =========================================================
// REVEAL CREDENTIAL
// =========================================================
//
// API:
// GET /api/credentials/{id}
//
// Flow:
//
// User explicitly clicks Reveal
//       ↓
// Backend owner authorization
//       ↓
// AES decrypt
//       ↓
// Return specific credential
//       ↓
// Show details
//
// Default list remains masked.
async function revealCredential(
    credential,
    button) {

    const id =
        getCredentialId(
            credential);


    if (!id) {
        showMessage(
            'Invalid credential.');

        return;
    }


    setButtonDisabled(
        button,
        true);


    showMessage(
        'Revealing credential...');


    try {
        const revealed =
            await credentialApi
                .get(
                    id);


        renderCredentialDetails(
            revealed);


        showMessage(
            '');
    }
    catch (error) {
        hideCredentialDetails();


        showMessage(
            error.message ||
            'Credential could not be revealed.');
    }
    finally {
        setButtonDisabled(
            button,
            false);
    }
}


// =========================================================
// RENDER REVEALED DETAILS
// =========================================================
//
// IMPORTANT:
// Decrypted password only explicit reveal area-la
// temporary-a display aagum.
function renderCredentialDetails(
    credential) {

    if (!detailsSection ||
        !detailsContent) {

        return;
    }


    detailsContent.replaceChildren();


    appendDetail(
        'Title',
        credential?.title ||
        '-');


    appendDetail(
        'Username',
        credential?.username ||
        '-');


    appendDetail(
        'Password',
        credential?.password ||
        '-');


    // Website safe link.
    const websiteRow =
        document.createElement(
            'p');


    const websiteLabel =
        document.createElement(
            'strong');


    websiteLabel.textContent =
        'Website: ';


    websiteRow.appendChild(
        websiteLabel);


    if (credential?.website &&
        isSafeWebsiteUrl(
            credential.website)) {

        websiteRow.appendChild(
            createSafeWebsiteLink(
                credential.website));
    }
    else {
        const websiteValue =
            document.createElement(
                'span');


        websiteValue.textContent =
            credential?.website ||
            '-';


        websiteRow.appendChild(
            websiteValue);
    }


    detailsContent.appendChild(
        websiteRow);


    appendDetail(
        'Notes',
        credential?.notes ||
        '-');


    detailsSection.hidden =
        false;
}


// =========================================================
// EDIT CREDENTIAL
// =========================================================
//
// IMPORTANT:
//
// Update DTO requires:
//
// Title
// Username
// Password
// Website
// Notes
//
// So first GET single credential call panni
// owner-authorized current values retrieve pannuvom.
async function editCredential(
    credential,
    button) {

    const id =
        getCredentialId(
            credential);


    if (!id) {
        showMessage(
            'Invalid credential.');

        return;
    }


    setButtonDisabled(
        button,
        true);


    showMessage(
        'Loading credential for editing...');


    try {
        const revealed =
            await credentialApi
                .get(
                    id);


        // =============================================
        // SET EDIT MODE
        // =============================================

        editingCredentialId =
            id;


        // =============================================
        // FILL FORM
        // =============================================

        titleInput.value =
            revealed?.title ||
            '';


        usernameInput.value =
            revealed?.username ||
            '';


        // Decrypted password temporary form value.
        passwordInput.value =
            revealed?.password ||
            '';


        if (websiteInput) {
            websiteInput.value =
                revealed?.website ||
                '';
        }


        if (notesInput) {
            notesInput.value =
                revealed?.notes ||
                '';
        }


        // =============================================
        // CHANGE FORM UI
        // =============================================

        if (formTitle) {
            formTitle.textContent =
                'Edit Credential';
        }


        if (saveButton) {
            saveButton.textContent =
                'Update Credential';
        }


        if (cancelEditButton) {
            cancelEditButton.hidden =
                false;
        }


        // Hide separately revealed details.
        hideCredentialDetails();


        showMessage(
            'Edit the credential and click Update Credential.');


        titleInput.focus();
    }
    catch (error) {
        resetForm();


        showMessage(
            error.message ||
            'Credential could not be loaded for editing.');
    }
    finally {
        setButtonDisabled(
            button,
            false);
    }
}


// =========================================================
// CANCEL EDIT
// =========================================================

if (cancelEditButton) {
    cancelEditButton.addEventListener(
        'click',
        () => {
            resetForm();

            showMessage(
                'Edit cancelled.');
        });
}


// =========================================================
// DELETE CREDENTIAL
// =========================================================

async function deleteCredential(
    credential,
    button) {

    const id =
        getCredentialId(
            credential);


    if (!id) {
        showMessage(
            'Invalid credential.');

        return;
    }


    const title =
        credential?.title ||
        'this credential';


    const confirmed =
        window.confirm(
            `Delete "${title}" from your vault?`);


    if (!confirmed) {
        return;
    }


    setButtonDisabled(
        button,
        true);


    showMessage(
        'Deleting credential...');


    try {
        await credentialApi
            .remove(
                id);


        // Deleting currently edited item?
        if (editingCredentialId ===
            id) {

            resetForm();
        }


        hideCredentialDetails();


        await loadCredentials();


        showMessage(
            'Credential deleted successfully.');
    }
    catch (error) {
        showMessage(
            error.message ||
            'Credential could not be deleted.');
    }
    finally {
        setButtonDisabled(
            button,
            false);
    }
}


// =========================================================
// HIDE REVEALED DETAILS
// =========================================================

if (hideCredentialButton) {
    hideCredentialButton.addEventListener(
        'click',
        () => {
            hideCredentialDetails();
        });
}


function hideCredentialDetails() {
    if (detailsContent) {
        detailsContent
            .replaceChildren();
    }


    if (detailsSection) {
        detailsSection.hidden =
            true;
    }
}


// =========================================================
// RESET FORM
// =========================================================
//
// Security:
//
// Password and notes DOM values clear pannum.
//
// No sensitive values browser storage-la save aagaathu.
function resetForm() {
    editingCredentialId =
        null;


    if (form) {
        form.reset();
    }


    // Extra explicit sensitive clear.
    if (passwordInput) {
        passwordInput.value =
            '';
    }


    if (notesInput) {
        notesInput.value =
            '';
    }


    if (formTitle) {
        formTitle.textContent =
            'Add Credential';
    }


    if (saveButton) {
        saveButton.textContent =
            'Save Credential';
    }


    if (cancelEditButton) {
        cancelEditButton.hidden =
            true;
    }
}


// =========================================================
// VALIDATE CREDENTIAL ID
// =========================================================

function getCredentialId(
    credential) {

    const id =
        Number(
            credential?.id);


    if (!Number.isInteger(
        id)
        ||
        id <= 0) {

        return null;
    }


    return id;
}


// =========================================================
// DETAILS HELPER
// =========================================================

function appendDetail(
    label,
    value) {

    if (!detailsContent) {
        return;
    }


    const row =
        document.createElement(
            'p');


    const labelElement =
        document.createElement(
            'strong');


    labelElement.textContent =
        `${label}: `;


    const valueElement =
        document.createElement(
            'span');


    valueElement.textContent =
        String(
            value);


    row.appendChild(
        labelElement);


    row.appendChild(
        valueElement);


    detailsContent.appendChild(
        row);
}


// =========================================================
// SAFE WEBSITE LINK
// =========================================================
//
// Security:
//
// User supplied website direct javascript: URL
// aaga execute aaga koodathu.
//
// Only:
// http
// https
//
// allow pannuvom.
function createSafeWebsiteLink(
    website) {

    const container =
        document.createElement(
            'div');


    if (!isSafeWebsiteUrl(
        website)) {

        container.textContent =
            website;

        return container;
    }


    const link =
        document.createElement(
            'a');


    link.href =
        website;


    link.textContent =
        website;


    link.target =
        '_blank';


    link.rel =
        'noopener noreferrer';


    container.appendChild(
        link);


    return container;
}


// =========================================================
// WEBSITE VALIDATION
// =========================================================

function isSafeWebsiteUrl(
    value) {

    if (!value ||
        typeof value !==
        'string') {

        return false;
    }


    try {
        const url =
            new URL(
                value);


        return url.protocol ===
            'https:'
            ||
            url.protocol ===
            'http:';
    }
    catch {
        return false;
    }
}


// =========================================================
// CREATE BUTTON
// =========================================================

function createButton(
    text) {

    const button =
        document.createElement(
            'button');


    button.type =
        'button';


    button.textContent =
        text;


    return button;
}


// =========================================================
// BUTTON DISABLE HELPER
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
// MESSAGE
// =========================================================
//
// Security:
// innerHTML use panna maatom.
// textContent only.
function showMessage(
    text) {

    if (!message) {
        return;
    }


    message.textContent =
        text;
}
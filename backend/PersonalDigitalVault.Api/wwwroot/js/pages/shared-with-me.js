import {
    requireAuth
} from '../auth/authGuard.js';

import {
    documentShareApi
} from '../api/documentShareApi.js';

import {
    documentApi
} from '../api/documentApi.js';

import {
    fileSize
} from '../utils/formatters.js';


// =========================================================
// SHARED WITH ME PAGE
// =========================================================
//
// FEATURES:
//
// ✅ Current recipient accepted shares
// ✅ File name
// ✅ Owner name/email
// ✅ File size
// ✅ Accepted date
// ✅ Secure download
//
// IMPORTANT:
//
// Shared recipient:
// ❌ document owner aaga maattar
// ❌ rename panna mudiyadhu
// ❌ delete panna mudiyadhu
// ❌ owner metadata endpoint use panna mudiyadhu
//
// Download mattum existing secure endpoint:
//
// GET /api/documents/{id}/download
//
// =========================================================


// =========================================================
// STORAGE
// =========================================================

const SHARE_SUCCESS_KEY =
    'pdv_share_success';


// =========================================================
// AUTH
// =========================================================

const authenticated =
    requireAuth();


// =========================================================
// ELEMENTS
// =========================================================

const list =
    document.getElementById(
        'list');


const message =
    document.getElementById(
        'message');


// =========================================================
// START
// =========================================================

if (authenticated) {

    showPreviousSuccessMessage();

    loadSharedDocuments();
}


// =========================================================
// SUCCESS FROM INVITATION PAGE
// =========================================================

function showPreviousSuccessMessage() {

    const success =
        sessionStorage.getItem(
            SHARE_SUCCESS_KEY);


    if (!success) {
        return;
    }


    sessionStorage.removeItem(
        SHARE_SUCCESS_KEY);


    showMessage(
        success);
}


// =========================================================
// LOAD SHARED DOCUMENTS
// =========================================================

async function loadSharedDocuments() {

    if (!list) {
        return;
    }


    showMessage(
        'Loading shared documents...');


    try {
        const result =
            await documentShareApi
                .sharedWithMe();


        const rows =
            Array.isArray(
                result)
                ?
                result
                :
                [];


        renderSharedDocuments(
            rows);


        // Invitation success message illa-na
        // loading message clear pannuvom.
        if (!sessionStorage.getItem(
            SHARE_SUCCESS_KEY)) {

            if (rows.length > 0) {
                showMessage(
                    '');
            }
        }
    }
    catch (error) {

        list.replaceChildren();


        showMessage(
            error.message ||
            'Shared documents could not be loaded.');
    }
}


// =========================================================
// RENDER
// =========================================================

function renderSharedDocuments(
    rows) {

    list.replaceChildren();


    if (rows.length === 0) {

        const empty =
            document.createElement(
                'div');


        empty.className =
            'card vault-item';


        empty.textContent =
            'No accepted documents have been shared with you.';


        list.appendChild(
            empty);


        return;
    }


    rows.forEach(
        item => {
            list.appendChild(
                createSharedDocumentCard(
                    item));
        });
}


// =========================================================
// CREATE SHARED DOCUMENT CARD
// =========================================================

function createSharedDocumentCard(
    item) {

    const card =
        document.createElement(
            'div');


    card.className =
        'card vault-item';


    const information =
        document.createElement(
            'div');


    // File name
    const fileName =
        document.createElement(
            'strong');


    fileName.textContent =
        item?.fileName ||
        'Shared document';


    // Owner
    const owner =
        document.createElement(
            'div');


    owner.textContent =
        `Shared by: ${item?.sharedBy || '-'} (${item?.sharedByEmail || '-'})`;


    // Size
    const size =
        document.createElement(
            'div');


    size.textContent =
        `Size: ${fileSize(
            Number(item?.fileSize || 0)
        )}`;


    // Accepted
    const accepted =
        document.createElement(
            'div');


    accepted.textContent =
        `Accepted: ${formatDate(
            item?.acceptedAt
        )}`;


    information.appendChild(
        fileName);


    information.appendChild(
        owner);


    information.appendChild(
        size);


    information.appendChild(
        accepted);


    // =====================================================
    // DOWNLOAD
    // =====================================================

    const actions =
        document.createElement(
            'div');


    actions.className =
        'vault-actions';


    const downloadButton =
        document.createElement(
            'button');


    downloadButton.type =
        'button';


    downloadButton.textContent =
        'Download';


    downloadButton.addEventListener(
        'click',
        async () => {
            await downloadSharedDocument(
                item,
                downloadButton);
        });


    actions.appendChild(
        downloadButton);


    card.appendChild(
        information);


    card.appendChild(
        actions);


    return card;
}


// =========================================================
// DOWNLOAD SHARED DOCUMENT
// =========================================================
//
// Backend decides:
//
// owner
// OR
// accepted valid recipient.
//
// Frontend cannot bypass backend authorization.
async function downloadSharedDocument(
    item,
    button) {

    const documentId =
        Number(
            item?.documentId);


    if (!Number.isInteger(documentId) ||
        documentId <= 0) {

        showMessage(
            'Invalid shared document.');

        return;
    }


    setButtonDisabled(
        button,
        true);


    showMessage(
        'Preparing secure download...');


    try {
        await documentApi
            .download(
                documentId,
                item?.fileName ||
                'document');


        showMessage(
            'Shared document downloaded successfully.');
    }
    catch (error) {

        showMessage(
            error.message ||
            'The shared document could not be downloaded.');


        // Owner revoke pannirundha
        // next refresh-la item disappear aagum.
        await loadSharedDocuments();
    }
    finally {

        setButtonDisabled(
            button,
            false);
    }
}


// =========================================================
// DATE
// =========================================================

function formatDate(
    value) {

    if (!value) {
        return '-';
    }


    const date =
        new Date(
            value);


    if (Number.isNaN(
        date.getTime())) {

        return '-';
    }


    return date.toLocaleString();
}


// =========================================================
// BUTTON
// =========================================================

function setButtonDisabled(
    button,
    disabled) {

    if (button) {
        button.disabled =
            disabled;
    }
}


// =========================================================
// MESSAGE
// =========================================================

function showMessage(
    text) {

    if (message) {
        message.textContent =
            text;
    }
}
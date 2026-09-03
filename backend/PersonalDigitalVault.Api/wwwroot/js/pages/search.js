import {
    requireAuth
} from '../auth/authGuard.js';

import {
    searchApi
} from '../api/searchApi.js';


// =========================================================
// SEARCH PAGE
// =========================================================
//
// FEATURES:
//
// ✅ Search own vault
// ✅ Folder name search
// ✅ Document filename search
// ✅ Credential title search
// ✅ Empty keyword handling
// ✅ Empty results handling
// ✅ Client-side type filter
// ✅ Safe navigation
// ✅ Safe DOM rendering
//
// SECURITY:
//
// Search result-la whitelist:
//
// type
// id
// title
//
// mattum render pannuvom.
//
// Even unexpected backend property vandhaal:
//
// password
// username
// notes
// storagePath
// fileHash
//
// render panna maatom.
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
        'searchForm')
    ||
    document.querySelector(
        'form');


const keywordInput =
    form?.querySelector(
        '[name="keyword"]');


const searchButton =
    document.getElementById(
        'searchButton')
    ||
    form?.querySelector(
        'button[type="submit"], button');


const typeFilter =
    document.getElementById(
        'typeFilter');


const list =
    document.getElementById(
        'list');


const message =
    document.getElementById(
        'message');


const resultCount =
    document.getElementById(
        'resultCount');


// =========================================================
// LAST SEARCH RESULTS
// =========================================================
//
// Type dropdown change pannumbodhu
// API again call pannaama existing safe results
// filter panna.
//
// Sensitive data inga store panna maatom.
//
// SearchResultDto metadata mattum.
let lastResults =
    [];


// =========================================================
// PAGE START
// =========================================================

if (authenticated) {
    initializeSearchPage();
}


// =========================================================
// INITIALIZE
// =========================================================

function initializeSearchPage() {
    if (!form ||
        !keywordInput ||
        !list) {

        showMessage(
            'Search page could not be loaded.');

        return;
    }


    showEmptyStartState();
}


// =========================================================
// SEARCH SUBMIT
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
            // READ KEYWORD
            // =============================================

            const keyword =
                keywordInput.value
                    .trim();


            // =============================================
            // EMPTY KEYWORD
            // =============================================

            if (!keyword) {
                lastResults =
                    [];


                renderResults(
                    []);


                showMessage(
                    'Enter a search keyword.');


                keywordInput.focus();

                return;
            }


            // =============================================
            // DISABLE SEARCH BUTTON
            // =============================================

            setButtonDisabled(
                searchButton,
                true);


            showMessage(
                'Searching your vault...');


            setResultCount(
                '');


            try {
                // =========================================
                // GET /api/search?keyword=
                // =========================================

                const response =
                    await searchApi
                        .search(
                            keyword);


                // =========================================
                // SAFE ARRAY
                // =========================================

                const rows =
                    Array.isArray(
                        response)
                        ?
                        response
                        :
                        [];


                // =========================================
                // SANITIZE RESULT SHAPE
                // =========================================
                //
                // IMPORTANT:
                //
                // Backend object full-a copy panna maatom.
                //
                // type/id/title mattum whitelist pannuvom.
                lastResults =
                    rows
                        .map(
                            sanitizeResult)
                        .filter(
                            result =>
                                result !== null);


                // =========================================
                // APPLY TYPE FILTER
                // =========================================

                renderFilteredResults();


                if (lastResults.length === 0) {
                    showMessage(
                        `No results found for "${keyword}".`);
                }
                else {
                    showMessage(
                        '');
                }
            }
            catch (error) {
                lastResults =
                    [];


                renderResults(
                    []);


                setResultCount(
                    '');


                showMessage(
                    error.message ||
                    'Search could not be completed.');
            }
            finally {
                setButtonDisabled(
                    searchButton,
                    false);
            }
        });
}


// =========================================================
// TYPE FILTER
// =========================================================
//
// Backend endpoint same.
//
// API again call panna thevai illa.
//
// Existing result metadata mattum filter pannuvom.
if (typeFilter) {
    typeFilter.addEventListener(
        'change',
        () => {
            renderFilteredResults();
        });
}


// =========================================================
// SANITIZE SEARCH RESULT
// =========================================================
//
// Input:
// Backend SearchResultDto.
//
// Output:
//
// {
//     type,
//     id,
//     title
// }
//
// or null.
//
// SECURITY:
//
// Unexpected secret fields drop aagum.
function sanitizeResult(
    result) {

    if (!result ||
        typeof result !== 'object') {

        return null;
    }


    const id =
        Number(
            result.id);


    const type =
        String(
            result.type ?? '')
            .trim();


    const title =
        String(
            result.title ?? '')
            .trim();


    if (!Number.isInteger(
        id)
        ||
        id <= 0
        ||
        !type
        ||
        !title) {

        return null;
    }


    return {
        id,
        type,
        title
    };
}


// =========================================================
// FILTER RESULTS
// =========================================================

function renderFilteredResults() {
    const selectedType =
        String(
            typeFilter?.value ||
            'all')
            .toLowerCase();


    let filtered =
        lastResults;


    if (selectedType !==
        'all') {

        filtered =
            lastResults.filter(
                result =>
                    normalizeType(
                        result.type)
                    ===
                    selectedType);
    }


    renderResults(
        filtered);


    if (lastResults.length > 0) {
        setResultCount(
            `${filtered.length} result(s) shown`);
    }
}


// =========================================================
// RENDER RESULTS
// =========================================================
//
// Security:
// innerHTML use panna maatom.
//
// User-controlled titles
// textContent use panni render pannuvom.
function renderResults(
    results) {

    list.replaceChildren();


    // =====================================================
    // EMPTY RESULTS
    // =====================================================

    if (results.length === 0) {
        const empty =
            document.createElement(
                'div');


        empty.className =
            'card vault-item';


        empty.textContent =
            'No matching vault items.';


        list.appendChild(
            empty);


        return;
    }


    // =====================================================
    // RESULT CARDS
    // =====================================================

    results.forEach(
        result => {
            list.appendChild(
                createResultCard(
                    result));
        });
}


// =========================================================
// CREATE RESULT CARD
// =========================================================

function createResultCard(
    result) {

    const card =
        document.createElement(
            'div');


    card.className =
        'card vault-item';


    // =====================================================
    // INFO
    // =====================================================

    const information =
        document.createElement(
            'div');


    const title =
        document.createElement(
            'strong');


    title.textContent =
        result.title;


    const type =
        document.createElement(
            'div');


    type.textContent =
        `Type: ${getDisplayType(
            result.type
        )}`;


    information.appendChild(
        title);


    information.appendChild(
        type);


    // =====================================================
    // ACTION
    // =====================================================

    const actions =
        document.createElement(
            'div');


    actions.className =
        'vault-actions';


    const openButton =
        document.createElement(
            'button');


    openButton.type =
        'button';


    openButton.textContent =
        getOpenButtonText(
            result.type);


    const destination =
        getDestination(
            result.type);


    if (destination) {
        openButton.addEventListener(
            'click',
            () => {
                window.location.href =
                    destination;
            });
    }
    else {
        openButton.disabled =
            true;
    }


    actions.appendChild(
        openButton);


    card.appendChild(
        information);


    card.appendChild(
        actions);


    return card;
}


// =========================================================
// NORMALIZE RESULT TYPE
// =========================================================
//
// Backend expected:
//
// Folder
// Document
// Credential
//
// Case differences safely handle pannuvom.
function normalizeType(
    type) {

    const value =
        String(
            type ?? '')
            .trim()
            .toLowerCase();


    if (value === 'folder') {
        return 'folder';
    }


    if (value === 'document') {
        return 'document';
    }


    if (value === 'credential') {
        return 'credential';
    }


    return 'unknown';
}


// =========================================================
// DISPLAY TYPE
// =========================================================

function getDisplayType(
    type) {

    switch (normalizeType(
        type)) {

        case 'folder':
            return 'Folder';

        case 'document':
            return 'Document';

        case 'credential':
            return 'Credential';

        default:
            return 'Vault Item';
    }
}


// =========================================================
// RESULT NAVIGATION
// =========================================================
//
// Current Folder/Document/Credential pages
// per-record deep-link/highlight support pannaathu.
//
// So fake ?id route create pannaama
// correct module page-ku mattum navigate pannuvom.
//
// Later needed-na page-level focus support
// separately add pannalaam.
function getDestination(
    type) {

    switch (normalizeType(
        type)) {

        case 'folder':
            return '/html/folders.html';

        case 'document':
            return '/html/documents.html';

        case 'credential':
            return '/html/credentials.html';

        default:
            return null;
    }
}


// =========================================================
// OPEN BUTTON TEXT
// =========================================================

function getOpenButtonText(
    type) {

    switch (normalizeType(
        type)) {

        case 'folder':
            return 'Open Folders';

        case 'document':
            return 'Open Documents';

        case 'credential':
            return 'Open Credentials';

        default:
            return 'Unavailable';
    }
}


// =========================================================
// EMPTY START STATE
// =========================================================

function showEmptyStartState() {
    list.replaceChildren();


    const card =
        document.createElement(
            'div');


    card.className =
        'card vault-item';


    card.textContent =
        'Enter a keyword to search your folders, documents and credential titles.';


    list.appendChild(
        card);


    setResultCount(
        '');
}


// =========================================================
// RESULT COUNT
// =========================================================

function setResultCount(
    text) {

    if (!resultCount) {
        return;
    }


    resultCount.textContent =
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


// =========================================================
// MESSAGE
// =========================================================
//
// Security:
// textContent only.
function showMessage(
    text) {

    if (!message) {
        return;
    }


    message.textContent =
        text;
}
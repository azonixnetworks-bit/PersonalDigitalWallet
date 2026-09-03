import {
    requireAuth
} from '../auth/authGuard.js';

import {
    folderApi
} from '../api/folderApi.js';


// =========================================================
// FOLDERS PAGE
// =========================================================
//
// FEATURES:
//
// ✅ List current user's folders
// ✅ Create folder
// ✅ Rename folder
// ✅ Delete folder
// ✅ Reload list without full browser refresh
// ✅ Safe error messages
// ✅ Prevent double click
//
// SECURITY:
//
// Frontend UserId send panna maatom.
//
// Folder Id mattum backend-ku send pannuvom.
//
// Backend:
// JWT current UserId
// +
// folder ownership
//
// verify pannum.
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
        'folderForm')
    ||
    document.querySelector(
        'form');


const nameInput =
    form?.querySelector(
        '[name="name"]');


const createButton =
    document.getElementById(
        'createFolderButton')
    ||
    form?.querySelector(
        'button[type="submit"], button');


const list =
    document.getElementById(
        'list');


const message =
    document.getElementById(
        'message');


// =========================================================
// PAGE START
// =========================================================

if (authenticated) {
    initializeFoldersPage();
}


// =========================================================
// INITIALIZE PAGE
// =========================================================
//
// Function:
// Required HTML elements check panni
// current user folders load pannum.
//
// Input:
// None.
//
// Output:
// Folder list screen-la show aagum.
async function initializeFoldersPage() {
    if (!form ||
        !nameInput ||
        !list) {

        showMessage(
            'Folder page could not be loaded.');

        return;
    }


    await loadFolders();
}


// =========================================================
// LOAD FOLDERS
// =========================================================
//
// Function:
// GET /api/folders call pannum.
//
// Output:
// Current user folders render pannum.
async function loadFolders() {
    showMessage(
        'Loading folders...');


    try {
        const folders =
            await folderApi
                .all();


        // Backend ideally array return pannum.
        //
        // Unexpected response vandhaal empty array use pannuvom.
        const rows =
            Array.isArray(
                folders)
                ?
                folders
                :
                [];


        renderFolders(
            rows);


        showMessage(
            '');
    }
    catch (error) {
        list.replaceChildren();


        showMessage(
            error.message ||
            'Folders could not be loaded.');
    }
}


// =========================================================
// CREATE FOLDER
// =========================================================
//
// Flow:
//
// User enters name
//      ↓
// Validate
//      ↓
// POST /api/folders
//      ↓
// Reset form
//      ↓
// Reload list
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
            // READ NAME
            // =============================================

            const name =
                nameInput?.value
                    ?.trim()
                || '';


            // =============================================
            // VALIDATION
            // =============================================

            if (!name) {
                showMessage(
                    'Folder name is required.');

                nameInput?.focus();

                return;
            }


            // =============================================
            // PREVENT DOUBLE SUBMIT
            // =============================================

            setButtonDisabled(
                createButton,
                true);


            showMessage(
                'Creating folder...');


            try {
                // =========================================
                // POST /api/folders
                // =========================================

                await folderApi
                    .create(
                        {
                            name
                        });


                // =========================================
                // CLEAR FORM
                // =========================================

                form.reset();


                showMessage(
                    'Folder created successfully.');


                // =========================================
                // RELOAD WITHOUT FULL PAGE REFRESH
                // =========================================

                await loadFolders();
            }
            catch (error) {
                showMessage(
                    error.message ||
                    'Folder could not be created.');
            }
            finally {
                setButtonDisabled(
                    createButton,
                    false);
            }
        });
}


// =========================================================
// RENDER FOLDERS
// =========================================================
//
// Function:
// FolderDto list-a safe DOM elements use panni render pannum.
//
// IMPORTANT:
//
// innerHTML-la folder name inject panna maatom.
//
// Reason:
// Folder name user input.
//
// textContent use pannuvom to reduce XSS risk.
function renderFolders(
    folders) {

    if (!list) {
        return;
    }


    // Existing cards clear.
    list.replaceChildren();


    // =====================================================
    // EMPTY STATE
    // =====================================================

    if (folders.length === 0) {
        const emptyCard =
            document.createElement(
                'div');


        emptyCard.className =
            'card vault-item';


        emptyCard.textContent =
            'No folders created yet.';


        list.appendChild(
            emptyCard);


        return;
    }


    // =====================================================
    // CREATE EACH FOLDER CARD
    // =====================================================

    folders.forEach(
        folder => {
            const card =
                createFolderCard(
                    folder);


            list.appendChild(
                card);
        });
}


// =========================================================
// CREATE FOLDER CARD
// =========================================================
//
// Input:
// FolderDto.
//
// Output:
// DOM card.
//
// Card:
//
// Folder Name
// Rename Button
// Delete Button
function createFolderCard(
    folder) {

    // =====================================================
    // CARD
    // =====================================================

    const card =
        document.createElement(
            'div');


    card.className =
        'card vault-item';


    // =====================================================
    // NAME
    // =====================================================

    const folderName =
        document.createElement(
            'span');


    folderName.textContent =
        folder?.name ||
        'Unnamed folder';


    // =====================================================
    // ACTION AREA
    // =====================================================

    const actions =
        document.createElement(
            'div');


    actions.className =
        'vault-actions';


    // =====================================================
    // RENAME BUTTON
    // =====================================================

    const renameButton =
        document.createElement(
            'button');


    renameButton.type =
        'button';


    renameButton.textContent =
        'Rename';


    renameButton.addEventListener(
        'click',
        async () => {
            await renameFolder(
                folder,
                renameButton,
                deleteButton);
        });


    // =====================================================
    // DELETE BUTTON
    // =====================================================

    const deleteButton =
        document.createElement(
            'button');


    deleteButton.type =
        'button';


    deleteButton.textContent =
        'Delete';


    deleteButton.addEventListener(
        'click',
        async () => {
            await deleteFolder(
                folder,
                renameButton,
                deleteButton);
        });


    // =====================================================
    // BUILD CARD
    // =====================================================

    actions.appendChild(
        renameButton);


    actions.appendChild(
        deleteButton);


    card.appendChild(
        folderName);


    card.appendChild(
        actions);


    return card;
}


// =========================================================
// RENAME FOLDER
// =========================================================
//
// Function:
// User-kitta new folder name ketu
// PUT /api/folders/{id} call pannum.
//
// Input:
// FolderDto
// buttons.
//
// Output:
// Updated list.
//
// Security:
//
// Id + name mattum frontend send pannum.
//
// Owner UserId send panna maatom.
async function renameFolder(
    folder,
    renameButton,
    deleteButton) {

    // =====================================================
    // VALID FOLDER ID
    // =====================================================

    const folderId =
        Number(
            folder?.id);


    if (!Number.isInteger(
        folderId)
        ||
        folderId <= 0) {

        showMessage(
            'Invalid folder.');

        return;
    }


    // =====================================================
    // ASK NEW NAME
    // =====================================================

    const currentName =
        folder?.name || '';


    const input =
        window.prompt(
            'Enter the new folder name:',
            currentName);


    // Cancel clicked.
    if (input === null) {
        return;
    }


    const newName =
        input.trim();


    // =====================================================
    // VALIDATION
    // =====================================================

    if (!newName) {
        showMessage(
            'Folder name cannot be empty.');

        return;
    }


    // Same name-na unnecessary API request avoid.
    if (newName === currentName) {
        showMessage(
            'Folder name was not changed.');

        return;
    }


    // =====================================================
    // DISABLE ACTION BUTTONS
    // =====================================================

    setButtonDisabled(
        renameButton,
        true);


    setButtonDisabled(
        deleteButton,
        true);


    showMessage(
        'Renaming folder...');


    try {
        // =============================================
        // PUT /api/folders/{id}
        // =============================================

        await folderApi
            .update(
                folderId,
                {
                    name:
                        newName
                });


        showMessage(
            'Folder renamed successfully.');


        // =============================================
        // REFRESH LIST
        // =============================================

        await loadFolders();
    }
    catch (error) {
        showMessage(
            error.message ||
            'Folder could not be renamed.');
    }
    finally {
        setButtonDisabled(
            renameButton,
            false);


        setButtonDisabled(
            deleteButton,
            false);
    }
}


// =========================================================
// DELETE FOLDER
// =========================================================
//
// Function:
// Folder delete confirmation ketu
// DELETE /api/folders/{id} call pannum.
//
// IMPORTANT:
//
// Current backend relationship:
//
// Folder
//    ↓ delete
// Document FolderId
//    ↓
// null
//
// Document itself delete aaga koodathu.
async function deleteFolder(
    folder,
    renameButton,
    deleteButton) {

    // =====================================================
    // VALIDATE ID
    // =====================================================

    const folderId =
        Number(
            folder?.id);


    if (!Number.isInteger(
        folderId)
        ||
        folderId <= 0) {

        showMessage(
            'Invalid folder.');

        return;
    }


    // =====================================================
    // CONFIRM DELETE
    // =====================================================

    const folderName =
        folder?.name ||
        'this folder';


    const confirmed =
        window.confirm(
            `Delete "${folderName}"? Documents inside the folder will remain in your vault.`);


    if (!confirmed) {
        return;
    }


    // =====================================================
    // DISABLE BUTTONS
    // =====================================================

    setButtonDisabled(
        renameButton,
        true);


    setButtonDisabled(
        deleteButton,
        true);


    showMessage(
        'Deleting folder...');


    try {
        // =============================================
        // DELETE /api/folders/{id}
        // =============================================

        await folderApi
            .remove(
                folderId);


        showMessage(
            'Folder deleted successfully.');


        // =============================================
        // REFRESH LIST
        // =============================================

        await loadFolders();
    }
    catch (error) {
        showMessage(
            error.message ||
            'Folder could not be deleted.');
    }
    finally {
        setButtonDisabled(
            renameButton,
            false);


        setButtonDisabled(
            deleteButton,
            false);
    }
}


// =========================================================
// BUTTON HELPER
// =========================================================
//
// Function:
// API operation nadakkumbodhu double click
// prevent pannum.
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
// Success / error / loading text show pannum.
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
import {
    requireAuth
} from '../auth/authGuard.js';

import {
    documentApi
} from '../api/documentApi.js';

import {
    folderApi
} from '../api/folderApi.js';

import {
    documentShareApi
} from '../api/documentShareApi.js';

import {
    fileSize
} from '../utils/formatters.js';


// =========================================================
// DOCUMENT PAGE
// =========================================================
//
// OWN DOCUMENT FEATURES:
//
// ✅ Upload
// ✅ List
// ✅ Details
// ✅ Download
// ✅ Rename
// ✅ Delete
//
// SECURE SHARING:
//
// ✅ Share
// ✅ Recipient email
// ✅ Invitation email
// ✅ Pending / Accepted / Revoked
// ✅ Manage shares
// ✅ Revoke
//
// =========================================================


const MAX_FILE_SIZE =
    10 * 1024 * 1024;


const ALLOWED_EXTENSIONS =
    [
        '.pdf',
        '.doc',
        '.docx',
        '.jpg',
        '.jpeg',
        '.png'
    ];


// =========================================================
// AUTH
// =========================================================

const authenticated =
    requireAuth();


// =========================================================
// DOCUMENT ELEMENTS
// =========================================================

const form =
    document.getElementById(
        'documentForm');


const fileInput =
    form?.querySelector(
        '[name="file"]');


const folderSelect =
    form?.querySelector(
        '[name="folderId"]');


const uploadButton =
    document.getElementById(
        'uploadButton');


const locationRoot =
    document.getElementById(
        'locationRoot');


const locationFolder =
    document.getElementById(
        'locationFolder');


const folderSelectionArea =
    document.getElementById(
        'folderSelectionArea');


const folderAvailabilityMessage =
    document.getElementById(
        'folderAvailabilityMessage');


const uploadDestination =
    document.getElementById(
        'uploadDestination');


const encryptionProgressPanel =
    document.getElementById(
        'encryptionProgressPanel');


const encryptionProgressTitle =
    document.getElementById(
        'encryptionProgressTitle');


const encryptionProgressText =
    document.getElementById(
        'encryptionProgressText');


const encryptionProgressPercent =
    document.getElementById(
        'encryptionProgressPercent');


const encryptionProgressTrack =
    document.getElementById(
        'encryptionProgressTrack');


const encryptionProgressBar =
    document.getElementById(
        'encryptionProgressBar');


const encryptionStatusIcon =
    document.getElementById(
        'encryptionStatusIcon');


const encryptionStatusText =
    document.getElementById(
        'encryptionStatusText');


const list =
    document.getElementById(
        'list');


const message =
    document.getElementById(
        'message');


const detailsSection =
    document.getElementById(
        'details');


const detailsContent =
    document.getElementById(
        'detailsContent');


// =========================================================
// SHARE ELEMENTS
// =========================================================

const sharePanel =
    document.getElementById(
        'sharePanel');


const sharePanelTitle =
    document.getElementById(
        'sharePanelTitle');


const shareDocumentName =
    document.getElementById(
        'shareDocumentName');


const shareForm =
    document.getElementById(
        'shareForm');


const recipientEmailInput =
    shareForm?.querySelector(
        '[name="recipientEmail"]');


const sendShareButton =
    document.getElementById(
        'sendShareButton');


const closeShareButton =
    document.getElementById(
        'closeShareButton');


const shareList =
    document.getElementById(
        'shareList');


// =========================================================
// STATE
// =========================================================

const folderNames =
    new Map();


let selectedShareDocument =
    null;


let availableFolderCount =
    0;


// =========================================================
// START
// =========================================================

if (authenticated) {

    initializeDocumentPage();
}


// =========================================================
// INITIALIZE
// =========================================================

async function initializeDocumentPage() {

    resetEncryptionProgress();

    await loadFolders();

    syncUploadLocationUi();

    await loadDocuments();
}


// =========================================================
// LOAD FOLDERS
// =========================================================

async function loadFolders() {

    try {

        const result =
            await folderApi.all();


        const folders =
            Array.isArray(result)
                ? result
                : [];


        availableFolderCount =
            folders.length;


        folderNames.clear();


        folderSelect
            ?.replaceChildren();


        const placeholderOption =
            document.createElement(
                'option');


        placeholderOption.value =
            '';


        placeholderOption.textContent =
            folders.length > 0
                ? 'Select a folder'
                : 'No folders available';


        folderSelect
            ?.appendChild(
                placeholderOption);


        folders.forEach(
            folder => {
                const id =
                    Number(folder?.id);


                if (!Number.isInteger(id) ||
                    id <= 0) {

                    return;
                }


                const name =
                    folder?.name ||
                    `Folder ${id}`;


                folderNames.set(
                    id,
                    name);


                const option =
                    document.createElement(
                        'option');


                option.value =
                    String(id);


                option.textContent =
                    name;


                folderSelect
                    ?.appendChild(
                        option);
            });


        if (locationFolder) {
            locationFolder.disabled =
                folders.length === 0;
        }


        if (folders.length === 0) {
            if (locationRoot) {
                locationRoot.checked =
                    true;
            }


            if (folderAvailabilityMessage) {
                folderAvailabilityMessage.textContent =
                    'No folders yet. Create a folder first if you want to organise this upload.';
            }
        }
        else if (folderAvailabilityMessage) {
            folderAvailabilityMessage.textContent =
                `${folders.length} folder${folders.length === 1 ? '' : 's'} available.`;
        }


        syncUploadLocationUi();
    }
    catch (error) {

        availableFolderCount =
            0;


        if (locationFolder) {
            locationFolder.disabled =
                true;
        }


        if (locationRoot) {
            locationRoot.checked =
                true;
        }


        if (folderAvailabilityMessage) {
            folderAvailabilityMessage.textContent =
                'Folders could not be loaded. The file can still be stored in the Root Vault.';
        }


        syncUploadLocationUi();


        showMessage(
            error.message ||
            'Folders could not be loaded.');
    }
}


// =========================================================
// UPLOAD LOCATION UI
// =========================================================

locationRoot?.addEventListener(
    'change',
    syncUploadLocationUi);


locationFolder?.addEventListener(
    'change',
    syncUploadLocationUi);


folderSelect?.addEventListener(
    'change',
    updateUploadDestination);


function syncUploadLocationUi() {

    const useFolder =
        Boolean(
            locationFolder?.checked
            && !locationFolder?.disabled
            && availableFolderCount > 0);


    if (folderSelect) {
        folderSelect.disabled =
            !useFolder;

        folderSelect.required =
            useFolder;


        if (!useFolder) {
            folderSelect.value =
                '';
        }
    }


    folderSelectionArea
        ?.classList.toggle(
            'is-disabled',
            !useFolder);


    updateUploadDestination();
}


function updateUploadDestination() {

    if (!uploadDestination) {
        return;
    }


    let destination =
        'Root Vault';


    if (
        locationFolder?.checked
        && !locationFolder.disabled
    ) {
        const folderId =
            Number(
                folderSelect?.value || 0);


        destination =
            folderId > 0
                ? (folderNames.get(folderId) || 'Selected Folder')
                : 'Choose a folder';
    }


    uploadDestination.replaceChildren();


    uploadDestination.append(
        document.createTextNode(
            'Upload destination: '));


    const strong =
        document.createElement(
            'strong');


    strong.textContent =
        destination;


    uploadDestination.appendChild(
        strong);
}


// =========================================================
// LOAD DOCUMENTS
// =========================================================

async function loadDocuments() {

    showMessage(
        'Loading documents...');


    try {

        const result =
            await documentApi.all();


        const documents =
            Array.isArray(result)
                ? result
                : [];


        renderDocuments(
            documents);


        showMessage(
            '');
    }
    catch (error) {

        list?.replaceChildren();


        showMessage(
            error.message ||
            'Documents could not be loaded.');
    }
}


// =========================================================
// UPLOAD
// =========================================================

form?.addEventListener(
    'submit',
    async event => {
        event.preventDefault();


        if (!requireAuth()) {
            return;
        }


        const file =
            fileInput?.files?.[0];


        if (!file) {

            showMessage(
                'Please select a document.');

            return;
        }


        const extension =
            getExtension(
                file.name);


        if (!ALLOWED_EXTENSIONS.includes(
            extension)) {

            showMessage(
                'Allowed files: PDF, DOC, DOCX, JPG, JPEG and PNG.');

            return;
        }


        if (file.size <= 0) {

            showMessage(
                'The selected file is empty.');

            return;
        }


        if (file.size >
            MAX_FILE_SIZE) {

            showMessage(
                'The selected file must not exceed 10 MB.');

            return;
        }


        const saveInsideFolder =
            Boolean(
                locationFolder?.checked
                && !locationFolder?.disabled);


        const folderId =
            saveInsideFolder
                ? folderSelect?.value?.trim()
                : '';


        if (
            saveInsideFolder
            && !folderId
        ) {
            showMessage(
                'Please choose a folder, or select Root Vault.');

            folderSelect?.focus();

            return;
        }


        const formData =
            new FormData();


        formData.append(
            'file',
            file);


        // FolderId optional.
        // Root Vault select pannina FolderId send panna maatom.
        if (folderId) {

            formData.append(
                'folderId',
                folderId);
        }


        setButtonDisabled(
            uploadButton,
            true);


        beginEncryptionProgress(
            file.name);


        showMessage(
            'Uploading document securely...');


        try {

            await documentApi
                .upload(
                    formData,
                    {
                        onUploadProgress:
                            percent => {
                                // Network upload is the first stage.
                                // Reserve the final 10% for server-side
                                // validation + AES encryption + secure save.
                                const secureProgress =
                                    Math.min(
                                        90,
                                        Math.round(
                                            percent * 0.9));


                                updateEncryptionProgress(
                                    secureProgress,
                                    'Uploading securely...',
                                    `${percent}% of the file has reached the secure server.`,
                                    'uploading');
                            },

                        onServerProcessing:
                            () => {
                                updateEncryptionProgress(
                                    90,
                                    'Encrypting document...',
                                    'Upload complete. AES encryption and secure vault storage are being completed on the server.',
                                    'encrypting');
                            }
                    });


            completeEncryptionProgress();


            form.reset();


            // reset() returns radio to Root Vault, then UI
            // state must be synchronized explicitly.
            syncUploadLocationUi();


            clearDetails();

            closeSharePanel();


            await loadDocuments();


            showMessage(
                folderId
                    ? `File encrypted and stored securely inside ${folderNames.get(Number(folderId)) || 'the selected folder'}.`
                    : 'File encrypted and stored securely in the Root Vault.');
        }
        catch (error) {

            failEncryptionProgress(
                error.message ||
                'Document could not be uploaded.');


            showMessage(
                error.message ||
                'Document could not be uploaded.');
        }
        finally {

            setButtonDisabled(
                uploadButton,
                false);
        }
    });


// =========================================================
// SECURE UPLOAD / ENCRYPTION PROGRESS
// =========================================================

function beginEncryptionProgress(
    fileName) {

    if (encryptionProgressPanel) {
        encryptionProgressPanel.hidden =
            false;
    }


    encryptionProgressPanel
        ?.classList.remove(
            'is-complete',
            'is-error',
            'is-encrypting');


    updateEncryptionProgress(
        0,
        'Preparing secure upload...',
        fileName
            ? `Preparing ${fileName} for secure transfer.`
            : 'Preparing document for secure transfer.',
        'preparing');
}


function updateEncryptionProgress(
    percent,
    title,
    detail,
    stage) {

    const safePercent =
        Math.min(
            100,
            Math.max(
                0,
                Math.round(
                    Number(percent) || 0)));


    if (encryptionProgressBar) {
        encryptionProgressBar.style.width =
            `${safePercent}%`;
    }


    if (encryptionProgressPercent) {
        encryptionProgressPercent.textContent =
            `${safePercent}%`;
    }


    if (encryptionProgressTrack) {
        encryptionProgressTrack.setAttribute(
            'aria-valuenow',
            String(safePercent));
    }


    if (encryptionProgressTitle) {
        encryptionProgressTitle.textContent =
            title;
    }


    if (encryptionProgressText) {
        encryptionProgressText.textContent =
            detail;
    }


    if (encryptionStatusText) {
        encryptionStatusText.textContent =
            title;
    }


    encryptionProgressPanel
        ?.classList.toggle(
            'is-encrypting',
            stage === 'encrypting');


    if (encryptionStatusIcon) {
        encryptionStatusIcon.dataset.stage =
            stage;
    }
}


function completeEncryptionProgress() {

    encryptionProgressPanel
        ?.classList.remove(
            'is-encrypting',
            'is-error');


    encryptionProgressPanel
        ?.classList.add(
            'is-complete');


    updateEncryptionProgress(
        100,
        'File encrypted',
        'Encryption completed successfully. The encrypted vault file has been stored securely.',
        'complete');
}


function failEncryptionProgress(
    reason) {

    encryptionProgressPanel
        ?.classList.remove(
            'is-encrypting',
            'is-complete');


    encryptionProgressPanel
        ?.classList.add(
            'is-error');


    if (encryptionProgressTitle) {
        encryptionProgressTitle.textContent =
            'Secure upload failed';
    }


    if (encryptionProgressText) {
        encryptionProgressText.textContent =
            reason ||
            'The document was not stored.';
    }


    if (encryptionStatusText) {
        encryptionStatusText.textContent =
            'Not stored';
    }


    if (encryptionStatusIcon) {
        encryptionStatusIcon.dataset.stage =
            'error';
    }
}


function resetEncryptionProgress() {

    if (encryptionProgressPanel) {
        encryptionProgressPanel.hidden =
            true;
    }


    if (encryptionProgressBar) {
        encryptionProgressBar.style.width =
            '0%';
    }


    if (encryptionProgressPercent) {
        encryptionProgressPercent.textContent =
            '0%';
    }


    if (encryptionProgressTrack) {
        encryptionProgressTrack.setAttribute(
            'aria-valuenow',
            '0');
    }
}


// =========================================================
// RENDER DOCUMENTS
// =========================================================

function renderDocuments(
    documents) {

    if (!list) {
        return;
    }


    list.replaceChildren();


    if (documents.length === 0) {

        const empty =
            document.createElement(
                'div');


        empty.className =
            'card vault-item';


        empty.textContent =
            'No documents uploaded yet.';


        list.appendChild(
            empty);


        return;
    }


    documents.forEach(
        item => {
            list.appendChild(
                createDocumentCard(
                    item));
        });
}


// =========================================================
// DOCUMENT CARD
// =========================================================

function createDocumentCard(
    item) {

    const card =
        document.createElement(
            'div');


    card.className =
        'card vault-item';


    const information =
        document.createElement(
            'div');


    const name =
        document.createElement(
            'strong');


    name.textContent =
        item?.fileName ||
        'Unnamed document';


    const metadata =
        document.createElement(
            'div');


    metadata.textContent =
        `${fileSize(
            Number(item?.fileSize || 0)
        )} · ${getFolderName(
            item?.folderId
        )}`;


    information.appendChild(
        name);


    information.appendChild(
        metadata);


    const actions =
        document.createElement(
            'div');


    actions.className =
        'vault-actions';


    // =====================================================
    // DETAILS
    // =====================================================

    const detailsButton =
        createButton(
            'Details');


    detailsButton.addEventListener(
        'click',
        async () => {
            await showDocumentDetails(
                item,
                detailsButton);
        });


    // =====================================================
    // DOWNLOAD
    // =====================================================

    const downloadButton =
        createButton(
            'Download');


    downloadButton.addEventListener(
        'click',
        async () => {
            await downloadDocument(
                item,
                downloadButton);
        });


    // =====================================================
    // RENAME
    // =====================================================

    const renameButton =
        createButton(
            'Rename');


    renameButton.addEventListener(
        'click',
        async () => {
            await renameDocument(
                item,
                renameButton);
        });


    // =====================================================
    // SHARE
    // =====================================================

    const shareButton =
        createButton(
            'Share');


    shareButton.addEventListener(
        'click',
        async () => {
            await openSharePanel(
                item,
                true);
        });


    // =====================================================
    // MANAGE SHARES
    // =====================================================

    const manageSharesButton =
        createButton(
            'Shared With');


    manageSharesButton.addEventListener(
        'click',
        async () => {
            await openSharePanel(
                item,
                false);
        });


    // =====================================================
    // DELETE
    // =====================================================

    const deleteButton =
        createButton(
            'Delete');


    deleteButton.addEventListener(
        'click',
        async () => {
            await deleteDocument(
                item,
                deleteButton);
        });


    actions.append(
        detailsButton,
        downloadButton,
        renameButton,
        shareButton,
        manageSharesButton,
        deleteButton);


    card.append(
        information,
        actions);


    return card;
}


// =========================================================
// DETAILS
// =========================================================

async function showDocumentDetails(
    item,
    button) {

    const id =
        getDocumentId(
            item);


    if (!id) {
        return;
    }


    setButtonDisabled(
        button,
        true);


    try {

        const details =
            await documentApi.get(
                id);


        renderDocumentDetails(
            details);
    }
    catch (error) {

        showMessage(
            error.message ||
            'Document details could not be loaded.');
    }
    finally {

        setButtonDisabled(
            button,
            false);
    }
}


function renderDocumentDetails(
    details) {

    if (!detailsSection ||
        !detailsContent) {

        return;
    }


    detailsContent.replaceChildren();


    appendDetail(
        detailsContent,
        'File Name',
        details?.fileName || '-');


    appendDetail(
        detailsContent,
        'Type',
        details?.contentType || '-');


    appendDetail(
        detailsContent,
        'Size',
        fileSize(
            Number(
                details?.fileSize || 0)));


    appendDetail(
        detailsContent,
        'Folder',
        getFolderName(
            details?.folderId));


    appendDetail(
        detailsContent,
        'Uploaded',
        formatDate(
            details?.createdAt));


    detailsSection.hidden =
        false;
}


// =========================================================
// DOWNLOAD
// =========================================================

async function downloadDocument(
    item,
    button) {

    const id =
        getDocumentId(
            item);


    if (!id) {
        return;
    }


    setButtonDisabled(
        button,
        true);


    try {

        await documentApi
            .download(
                id,
                item.fileName);


        showMessage(
            'Document downloaded successfully.');
    }
    catch (error) {

        showMessage(
            error.message ||
            'Document could not be downloaded.');
    }
    finally {

        setButtonDisabled(
            button,
            false);
    }
}


// =========================================================
// RENAME
// =========================================================

async function renameDocument(
    item,
    button) {

    const id =
        getDocumentId(
            item);


    if (!id) {
        return;
    }


    const oldName =
        item?.fileName || '';


    const input =
        window.prompt(
            'Enter the new document name:',
            oldName);


    if (input === null) {
        return;
    }


    const newName =
        input.trim();


    if (!newName) {

        showMessage(
            'Document name cannot be empty.');

        return;
    }


    if (getExtension(oldName) !==
        getExtension(newName)) {

        showMessage(
            'The file extension cannot be changed.');

        return;
    }


    setButtonDisabled(
        button,
        true);


    try {

        await documentApi
            .rename(
                id,
                {
                    fileName:
                        newName
                });


        clearDetails();

        closeSharePanel();

        await loadDocuments();


        showMessage(
            'Document renamed successfully.');
    }
    catch (error) {

        showMessage(
            error.message ||
            'Document could not be renamed.');
    }
    finally {

        setButtonDisabled(
            button,
            false);
    }
}


// =========================================================
// DELETE
// =========================================================

async function deleteDocument(
    item,
    button) {

    const id =
        getDocumentId(
            item);


    if (!id) {
        return;
    }


    const confirmed =
        window.confirm(
            `Delete "${item?.fileName || 'this document'}" permanently?`);


    if (!confirmed) {
        return;
    }


    setButtonDisabled(
        button,
        true);


    try {

        await documentApi
            .remove(
                id);


        clearDetails();

        closeSharePanel();

        await loadDocuments();


        showMessage(
            'Document deleted successfully.');
    }
    catch (error) {

        showMessage(
            error.message ||
            'Document could not be deleted.');
    }
    finally {

        setButtonDisabled(
            button,
            false);
    }
}


// =========================================================
// OPEN SHARE PANEL
// =========================================================

async function openSharePanel(
    documentItem,
    focusEmail) {

    const id =
        getDocumentId(
            documentItem);


    if (!id ||
        !sharePanel) {

        showMessage(
            'Invalid document.');

        return;
    }


    selectedShareDocument =
        documentItem;


    if (shareDocumentName) {

        shareDocumentName.textContent =
            `Document: ${documentItem.fileName}`;
    }


    if (sharePanelTitle) {

        sharePanelTitle.textContent =
            'Secure Document Sharing';
    }


    sharePanel.hidden =
        false;


    recipientEmailInput.value =
        '';


    await loadDocumentShares();


    if (focusEmail) {

        recipientEmailInput
            ?.focus();
    }


    sharePanel.scrollIntoView(
        {
            behavior:
                'smooth',

            block:
                'start'
        });
}


// =========================================================
// SEND SHARE INVITATION
// =========================================================

shareForm?.addEventListener(
    'submit',
    async event => {
        event.preventDefault();


        if (!requireAuth()) {
            return;
        }


        const documentId =
            getDocumentId(
                selectedShareDocument);


        if (!documentId) {

            showMessage(
                'Select a document first.');

            return;
        }


        const recipientEmail =
            recipientEmailInput
                ?.value
                ?.trim()
                .toLowerCase()
            || '';


        if (!recipientEmail) {

            showMessage(
                'Recipient email is required.');

            return;
        }


        setButtonDisabled(
            sendShareButton,
            true);


        showMessage(
            'Creating secure invitation...');


        try {

            const share =
                await documentShareApi
                    .share(
                        documentId,
                        recipientEmail);


            // Raw token intentionally unavailable here.
            recipientEmailInput.value =
                '';


            await loadDocumentShares();


            showMessage(
                `Secure invitation sent to ${share?.recipientEmail || recipientEmail}.`);
        }
        catch (error) {

            showMessage(
                error.message ||
                'The document could not be shared with this recipient.');
        }
        finally {

            setButtonDisabled(
                sendShareButton,
                false);
        }
    });


// =========================================================
// LOAD OWNER SHARE LIST
// =========================================================

async function loadDocumentShares() {

    if (!shareList) {
        return;
    }


    const documentId =
        getDocumentId(
            selectedShareDocument);


    if (!documentId) {
        return;
    }


    shareList.replaceChildren();


    const loading =
        document.createElement(
            'div');


    loading.textContent =
        'Loading share status...';


    shareList.appendChild(
        loading);


    try {

        const response =
            await documentShareApi
                .getDocumentShares(
                    documentId);


        const shares =
            Array.isArray(response)
                ? response
                : [];


        renderDocumentShares(
            shares);
    }
    catch (error) {

        shareList.replaceChildren();


        const failed =
            document.createElement(
                'div');


        failed.textContent =
            error.message ||
            'Share information could not be loaded.';


        shareList.appendChild(
            failed);
    }
}


// =========================================================
// RENDER SHARES
// =========================================================

function renderDocumentShares(
    shares) {

    shareList.replaceChildren();


    if (shares.length === 0) {

        const empty =
            document.createElement(
                'div');


        empty.textContent =
            'This document has not been shared yet.';


        shareList.appendChild(
            empty);


        return;
    }


    shares.forEach(
        share => {
            shareList.appendChild(
                createShareCard(
                    share));
        });
}


// =========================================================
// SHARE CARD
// =========================================================

function createShareCard(
    share) {

    const card =
        document.createElement(
            'div');


    card.className =
        'card vault-item';


    const info =
        document.createElement(
            'div');


    const recipient =
        document.createElement(
            'strong');


    recipient.textContent =
        share?.recipientName ||
        'Recipient';


    const email =
        document.createElement(
            'div');


    email.textContent =
        share?.recipientEmail ||
        '-';


    const status =
        document.createElement(
            'div');


    status.textContent =
        `Status: ${formatShareStatus(
            share?.status
        )}`;


    const shared =
        document.createElement(
            'div');


    shared.textContent =
        `Shared: ${formatDate(
            share?.sharedAt
        )}`;


    const expiry =
        document.createElement(
            'div');


    expiry.textContent =
        `Invitation expires: ${formatDate(
            share?.expiresAt
        )}`;


    info.append(
        recipient,
        email,
        status,
        shared,
        expiry);


    if (share?.acceptedAt) {

        const accepted =
            document.createElement(
                'div');


        accepted.textContent =
            `Accepted: ${formatDate(
                share.acceptedAt
            )}`;


        info.appendChild(
            accepted);
    }


    if (share?.revokedAt) {

        const revoked =
            document.createElement(
                'div');


        revoked.textContent =
            `Revoked: ${formatDate(
                share.revokedAt
            )}`;


        info.appendChild(
            revoked);
    }


    const actions =
        document.createElement(
            'div');


    actions.className =
        'vault-actions';


    const revokeButton =
        createButton(
            'Revoke');


    revokeButton.addEventListener(
        'click',
        async () => {
            await revokeShare(
                share,
                revokeButton);
        });


    actions.appendChild(
        revokeButton);


    card.append(
        info,
        actions);


    return card;
}


// =========================================================
// REVOKE
// =========================================================

async function revokeShare(
    share,
    button) {

    const documentId =
        getDocumentId(
            selectedShareDocument);


    const shareId =
        Number(
            share?.shareId);


    if (!documentId ||
        !Number.isInteger(shareId) ||
        shareId <= 0) {

        showMessage(
            'Invalid share.');

        return;
    }


    const confirmed =
        window.confirm(
            `Revoke access for ${share?.recipientEmail || 'this recipient'}?`);


    if (!confirmed) {
        return;
    }


    setButtonDisabled(
        button,
        true);


    try {

        await documentShareApi
            .revoke(
                documentId,
                shareId);


        await loadDocumentShares();


        showMessage(
            'Document share revoked successfully.');
    }
    catch (error) {

        showMessage(
            error.message ||
            'Document share could not be revoked.');
    }
    finally {

        setButtonDisabled(
            button,
            false);
    }
}


// =========================================================
// CLOSE SHARE PANEL
// =========================================================

closeShareButton?.addEventListener(
    'click',
    () => {
        closeSharePanel();
    });


function closeSharePanel() {

    selectedShareDocument =
        null;


    shareForm?.reset();


    shareList
        ?.replaceChildren();


    if (sharePanel) {

        sharePanel.hidden =
            true;
    }
}


// =========================================================
// HELPERS
// =========================================================

function getDocumentId(
    item) {

    const id =
        Number(
            item?.id);


    return Number.isInteger(id) &&
        id > 0
        ?
        id
        :
        null;
}


function getFolderName(
    folderId) {

    if (folderId === null ||
        folderId === undefined ||
        folderId === '') {

        return 'No folder';
    }


    return folderNames.get(
        Number(folderId))
        ||
        `Folder ${folderId}`;
}


function getExtension(
    fileName) {

    if (!fileName) {
        return '';
    }


    const position =
        fileName.lastIndexOf('.');


    if (position <= 0) {
        return '';
    }


    return fileName
        .slice(position)
        .toLowerCase();
}


function formatDate(
    value) {

    if (!value) {
        return '-';
    }


    const date =
        new Date(value);


    return Number.isNaN(
        date.getTime())
        ?
        '-'
        :
        date.toLocaleString();
}


// =========================================================
// SHARE STATUS
// =========================================================
//
// JsonStringEnumConverter irundha:
//
// Pending
// Accepted
// Revoked
//
// Numeric serialization irundhaal common enum order:
//
// 0 Pending
// 1 Accepted
// 2 Revoked
//
// rendu format-um display handle pannuvom.
function formatShareStatus(
    value) {

    const text =
        String(
            value ?? '')
            .trim();


    if (text === '0') {
        return 'Pending';
    }


    if (text === '1') {
        return 'Accepted';
    }


    if (text === '2') {
        return 'Revoked';
    }


    if (!text) {
        return 'Unknown';
    }


    return text;
}


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


function appendDetail(
    container,
    label,
    value) {

    const row =
        document.createElement(
            'p');


    const strong =
        document.createElement(
            'strong');


    strong.textContent =
        `${label}: `;


    const span =
        document.createElement(
            'span');


    span.textContent =
        String(value);


    row.append(
        strong,
        span);


    container.appendChild(
        row);
}


function clearDetails() {

    detailsContent
        ?.replaceChildren();


    if (detailsSection) {

        detailsSection.hidden =
            true;
    }
}


function setButtonDisabled(
    button,
    disabled) {

    if (button) {

        button.disabled =
            disabled;
    }
}


function showMessage(
    text) {

    if (message) {

        message.textContent =
            text;
    }
}
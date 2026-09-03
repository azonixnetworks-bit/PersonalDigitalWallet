import {
    requireAuth
} from '../auth/authGuard.js';

import {
    tokenManager
} from '../auth/tokenManager.js';

import {
    adminApi
} from '../api/adminApi.js';

import {
    authApi
} from '../api/authApi.js';


// =========================================================
// ADMIN PAGE
// =========================================================
//
// FEATURES:
//
// ✅ Admin JWT check
// ✅ Frontend Admin role check
// ✅ Dashboard statistics
// ✅ Recent SAFE upload metadata
// ✅ User list
// ✅ Client-side user search
// ✅ Enable normal users
// ✅ Disable normal users
// ✅ Admin account protected
// ✅ Admin logout
//
// PRIVACY:
//
// Admin page NEVER calls:
//
// ❌ /api/documents/{id}
// ❌ /api/documents/{id}/download
// ❌ /api/credentials
// ❌ /api/search
// ❌ /api/shares
//
// =========================================================


// =========================================================
// AUTHENTICATION CHECK
// =========================================================

const authenticated =
    requireAuth();


// =========================================================
// HTML ELEMENTS
// =========================================================

const statsContainer =
    document.getElementById(
        'stats');


const uploadsContainer =
    document.getElementById(
        'uploads');


const usersContainer =
    document.getElementById(
        'users');


const userSearch =
    document.getElementById(
        'userSearch');


const messageBox =
    document.getElementById(
        'message');


const logoutButton =
    document.getElementById(
        'adminLogoutButton');


// =========================================================
// STATE
// =========================================================
//
// API-lendhu varra safe AdminUserDto list
// temporary browser memory-la mattum store pannuvom.
let allUsers =
    [];


// =========================================================
// PAGE START
// =========================================================

if (authenticated) {

    initializeAdminPage();
}


// =========================================================
// INITIALIZE ADMIN PAGE
// =========================================================
//
// Flow:
//
// JWT exists
//    ↓
// Frontend role check
//    ↓
// Admin?
//    ├── NO -> dashboard
//    ↓ YES
// Load Dashboard + Users
//
// IMPORTANT:
//
// Frontend role check final security illa.
//
// Backend:
// [Authorize(Roles = "Admin")]
//
// final authority.
async function initializeAdminPage() {

    // =====================================================
    // FRONTEND ROLE CHECK
    // =====================================================

    const role =
        getCurrentTokenRole();


    if (
        role
        &&
        role.toLowerCase() !==
        'admin'
    ) {

        window.location.href =
            '/html/dashboard.html';

        return;
    }


    try {

        // Dashboard + User list independent requests.
        await Promise.all(
            [
                loadDashboard(),
                loadUsers()
            ]
        );
    }
    catch (error) {

        showMessage(
            error.message ||
            'Administrator dashboard could not be loaded.',
            'error'
        );
    }
}


// =========================================================
// GET CURRENT TOKEN ROLE
// =========================================================
//
// JWT claim serialization configuration depend panni
// role key different form-la irukkalaam.
//
// Common variants safely check pannuvom.
//
// IMPORTANT:
// Actual authorization backend dhaan.
function getCurrentTokenRole() {

    const payload =
        tokenManager.getPayload();


    if (!payload) {
        return '';
    }


    const role =
        payload.role
        ??
        payload.Role
        ??
        payload[
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ]
        ??
        '';


    return String(role)
        .trim();
}


// =========================================================
// LOAD DASHBOARD
// =========================================================
//
// API:
// GET /api/admin/dashboard
//
// Output:
//
// totalUsers
// totalUploads
// totalStoredFiles
// recentUploads
async function loadDashboard() {

    const dashboard =
        await adminApi
            .dashboard();


    renderStatistics(
        dashboard);


    renderRecentUploads(
        Array.isArray(
            dashboard?.recentUploads)
            ?
            dashboard.recentUploads
            :
            []
    );
}


// =========================================================
// RENDER STATISTICS
// =========================================================
//
// Input:
// AdminDashboardDto.
//
// Output:
// 3 dashboard cards.
//
// No private user data.
function renderStatistics(
    dashboard) {

    if (!statsContainer) {
        return;
    }


    statsContainer
        .replaceChildren();


    statsContainer.append(
        createStatCard(
            'Total Users',
            safeCount(
                dashboard?.totalUsers),
            'Registered accounts'
        ),

        createStatCard(
            'Total Uploads',
            safeCount(
                dashboard?.totalUploads),
            'Files uploaded'
        ),

        createStatCard(
            'Total Stored Files',
            safeCount(
                dashboard?.totalStoredFiles),
            'Currently stored files'
        )
    );
}


// =========================================================
// CREATE STAT CARD
// =========================================================

function createStatCard(
    label,
    value,
    description) {

    const card =
        document.createElement(
            'article');


    card.className =
        'admin-stat-card card';


    const labelElement =
        document.createElement(
            'div');


    labelElement.className =
        'stat-label';


    labelElement.textContent =
        label;


    const numberElement =
        document.createElement(
            'div');


    numberElement.className =
        'stat-number';


    numberElement.textContent =
        String(value);


    const descriptionElement =
        document.createElement(
            'div');


    descriptionElement.className =
        'stat-description';


    descriptionElement.textContent =
        description;


    card.append(
        labelElement,
        numberElement,
        descriptionElement
    );


    return card;
}


// =========================================================
// RENDER RECENT UPLOADS
// =========================================================
//
// IMPORTANT SECURITY:
//
// Frontend whitelist:
//
// ✅ uploadedBy
// ✅ fileName
// ✅ fileSize
// ✅ uploadedAt
//
// Even backend object accidentally extra properties
// contain pannina:
//
// storagePath
// storedFileName
// fileHash
//
// render panna maatom.
function renderRecentUploads(
    uploads) {

    if (!uploadsContainer) {
        return;
    }


    uploadsContainer
        .replaceChildren();


    if (uploads.length === 0) {

        const row =
            document.createElement(
                'tr');


        const cell =
            document.createElement(
                'td');


        cell.colSpan =
            4;


        cell.textContent =
            'No recent upload activity found.';


        row.appendChild(
            cell);


        uploadsContainer
            .appendChild(
                row);


        return;
    }


    uploads.forEach(
        upload => {
            const row =
                document.createElement(
                    'tr');


            appendTableCell(
                row,
                upload?.uploadedBy ||
                'Unknown User'
            );


            appendTableCell(
                row,
                upload?.fileName ||
                'Unnamed file'
            );


            appendTableCell(
                row,
                formatFileSize(
                    upload?.fileSize)
            );


            appendTableCell(
                row,
                formatDate(
                    upload?.uploadedAt)
            );


            uploadsContainer
                .appendChild(
                    row);
        }
    );
}


// =========================================================
// LOAD USERS
// =========================================================
//
// API:
// GET /api/admin/users
async function loadUsers() {

    const result =
        await adminApi
            .users();


    allUsers =
        Array.isArray(result)
            ?
            result
                .map(
                    sanitizeUser)
                .filter(
                    user =>
                        user !== null)
            :
            [];


    renderUsers(
        allUsers);
}


// =========================================================
// SANITIZE USER DTO
// =========================================================
//
// Whitelist safe Admin User fields.
//
// Output:
//
// {
//     id,
//     fullName,
//     email,
//     role,
//     isActive
// }
//
// Unexpected backend fields drop pannuvom.
function sanitizeUser(
    user) {

    if (!user ||
        typeof user !==
        'object') {

        return null;
    }


    const id =
        Number(
            user.id);


    if (
        !Number.isInteger(id)
        ||
        id <= 0
    ) {

        return null;
    }


    return {

        id,

        fullName:
            String(
                user.fullName ?? '')
                .trim(),

        email:
            String(
                user.email ?? '')
                .trim(),

        role:
            String(
                user.role ?? 'User')
                .trim(),

        isActive:
            Boolean(
                user.isActive)
    };
}


// =========================================================
// USER SEARCH
// =========================================================
//
// Backend new endpoint create panna maatom.
//
// GET /api/admin/users result-ai
// client side filter pannuvom.
userSearch?.addEventListener(
    'input',
    () => {
        const keyword =
            userSearch.value
                .trim()
                .toLowerCase();


        if (!keyword) {

            renderUsers(
                allUsers);

            return;
        }


        const filtered =
            allUsers.filter(
                user => {
                    return user.fullName
                        .toLowerCase()
                        .includes(
                            keyword)

                        ||

                        user.email
                            .toLowerCase()
                            .includes(
                                keyword);
                }
            );


        renderUsers(
            filtered);
    }
);


// =========================================================
// RENDER USERS
// =========================================================

function renderUsers(
    users) {

    if (!usersContainer) {
        return;
    }


    usersContainer
        .replaceChildren();


    if (users.length === 0) {

        const row =
            document.createElement(
                'tr');


        const cell =
            document.createElement(
                'td');


        cell.colSpan =
            5;


        cell.textContent =
            'No matching users found.';


        row.appendChild(
            cell);


        usersContainer
            .appendChild(
                row);


        return;
    }


    users.forEach(
        user => {
            usersContainer
                .appendChild(
                    createUserRow(
                        user));
        }
    );
}


// =========================================================
// CREATE USER ROW
// =========================================================
//
// Columns:
//
// Name
// Email
// Role
// Status
// Action
function createUserRow(
    user) {

    const row =
        document.createElement(
            'tr');


    // =====================================================
    // NAME
    // =====================================================

    appendTableCell(
        row,
        user.fullName ||
        '-'
    );


    // =====================================================
    // EMAIL
    // =====================================================

    appendTableCell(
        row,
        user.email ||
        '-'
    );


    // =====================================================
    // ROLE
    // =====================================================

    const roleCell =
        document.createElement(
            'td');


    const roleBadge =
        document.createElement(
            'span');


    roleBadge.className =
        `admin-badge ${isAdminRole(
            user.role)
            ?
            'role-admin'
            :
            'role-user'
        }`;


    roleBadge.textContent =
        user.role ||
        'User';


    roleCell.appendChild(
        roleBadge);


    row.appendChild(
        roleCell);


    // =====================================================
    // STATUS
    // =====================================================

    const statusCell =
        document.createElement(
            'td');


    const statusBadge =
        document.createElement(
            'span');


    statusBadge.className =
        `admin-badge ${user.isActive
            ?
            'status-active'
            :
            'status-disabled'
        }`;


    statusBadge.textContent =
        user.isActive
            ?
            'Active'
            :
            'Disabled';


    statusCell.appendChild(
        statusBadge);


    row.appendChild(
        statusCell);


    // =====================================================
    // ACTION
    // =====================================================

    const actionCell =
        document.createElement(
            'td');


    // Admin accounts protected.
    if (isAdminRole(
        user.role)) {

        const protectedText =
            document.createElement(
                'span');


        protectedText.className =
            'admin-protected';


        protectedText.textContent =
            'Protected';


        actionCell.appendChild(
            protectedText);
    }
    else {

        const button =
            document.createElement(
                'button');


        button.type =
            'button';


        button.className =
            `admin-user-action ${user.isActive
                ?
                'action-disable'
                :
                'action-enable'
            }`;


        const nextStatus =
            !user.isActive;


        button.textContent =
            nextStatus
                ?
                'Enable'
                :
                'Disable';


        button.addEventListener(
            'click',
            async () => {
                await changeUserStatus(
                    user,
                    nextStatus,
                    button);
            }
        );


        actionCell.appendChild(
            button);
    }


    row.appendChild(
        actionCell);


    return row;
}


// =========================================================
// CHANGE USER STATUS
// =========================================================
//
// API:
//
// PUT /api/admin/users/{id}/status
//
// Body:
//
// {
//     isActive
// }
async function changeUserStatus(
    user,
    newStatus,
    button) {

    // =====================================================
    // FRONTEND ADMIN PROTECTION
    // =====================================================
    //
    // Backend also rejects Admin modification.
    if (isAdminRole(
        user.role)) {

        showMessage(
            'Administrator accounts are protected.',
            'error'
        );

        return;
    }


    const actionName =
        newStatus
            ?
            'enable'
            :
            'disable';


    const confirmed =
        window.confirm(
            `${actionName === 'enable'
                ? 'Enable'
                : 'Disable'
            } "${user.email}"?`
        );


    if (!confirmed) {
        return;
    }


    button.disabled =
        true;


    showMessage(
        `${newStatus
            ? 'Enabling'
            : 'Disabling'
        } user...`,
        'info'
    );


    try {

        await adminApi
            .setStatus(
                user.id,
                newStatus
            );


        // Fresh server truth load.
        await loadUsers();


        // Dashboard counts/metadata refresh too.
        await loadDashboard();


        showMessage(
            newStatus
                ?
                'User enabled successfully.'
                :
                'User disabled successfully.',
            'success'
        );
    }
    catch (error) {

        showMessage(
            error.message ||
            'User account status could not be changed.',
            'error'
        );
    }
    finally {

        // Old button DOM reload aana replace aagirukkalaam.
        //
        // Still safe-aa check pannuvom.
        if (button.isConnected) {

            button.disabled =
                false;
        }
    }
}


// =========================================================
// ADMIN LOGOUT
// =========================================================

logoutButton?.addEventListener(
    'click',
    async () => {
        logoutButton.disabled =
            true;


        try {

            // Backend logout endpoint available.
            await authApi
                .logout();
        }
        catch {

            // Logout API fail aanaalum
            // browser access token preserve panna koodathu.
        }
        finally {

            tokenManager.clear();


            // Temporary auth data cleanup.
            clearTemporaryAuthData();


            window.location.href =
                '/html/login.html';
        }
    }
);


// =========================================================
// CLEAR TEMP AUTH VALUES
// =========================================================

function clearTemporaryAuthData() {

    sessionStorage.removeItem(
        'pdv_login_mfa_challenge');


    sessionStorage.removeItem(
        'pdv_login_email');


    sessionStorage.removeItem(
        'pdv_post_login_redirect');
}


// =========================================================
// IS ADMIN ROLE
// =========================================================

function isAdminRole(
    role) {

    return String(
        role ?? '')
        .trim()
        .toLowerCase()
        ===
        'admin';
}


// =========================================================
// SAFE COUNT
// =========================================================

function safeCount(
    value) {

    const number =
        Number(
            value ?? 0);


    if (
        !Number.isFinite(number)
        ||
        number < 0
    ) {

        return 0;
    }


    return Math.floor(
        number);
}


// =========================================================
// TABLE CELL
// =========================================================
//
// Security:
//
// innerHTML use panna maatom.
//
// Database text ellam textContent.
function appendTableCell(
    row,
    value) {

    const cell =
        document.createElement(
            'td');


    cell.textContent =
        String(
            value ?? '-');


    row.appendChild(
        cell);
}


// =========================================================
// FILE SIZE
// =========================================================
//
// Input:
// bytes.
//
// Output:
// B / KB / MB / GB.
function formatFileSize(
    bytes) {

    const value =
        Number(
            bytes ?? 0);


    if (
        !Number.isFinite(value)
        ||
        value <= 0
    ) {

        return '0 B';
    }


    const units =
        [
            'B',
            'KB',
            'MB',
            'GB'
        ];


    const index =
        Math.min(
            Math.floor(
                Math.log(value) /
                Math.log(1024)
            ),
            units.length - 1
        );


    const size =
        value /
        Math.pow(
            1024,
            index);


    return `${size.toFixed(
        index === 0
            ?
            0
            :
            1
    )} ${units[index]}`;
}


// =========================================================
// DATE FORMAT
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


    return date
        .toLocaleString();
}


// =========================================================
// SHOW MESSAGE
// =========================================================
//
// Input:
//
// text
// type:
// success
// error
// info
//
// Output:
// Admin status message.
//
// Security:
// textContent only.
function showMessage(
    text,
    type = 'info') {

    if (!messageBox) {
        return;
    }


    messageBox.hidden =
        false;


    messageBox.className =
        `admin-message ${type}`;


    messageBox.textContent =
        text;
}
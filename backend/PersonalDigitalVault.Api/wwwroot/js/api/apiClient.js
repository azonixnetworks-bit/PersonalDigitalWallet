import {
    API_BASE_URL
} from '../config.js';

import {
    tokenManager
} from '../auth/tokenManager.js';


// =========================================================
// API CLIENT
// =========================================================
//
// PURPOSE:
//
// Ella frontend API modules:
//
// authApi
// profileApi
// folderApi
// documentApi
// credentialApi
// searchApi
// adminApi
//
// direct fetch repeat pannaama
// indha common api() function use pannum.
//
// Flow:
//
// Page JS
//    ↓
// Feature API
//    ↓
// apiClient
//    ↓
// ASP.NET Core API
//
// =========================================================


// =========================================================
// API FUNCTION
// =========================================================
//
// Input:
//
// path:
//   example:
//   /profile
//   /folders
//   /documents
//
// options:
//   fetch configuration.
//
// Output:
//
// Parsed JSON
// Blob
// null
//
// Security:
//
// Existing FINAL JWT irundha
// Authorization Bearer automatically add pannum.
export async function api(
    path,
    options = {}) {

    // =====================================================
    // CREATE HEADERS
    // =====================================================

    const headers =
        new Headers(
            options.headers || {});


    // =====================================================
    // FINAL JWT
    // =====================================================

    const token =
        tokenManager.get();


    if (token) {
        headers.set(
            'Authorization',
            `Bearer ${token}`);
    }


    // =====================================================
    // CONTENT TYPE
    // =====================================================
    //
    // FormData upload-na browser itself multipart boundary
    // generate pannum.
    //
    // So FormData-ku Content-Type manually set
    // panna koodathu.

    if (
        options.body
        &&
        !(options.body instanceof FormData)
        &&
        !headers.has('Content-Type')
    ) {
        headers.set(
            'Content-Type',
            'application/json');
    }


    let response;


    // =====================================================
    // HTTP REQUEST
    // =====================================================

    try {
        response =
            await fetch(
                `${API_BASE_URL}${path}`,
                {
                    ...options,
                    headers
                });
    }
    catch {
        // Network/server unavailable.
        //
        // Internal browser/network details
        // user-ku expose panna vendaam.
        throw new Error(
            'Unable to connect to the server.');
    }


    // =====================================================
    // 401 UNAUTHORIZED
    // =====================================================
    //
    // IMPORTANT:
    //
    // Login/MFA public requests final token use pannaathu.
    //
    // So existing final JWT send pannina request mattum
    // 401 return aana local final token cleanup pannuvom.

    if (
        response.status === 401
        &&
        token
    ) {
        tokenManager.clear();


        // Current page login page illa-na
        // login-ku redirect.
        if (!window.location.pathname
            .endsWith('/login.html')) {

            window.location.href =
                '/html/login.html?reason=unauthorized';
        }


        throw new Error(
            'Your session is no longer valid. Please login again.');
    }


    // =====================================================
    // 204 NO CONTENT
    // =====================================================

    if (response.status === 204) {
        return null;
    }


    // =====================================================
    // RESPONSE TYPE
    // =====================================================

    const contentType =
        response.headers
            .get('content-type')
        || '';


    let data =
        null;


    // =====================================================
    // JSON RESPONSE
    // =====================================================

    if (contentType.includes(
        'application/json')) {

        try {
            data =
                await response.json();
        }
        catch {
            data =
                null;
        }
    }

    // =====================================================
    // FILE / BLOB RESPONSE
    // =====================================================

    else {
        try {
            data =
                await response.blob();
        }
        catch {
            data =
                null;
        }
    }


    // =====================================================
    // ERROR RESPONSE
    // =====================================================

    if (!response.ok) {
        // Backend normal error shape:
        //
        // {
        //     "message": "..."
        // }
        //
        // ASP.NET validation response sometimes:
        //
        // {
        //     "title": "...",
        //     "errors": {...}
        // }

        const message =
            getErrorMessage(
                data,
                response.status);


        throw new Error(
            message);
    }


    // =====================================================
    // SUCCESS
    // =====================================================

    return data;
}


// =========================================================
// MULTIPART UPLOAD WITH PROGRESS
// =========================================================
//
// fetch() browser upload progress expose pannaathu.
// Document secure-upload page-ku actual bytes-sent progress
// thevai, so XMLHttpRequest use pannuvom.
//
// IMPORTANT:
// - JWT common tokenManager-lendhu mattum.
// - multipart Content-Type manually set panna koodathu.
// - onUploadProgress = network upload bytes progress.
// - onServerProcessing = full request body server reach aana
//   callback. Backend appuram validation/encryption/storage
//   complete pannum; exact encryption percentage browser-ku
//   available illa.
export function apiUpload(
    path,
    formData,
    callbacks = {}) {

    const {
        onUploadProgress,
        onServerProcessing
    } = callbacks;


    return new Promise(
        (resolve, reject) => {

            const token =
                tokenManager.get();


            const xhr =
                new XMLHttpRequest();


            xhr.open(
                'POST',
                `${API_BASE_URL}${path}`,
                true);


            xhr.setRequestHeader(
                'Accept',
                'application/json');


            if (token) {
                xhr.setRequestHeader(
                    'Authorization',
                    `Bearer ${token}`);
            }


            // Browser can report actual upload bytes.
            xhr.upload.onprogress =
                event => {
                    if (!event.lengthComputable) {
                        return;
                    }


                    const percent =
                        Math.min(
                            100,
                            Math.max(
                                0,
                                Math.round(
                                    (event.loaded / event.total) * 100)));


                    if (typeof onUploadProgress === 'function') {
                        onUploadProgress(
                            percent,
                            event.loaded,
                            event.total);
                    }
                };


            // Request body server-kku full-aa send aana point.
            // Backend response varum varaikkum validation,
            // AES encryption, encrypted storage and DB save
            // nadakkalaam.
            xhr.upload.onload =
                () => {
                    if (typeof onServerProcessing === 'function') {
                        onServerProcessing();
                    }
                };


            xhr.onerror =
                () => {
                    reject(
                        new Error(
                            'Unable to connect to the server.'));
                };


            xhr.onabort =
                () => {
                    reject(
                        new Error(
                            'The upload was cancelled.'));
                };


            xhr.onload =
                () => {
                    const contentType =
                        xhr.getResponseHeader(
                            'content-type') || '';


                    let data =
                        null;


                    if (contentType.includes(
                        'application/json')) {

                        try {
                            data =
                                xhr.responseText
                                    ? JSON.parse(
                                        xhr.responseText)
                                    : null;
                        }
                        catch {
                            data = null;
                        }
                    }


                    if (
                        xhr.status === 401
                        && token
                    ) {
                        tokenManager.clear();


                        if (!window.location.pathname
                            .endsWith('/login.html')) {

                            window.location.href =
                                '/html/login.html?reason=unauthorized';
                        }


                        reject(
                            new Error(
                                'Your session is no longer valid. Please login again.'));

                        return;
                    }


                    if (
                        xhr.status >= 200
                        && xhr.status < 300
                    ) {
                        resolve(data);
                        return;
                    }


                    reject(
                        new Error(
                            getErrorMessage(
                                data,
                                xhr.status)));
                };


            xhr.send(
                formData);
        });
}


// =========================================================
// ERROR MESSAGE HELPER
// =========================================================
//
// Function:
// Backend response-lendhu safe useful message extract pannum.
//
// Input:
// response data
// status
//
// Output:
// user-friendly string.
function getErrorMessage(
    data,
    status) {

    // =====================================================
    // NORMAL API MESSAGE
    // =====================================================

    if (
        data
        &&
        typeof data === 'object'
        &&
        !(data instanceof Blob)
        &&
        typeof data.message === 'string'
        &&
        data.message.trim() !== ''
    ) {
        return data.message;
    }


    // =====================================================
    // ASP.NET VALIDATION ERRORS
    // =====================================================

    if (
        data
        &&
        typeof data === 'object'
        &&
        !(data instanceof Blob)
        &&
        data.errors
    ) {
        const validationMessages =
            Object.values(
                data.errors)
                .flat()
                .filter(
                    message =>
                        typeof message === 'string'
                        &&
                        message.trim() !== '');


        if (validationMessages.length > 0) {
            return validationMessages[0];
        }
    }


    // =====================================================
    // STATUS FALLBACK
    // =====================================================

    switch (status) {
        case 400:
            return 'The request is invalid. Please check the entered information.';

        case 401:
            return 'Authentication is required.';

        case 403:
            return 'You are not allowed to perform this action.';

        case 404:
            return 'The requested item was not found.';

        case 409:
            return 'The request conflicts with the current data.';

        case 413:
            return 'The uploaded file is too large.';

        case 429:
            return 'Too many requests. Please try again later.';

        case 500:
            return 'The server could not complete the request.';

        default:
            return `Request failed with status ${status}.`;
    }
}
// =========================================================
// TOKEN MANAGER
// =========================================================
//
// PURPOSE:
//
// Final authenticated JWT token-a frontend-la
// manage panna indha helper use pannuvom.
//
// IMPORTANT:
//
// Final Access JWT:
// localStorage -> pdv_token
//
// Share invitation token:
// sessionStorage
//
// rendu mix panna koodathu.
//
// Security Note:
//
// localStorage completely XSS-proof illa.
// Aana current university project architecture-la
// final JWT storage-ku localStorage frozen choice.
//
// Temporary MFA token / share token inga store panna maatom.
// =========================================================


// =========================================================
// TOKEN STORAGE KEY
// =========================================================

const TOKEN_KEY =
    'pdv_token';


// =========================================================
// GET TOKEN
// =========================================================
//
// Function:
// Browser localStorage-lendhu final JWT read pannum.
//
// Input:
// None.
//
// Output:
// JWT string
// or
// null.
//
// Reason:
// apiClient Authorization header-ku token venum.
function getToken() {
    const token =
        localStorage.getItem(
            TOKEN_KEY);


    if (!token ||
        token.trim() === '') {
        return null;
    }


    return token;
}


// =========================================================
// SET TOKEN
// =========================================================
//
// Function:
// Successful password + TOTP login mudinja piragu
// backend return pannura FINAL JWT mattum store pannum.
//
// Input:
// token
//
// Output:
// None.
//
// Security:
// null / empty token save panna koodathu.
function setToken(token) {
    if (!token ||
        typeof token !== 'string' ||
        token.trim() === '') {
        throw new Error(
            'A valid access token is required.');
    }


    localStorage.setItem(
        TOKEN_KEY,
        token.trim());
}


// =========================================================
// CLEAR TOKEN
// =========================================================
//
// Function:
// Logout / expired JWT / unauthorized response
// vandha token remove pannum.
//
// Input:
// None.
//
// Output:
// None.
function clearToken() {
    localStorage.removeItem(
        TOKEN_KEY);
}


// =========================================================
// CHECK TOKEN EXISTS
// =========================================================
//
// Function:
// Final JWT browser-la irukkaa check pannum.
//
// Output:
// true / false.
function hasToken() {
    return getToken() !== null;
}


// =========================================================
// READ JWT PAYLOAD
// =========================================================
//
// Function:
// JWT middle payload section-a decode pannum.
//
// IMPORTANT:
//
// Idhu authorization security check illa.
//
// Frontend token expiry check / UI helper-ku mattum.
//
// Actual authorization ALWAYS backend controller/service
// decide pannum.
function getPayload() {
    const token =
        getToken();


    if (!token) {
        return null;
    }


    try {
        const parts =
            token.split('.');


        if (parts.length !== 3) {
            return null;
        }


        // JWT Base64Url format use pannum.
        let base64 =
            parts[1]
                .replace(/-/g, '+')
                .replace(/_/g, '/');


        // Base64 length multiple of 4 aaganum.
        while (base64.length % 4 !== 0) {
            base64 += '=';
        }


        const json =
            decodeURIComponent(
                atob(base64)
                    .split('')
                    .map(
                        character =>
                            '%' +
                            character
                                .charCodeAt(0)
                                .toString(16)
                                .padStart(2, '0'))
                    .join(''));


        return JSON.parse(
            json);
    }
    catch {
        return null;
    }
}


// =========================================================
// TOKEN EXPIRY CHECK
// =========================================================
//
// Function:
// JWT exp claim check pannum.
//
// Output:
//
// true  -> expired / invalid
// false -> token innum valid time range-la irukku.
//
// IMPORTANT:
//
// Backend token validation still final authority.
function isExpired() {
    const payload =
        getPayload();


    if (!payload ||
        typeof payload.exp !== 'number') {
        return true;
    }


    const currentUnixTime =
        Math.floor(
            Date.now() / 1000);


    return payload.exp <=
        currentUnixTime;
}


// =========================================================
// PUBLIC TOKEN MANAGER
// =========================================================

export const tokenManager =
{
    get:
        getToken,

    set:
        setToken,

    clear:
        clearToken,

    has:
        hasToken,

    getPayload:
        getPayload,

    isExpired:
        isExpired
};
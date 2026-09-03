import {
    api
} from './apiClient.js';


// =========================================================
// AUTH API
// =========================================================
//
// PURPOSE:
//
// Authentication related backend endpoints
// ellathayum ore place-la maintain pannuvom.
//
// Page JS direct fetch() use panna vendaam.
//
// Page JS
//    ↓
// authApi
//    ↓
// apiClient
//    ↓
// ASP.NET Core AuthController
//
// =========================================================

export const authApi =
{
    // =====================================================
    // 1. REGISTER
    // =====================================================
    //
    // Input:
    // fullName + email + password
    //
    register:
        data =>
            api(
                '/auth/register',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 2. VERIFY EMAIL OTP
    // =====================================================

    verifyEmail:
        data =>
            api(
                '/auth/verify-email',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 3. RESEND EMAIL OTP
    // =====================================================

    resendEmailOtp:
        data =>
            api(
                '/auth/resend-email-otp',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 4. FORGOT PASSWORD
    // =====================================================
    //
    // API:
    // POST /api/auth/forgot-password
    //
    // Input:
    //
    // {
    //     email
    // }
    //
    // Output:
    // Generic response.
    //
    // Security:
    // Frontend account irukka / illaya
    // identify panna koodathu.
    //
    forgotPassword:
        data =>
            api(
                '/auth/forgot-password',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 5. RESET PASSWORD
    // =====================================================
    //
    // API:
    // POST /api/auth/reset-password
    //
    // Input:
    //
    // {
    //     token,
    //     newPassword,
    //     confirmPassword
    // }
    //
    // Output:
    // Password reset success / generic failure.
    //
    resetPassword:
        data =>
            api(
                '/auth/reset-password',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 6. SETUP TOTP
    // =====================================================

    setupTotp:
        data =>
            api(
                '/auth/setup-totp',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 7. VERIFY TOTP SETUP
    // =====================================================

    verifyTotpSetup:
        data =>
            api(
                '/auth/verify-totp-setup',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 8. LOGIN PASSWORD STEP
    // =====================================================
    //
    // Normal User:
    // Password -> MFA challenge
    //
    // Admin:
    // Password -> Final JWT
    //
    login:
        data =>
            api(
                '/auth/login',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 9. VERIFY LOGIN TOTP
    // =====================================================

    verifyLoginTotp:
        data =>
            api(
                '/auth/verify-login-totp',
                {
                    method:
                        'POST',

                    body:
                        JSON.stringify(
                            data)
                }),


    // =====================================================
    // 10. LOGOUT
    // =====================================================

    logout:
        () =>
            api(
                '/auth/logout',
                {
                    method:
                        'POST'
                })
};
export const STORAGE_KEYS = {
  accessToken: 'pdv_token',
  registrationEmail: 'pdv_registration_email',
  totpSetupToken: 'pdv_totp_setup_token',
  totpSetupResponse: 'pdv_totp_setup_response',
  loginMfaChallenge: 'pdv_login_mfa_challenge',
  loginEmail: 'pdv_login_email',
  postLoginRedirect: 'pdv_post_login_redirect',
  pendingShareToken: 'pdv_pending_share_token',
  shareSuccess: 'pdv_share_success'
} as const;

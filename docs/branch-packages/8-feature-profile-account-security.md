# Angular profile and account-security migration (Phase 8)

Branch: `feature/profile-account-security`
Merge target: `development`

Scope: authenticated profile read/update through the real `/api/profile` contract, server-owned account security metadata, responsive Angular profile/security UI, and legacy profile URL compatibility.

Security boundary: only safe account metadata is returned. Password hashes, OTP values/hashes, password-reset tokens, TOTP encrypted secrets, and other authentication secrets remain excluded. Email, role, account state and MFA state are read-only in this phase.

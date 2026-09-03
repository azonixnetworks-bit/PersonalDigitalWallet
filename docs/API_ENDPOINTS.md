# 24 Frozen API Endpoints
1. POST /api/auth/register
2. POST /api/auth/login
3. POST /api/auth/logout
4. GET /api/profile
5. PUT /api/profile
6. GET /api/folders
7. POST /api/folders
8. PUT /api/folders/{id}
9. DELETE /api/folders/{id}
10. POST /api/documents/upload
11. GET /api/documents
12. GET /api/documents/{id}
13. GET /api/documents/{id}/download
14. PUT /api/documents/{id}
15. DELETE /api/documents/{id}
16. POST /api/credentials
17. GET /api/credentials
18. GET /api/credentials/{id}
19. PUT /api/credentials/{id}
20. DELETE /api/credentials/{id}
21. GET /api/search?keyword=
22. GET /api/admin/dashboard
23. GET /api/admin/users
24. PUT /api/admin/users/{id}/status

## User Dashboard V2

- `GET /api/dashboard` — authenticated user vault summary, quota, safe billing lifecycle metadata, security state, recent documents and folders.

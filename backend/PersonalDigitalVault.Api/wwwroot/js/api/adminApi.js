import {
    api
} from './apiClient.js';


// =========================================================
// ADMIN API
// =========================================================
//
// PURPOSE:
//
// Administrator frontend direct fetch() use pannaama
// common apiClient moolama backend AdminController
// endpoints call pannum.
//
// SECURITY:
//
// Ella endpoint-um backend-la:
// [Authorize(Roles = "Admin")]
//
// Frontend Admin check convenience mattum.
// Backend authorization dhaan final security authority.
//
// Admin APIs:
// 1. GET /api/admin/dashboard
// 2. GET /api/admin/users
// 3. PUT /api/admin/users/{id}/status
//
// IMPORTANT:
//
// Admin-ku private:
//
// ❌ document download
// ❌ document preview
// ❌ credential reveal
// ❌ AES key
// ❌ StoragePath
// ❌ FileHash
//
// endpoint inga create panna maatom.
// =========================================================


export const adminApi =
{
    // =====================================================
    // ADMIN DASHBOARD
    // =====================================================
    //
    // API:
    // GET /api/admin/dashboard
    //
    // OUTPUT:
    //
    // {
    //     totalUsers,
    //     totalUploads,
    //     totalStoredFiles,
    //     recentUploads
    // }
    //
    dashboard:
        () =>
            api(
                '/admin/dashboard'
            ),


    // =====================================================
    // USER LIST
    // =====================================================
    //
    // API:
    // GET /api/admin/users
    //
    // OUTPUT:
    //
    // [
    //     {
    //         id,
    //         fullName,
    //         email,
    //         role,
    //         isActive
    //     }
    // ]
    users:
        () =>
            api(
                '/admin/users'
            ),


    // =====================================================
    // ENABLE / DISABLE USER
    // =====================================================
    //
    // API:
    //
    // PUT /api/admin/users/{id}/status
    //
    // INPUT:
    //
    // id
    // isActive
    //
    // BODY:
    //
    // {
    //     isActive: true / false
    // }
    //
    // SECURITY:
    //
    // Backend AdminService rejects Admin account
    // status modification.
    setStatus:
        (
            id,
            isActive
        ) => {
            const userId =
                Number(id);


            if (
                !Number.isInteger(userId)
                ||
                userId <= 0
            ) {
                throw new Error(
                    'Invalid user.'
                );
            }


            if (
                typeof isActive !==
                'boolean'
            ) {
                throw new Error(
                    'Invalid user status.'
                );
            }


            return api(
                `/admin/users/${userId}/status`,
                {
                    method:
                        'PUT',

                    body:
                        JSON.stringify(
                            {
                                isActive
                            }
                        )
                }
            );
        }
};
import {
    api
} from './apiClient.js';


// =========================================================
// ADMIN SUBSCRIPTION API
// =========================================================
//
// JWT apiClient automatic-aa attach pannum.
//
// Backend final security:
//
// [Authorize(Roles = "Admin")]
//
export const adminSubscriptionApi = {

    // Billing summary.
    summary: () =>
        api(
            '/admin/subscriptions/summary'
        ),


    // Safe billing metadata list.
    all: () =>
        api(
            '/admin/subscriptions'
        )
};
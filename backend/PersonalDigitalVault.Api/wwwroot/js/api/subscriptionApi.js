import {
    api
} from './apiClient.js';


// =========================================================
// SUBSCRIPTION API
// =========================================================
//
// Function:
// Frontend subscription page-lendhu
// SubscriptionController endpoints call pannum.
//
// Flow:
//
// subscription.js
//      ↓
// subscriptionApi.js
//      ↓
// apiClient.js
//      ↓
// SubscriptionController
//
// IMPORTANT:
// JWT token manually inga add panna thevai illa.
//
// Existing apiClient.js automatic-aa:
//
// Authorization: Bearer <JWT>
//
// add pannum.
// =========================================================

export const subscriptionApi = {

    // =====================================================
    // GET PAYPAL CONFIG
    // =====================================================
    //
    // API:
    // GET /api/subscriptions/config
    //
    // Output:
    //
    // {
    //   clientId,
    //   planId,
    //   planName,
    //   environment,
    //   billingReference
    // }
    //
    // ClientSecret inga varakoodathu.
    getConfig: () =>
        api(
            '/subscriptions/config'
        ),


    // =====================================================
    // GET CURRENT USER SUBSCRIPTION
    // =====================================================
    //
    // API:
    // GET /api/subscriptions/me
    //
    // Output:
    //
    // {
    //   hasSubscription: true/false,
    //   subscription: {...} / null
    // }
    getMine: () =>
        api(
            '/subscriptions/me'
        ),


    // =====================================================
    // CONFIRM PAYPAL SUBSCRIPTION
    // =====================================================
    //
    // Input:
    // PayPal Subscription ID
    //
    // Example:
    // I-ABC123XYZ
    //
    // Frontend success-a direct trust pannaama
    // backend-ku anuppuvom.
    //
    // Backend PayPal API-la verify pannum.
    confirm: (
        subscriptionId
    ) =>
        api(
            '/subscriptions/confirm',
            {
                method:
                    'POST',

                body:
                    JSON.stringify(
                        {
                            subscriptionId:
                                subscriptionId
                        }
                    )
            }
        ),


    // =====================================================
    // CANCEL CURRENT SUBSCRIPTION
    // =====================================================
    //
    // Body:
    // None.
    //
    // Security:
    // Frontend subscription ID send pannaathu.
    //
    // Backend JWT current UserId use panni
    // own subscription mattum cancel pannum.
    cancel: () =>
        api(
            '/subscriptions/cancel',
            {
                method:
                    'POST'
            }
        )
};
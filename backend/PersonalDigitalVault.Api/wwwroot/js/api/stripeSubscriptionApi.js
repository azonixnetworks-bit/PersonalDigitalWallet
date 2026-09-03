import { api } from './apiClient.js';

export const stripeSubscriptionApi = {
    getConfig: () =>
        api('/stripe/subscriptions/config'),

    getMine: () =>
        api('/stripe/subscriptions/me'),

    createCheckoutSession: () =>
        api('/stripe/subscriptions/checkout-session', {
            method: 'POST'
        }),

    verifyCheckout: (sessionId) =>
        api('/stripe/subscriptions/verify-checkout', {
            method: 'POST',
            body: JSON.stringify({ sessionId })
        }),

    createPortalSession: () =>
        api('/stripe/subscriptions/portal-session', {
            method: 'POST'
        })
};

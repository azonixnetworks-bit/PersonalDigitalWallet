import {
    requireAuth
} from '../auth/authGuard.js';

import {
    tokenManager
} from '../auth/tokenManager.js';

import {
    subscriptionApi
} from '../api/subscriptionApi.js';


// =========================================================
// SUBSCRIPTION PAGE
// =========================================================
//
// Flow:
//
// User JWT
//   ↓
// Load PayPal config + current subscription
//   ↓
// Premium active?
//
// YES -> status show
//
// NO -> PayPal SDK
//        ↓
//      Subscribe
//        ↓
//      Backend verify
//        ↓
//      Premium
// =========================================================


// =========================================================
// HTML ELEMENTS
// =========================================================

const messageBox =
    document.getElementById(
        'subscriptionMessage'
    );


const environmentBadge =
    document.getElementById(
        'environmentBadge'
    );


const currentPlanName =
    document.getElementById(
        'currentPlanName'
    );


const currentStatus =
    document.getElementById(
        'currentStatus'
    );


const subscriptionDetails =
    document.getElementById(
        'subscriptionDetails'
    );


const premiumPlanName =
    document.getElementById(
        'premiumPlanName'
    );


const paypalSection =
    document.getElementById(
        'paypalSection'
    );


const paypalLoadingText =
    document.getElementById(
        'paypalLoadingText'
    );


const paypalButtonContainer =
    document.getElementById(
        'paypal-button-container'
    );


const premiumActiveBox =
    document.getElementById(
        'premiumActiveBox'
    );


const cancelSubscriptionButton =
    document.getElementById(
        'cancelSubscriptionButton'
    );


// =========================================================
// PAGE STATE
// =========================================================

let paypalConfig =
    null;


let currentSubscription =
    null;


let paypalButtonsRendered =
    false;


// =========================================================
// CANCEL BUTTON EVENT
// =========================================================

cancelSubscriptionButton
    ?.addEventListener(
        'click',
        cancelCurrentSubscription
    );


// =========================================================
// AUTH + PAGE START
// =========================================================
//
// IMPORTANT FIX:
//
// DOM variables ellam first initialize aana piragu
// dhaan page load function call pannuvom.
//
requireAuth();


const token =
    tokenManager.get();


if (token) {

    initializeSubscriptionPage();
}


// =========================================================
// INITIALIZE PAGE
// =========================================================

async function initializeSubscriptionPage() {

    try {

        showMessage(
            'Loading your subscription...',
            'info'
        );


        // =============================================
        // CONFIG + CURRENT SUBSCRIPTION
        // =============================================

        const results =
            await Promise.all(
                [
                    subscriptionApi
                        .getConfig(),

                    subscriptionApi
                        .getMine()
                ]
            );


        paypalConfig =
            results[0];


        const mineResponse =
            results[1];


        // =============================================
        // CONFIG UI
        // =============================================

        environmentBadge.textContent =
            paypalConfig?.environment
            ||
            'Sandbox';


        premiumPlanName.textContent =
            paypalConfig?.planName
            ||
            'Premium Monthly';


        // =============================================
        // CURRENT SUBSCRIPTION
        // =============================================

        currentSubscription =
            mineResponse?.subscription
            ??
            null;


        renderCurrentSubscription(
            currentSubscription
        );


        clearMessage();


        // =============================================
        // PREMIUM ALREADY ACTIVE?
        // =============================================

        if (
            currentSubscription
            &&
            currentSubscription
                .isPremium === true
        ) {

            showPremiumActiveState();

            return;
        }


        // =============================================
        // FREE / INACTIVE
        // =============================================

        await preparePayPalCheckout();

    }
    catch (error) {

        showMessage(
            error?.message
            ||
            'Subscription information could not be loaded.',
            'error'
        );


        paypalLoadingText.textContent =
            'PayPal checkout is currently unavailable.';
    }
}


// =========================================================
// PREPARE PAYPAL CHECKOUT
// =========================================================

async function preparePayPalCheckout() {

    if (!paypalConfig) {

        return;
    }


    // ClientId + PlanId + current user binding
    // backend config-lendhu varanum.
    if (
        !paypalConfig.clientId
        ||
        !paypalConfig.planId
        ||
        !paypalConfig.billingReference
    ) {

        showMessage(
            'PayPal subscription configuration is incomplete.',
            'error'
        );

        return;
    }


    paypalSection.hidden =
        false;


    premiumActiveBox.hidden =
        true;


    paypalLoadingText.textContent =
        'Loading secure PayPal checkout...';


    try {

        await loadPayPalSdk(
            paypalConfig.clientId
        );


        await renderPayPalButtons();

    }
    catch {

        paypalLoadingText.textContent =
            'PayPal checkout could not be loaded.';


        showMessage(
            'Unable to connect to PayPal checkout.',
            'error'
        );
    }
}


// =========================================================
// LOAD PAYPAL SDK
// =========================================================
//
// IMPORTANT:
//
// Client ID browser-la use panna okay.
//
// ClientSecret browser-ku varave koodathu.
//
function loadPayPalSdk(
    clientId
) {

    return new Promise(
        (
            resolve,
            reject
        ) => {

            // Already ready.
            if (
                window.paypal
                &&
                window.paypal.Buttons
            ) {

                resolve();

                return;
            }


            const existingScript =
                document.getElementById(
                    'paypal-sdk-script'
                );


            if (existingScript) {

                existingScript
                    .addEventListener(
                        'load',
                        () => resolve(),
                        {
                            once: true
                        }
                    );


                existingScript
                    .addEventListener(
                        'error',
                        () =>
                            reject(
                                new Error(
                                    'PayPal SDK failed to load.'
                                )
                            ),
                        {
                            once: true
                        }
                    );


                return;
            }


            const script =
                document.createElement(
                    'script'
                );


            script.id =
                'paypal-sdk-script';


            script.src =
                'https://www.paypal.com/sdk/js'
                +
                '?client-id='
                +
                encodeURIComponent(
                    clientId
                )
                +
                '&components=buttons'
                +
                '&vault=true'
                +
                '&intent=subscription';


            script.async =
                true;


            script.onload =
                () =>
                    resolve();


            script.onerror =
                () =>
                    reject(
                        new Error(
                            'PayPal SDK failed to load.'
                        )
                    );


            document.head
                .appendChild(
                    script
                );
        }
    );
}


// =========================================================
// RENDER PAYPAL BUTTON
// =========================================================

async function renderPayPalButtons() {

    if (
        paypalButtonsRendered
        ||
        !paypalConfig
        ||
        !window.paypal
    ) {

        return;
    }


    paypalButtonContainer
        .replaceChildren();


    paypalLoadingText.textContent =
        '';


    const buttons =
        window.paypal.Buttons(
            {
                // =====================================
                // CREATE PAYPAL SUBSCRIPTION
                // =====================================

                createSubscription(
                    data,
                    actions
                ) {

                    return actions
                        .subscription
                        .create(
                            {
                                plan_id:
                                    paypalConfig
                                        .planId,

                                // User binding.
                                //
                                // Example:
                                // PDV-USER-8
                                custom_id:
                                    paypalConfig
                                        .billingReference
                            }
                        );
                },


                // =====================================
                // APPROVED
                // =====================================

                async onApprove(
                    data
                ) {

                    const subscriptionId =
                        data?.subscriptionID;


                    if (!subscriptionId) {

                        showMessage(
                            'PayPal did not return a subscription ID.',
                            'error'
                        );

                        return;
                    }


                    showMessage(
                        'PayPal approved. Verifying your subscription securely...',
                        'info'
                    );


                    try {

                        await confirmSubscriptionWithBackend(
                            subscriptionId
                        );

                    }
                    catch (error) {

                        showMessage(
                            error?.message
                            ||
                            'Your subscription could not be verified.',
                            'error'
                        );
                    }
                },


                // =====================================
                // USER CANCELLED PAYPAL WINDOW
                // =====================================

                onCancel() {

                    showMessage(
                        'PayPal checkout was cancelled. No Premium access was activated.',
                        'info'
                    );
                },


                // =====================================
                // PAYPAL ERROR
                // =====================================

                onError() {

                    // Raw PayPal error details
                    // browser-la expose/log panna maatom.
                    showMessage(
                        'PayPal checkout encountered an error. Please try again.',
                        'error'
                    );
                }
            }
        );


    await buttons.render(
        '#paypal-button-container'
    );


    paypalButtonsRendered =
        true;
}


// =========================================================
// BACKEND CONFIRMATION
// =========================================================
//
// Browser approval mattum Premium authority illa.
//
// Backend:
// Subscription ID
//    ↓
// PayPal GET
//    ↓
// Plan check
// custom_id check
// ACTIVE check
//    ↓
// Database
//
async function confirmSubscriptionWithBackend(
    subscriptionId
) {

    let lastError =
        null;


    for (
        let attempt = 1;
        attempt <= 3;
        attempt++
    ) {

        try {

            const response =
                await subscriptionApi
                    .confirm(
                        subscriptionId
                    );


            const subscription =
                response?.subscription;


            if (!subscription) {

                throw new Error(
                    'Subscription verification failed.'
                );
            }


            currentSubscription =
                subscription;


            renderCurrentSubscription(
                currentSubscription
            );


            showPremiumActiveState();


            showMessage(
                'Premium subscription activated successfully.',
                'success'
            );


            return;

        }
        catch (error) {

            lastError =
                error;


            if (attempt === 3) {

                break;
            }


            await delay(
                1500
            );
        }
    }


    throw (
        lastError
        ??
        new Error(
            'Subscription verification failed.'
        )
    );
}


// =========================================================
// CANCEL SUBSCRIPTION
// =========================================================

async function cancelCurrentSubscription() {

    if (
        !currentSubscription
        ||
        currentSubscription
            .isPremium !== true
    ) {

        showMessage(
            'There is no active Premium subscription to cancel.',
            'info'
        );

        return;
    }


    const confirmed =
        window.confirm(
            'Are you sure you want to cancel your Premium subscription?'
        );


    if (!confirmed) {

        return;
    }


    cancelSubscriptionButton.disabled =
        true;


    cancelSubscriptionButton.textContent =
        'Cancelling...';


    try {

        await subscriptionApi
            .cancel();


        showMessage(
            'Subscription cancelled successfully.',
            'success'
        );


        const mineResponse =
            await subscriptionApi
                .getMine();


        currentSubscription =
            mineResponse?.subscription
            ??
            null;


        renderCurrentSubscription(
            currentSubscription
        );


        paypalSection.hidden =
            false;


        premiumActiveBox.hidden =
            true;


        cancelSubscriptionButton.hidden =
            true;


        paypalButtonsRendered =
            false;


        paypalButtonContainer
            .replaceChildren();


        await renderPayPalButtons();

    }
    catch (error) {

        showMessage(
            error?.message
            ||
            'Subscription could not be cancelled.',
            'error'
        );
    }
    finally {

        cancelSubscriptionButton.disabled =
            false;


        cancelSubscriptionButton.textContent =
            'Cancel Subscription';
    }
}


// =========================================================
// RENDER CURRENT SUBSCRIPTION
// =========================================================

function renderCurrentSubscription(
    subscription
) {

    // =============================================
    // FREE USER
    // =============================================

    if (!subscription) {

        currentPlanName.textContent =
            'Free Plan';


        setStatusBadge(
            'FREE'
        );


        subscriptionDetails.innerHTML =
            `
                <div class="detail-item">
                    <span>Plan</span>
                    <strong>Free</strong>
                </div>

                <div class="detail-item">
                    <span>Billing</span>
                    <strong>No recurring billing</strong>
                </div>

                <div class="detail-item">
                    <span>Premium</span>
                    <strong>Not active</strong>
                </div>
            `;


        cancelSubscriptionButton.hidden =
            true;


        return;
    }


    // =============================================
    // EXISTING SUBSCRIPTION
    // =============================================

    currentPlanName.textContent =
        subscription.planName
        ||
        'Premium Monthly';


    setStatusBadge(
        subscription.status
    );


    subscriptionDetails.innerHTML =
        `
            <div class="detail-item">
                <span>Status</span>
                <strong>${escapeHtml(
            subscription.status
            ||
            'Unknown'
        )}</strong>
            </div>

            <div class="detail-item">
                <span>Provider</span>
                <strong>${escapeHtml(
            subscription.provider
            ||
            'PayPal'
        )}</strong>
            </div>

            <div class="detail-item">
                <span>Start Date</span>
                <strong>${formatDate(
            subscription.startDate
        )}</strong>
            </div>

            <div class="detail-item">
                <span>Next Billing</span>
                <strong>${formatDate(
            subscription.nextBillingDate
        )}</strong>
            </div>

            <div class="detail-item">
                <span>PayPal Subscription</span>
                <strong>${escapeHtml(
            subscription
                .payPalSubscriptionId
            ||
            '-'
        )}</strong>
            </div>

            <div class="detail-item">
                <span>Last Verified</span>
                <strong>${formatDate(
            subscription.lastVerifiedAt
        )}</strong>
            </div>
        `;


    cancelSubscriptionButton.hidden =
        subscription.isPremium !== true;
}


// =========================================================
// PREMIUM ACTIVE UI
// =========================================================

function showPremiumActiveState() {

    paypalSection.hidden =
        true;


    premiumActiveBox.hidden =
        false;


    cancelSubscriptionButton.hidden =
        false;


    paypalButtonContainer
        .replaceChildren();
}


// =========================================================
// STATUS BADGE
// =========================================================

function setStatusBadge(
    status
) {

    const normalized =
        String(
            status
            ||
            ''
        )
            .trim()
            .toUpperCase();


    currentStatus.textContent =
        normalized
        ||
        'UNKNOWN';


    currentStatus.className =
        'status-badge';


    if (normalized === 'ACTIVE') {

        currentStatus.classList
            .add(
                'status-active'
            );

        return;
    }


    if (normalized === 'CANCELLED') {

        currentStatus.classList
            .add(
                'status-cancelled'
            );

        return;
    }


    if (normalized === 'SUSPENDED') {

        currentStatus.classList
            .add(
                'status-suspended'
            );

        return;
    }


    currentStatus.classList
        .add(
            'status-neutral'
        );
}


// =========================================================
// DATE FORMAT
// =========================================================

function formatDate(
    value
) {

    if (!value) {

        return '-';
    }


    const date =
        new Date(
            value
        );


    if (
        Number.isNaN(
            date.getTime()
        )
    ) {

        return '-';
    }


    return date
        .toLocaleString();
}


// =========================================================
// SAFE HTML
// =========================================================

function escapeHtml(
    value
) {

    return String(
        value
        ??
        ''
    )
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}


// =========================================================
// MESSAGE
// =========================================================

function showMessage(
    message,
    type
) {

    messageBox.hidden =
        false;


    messageBox.textContent =
        message;


    messageBox.className =
        `subscription-message ${type}`;
}


// =========================================================
// CLEAR MESSAGE
// =========================================================

function clearMessage() {

    messageBox.hidden =
        true;


    messageBox.textContent =
        '';


    messageBox.className =
        'subscription-message';
}


// =========================================================
// SMALL DELAY
// =========================================================

function delay(
    milliseconds
) {

    return new Promise(
        resolve =>
            window.setTimeout(
                resolve,
                milliseconds
            )
    );
}
import { requireAuth } from '../auth/authGuard.js';
import { tokenManager } from '../auth/tokenManager.js';
import { stripeSubscriptionApi } from '../api/stripeSubscriptionApi.js';

const messageBox = document.getElementById('subscriptionMessage');
const environmentBadge = document.getElementById('environmentBadge');
const currentPlanName = document.getElementById('currentPlanName');
const currentStatus = document.getElementById('currentStatus');
const subscriptionDetails = document.getElementById('subscriptionDetails');
const premiumPlanName = document.getElementById('premiumPlanName');
const premiumPriceText = document.getElementById('premiumPriceText');
const premiumIntervalText = document.getElementById('premiumIntervalText');
const stripeSection = document.getElementById('stripeSection');
const stripeLoadingText = document.getElementById('stripeLoadingText');
const stripeCheckoutButton = document.getElementById('stripeCheckoutButton');
const premiumActiveBox = document.getElementById('premiumActiveBox');
const manageBillingButton = document.getElementById('manageBillingButton');

let currentSubscription = null;

stripeCheckoutButton?.addEventListener('click', startStripeCheckout);
manageBillingButton?.addEventListener('click', openStripePortal);

requireAuth();

if (tokenManager.get()) {
    initializeSubscriptionPage();
}

async function initializeSubscriptionPage() {
    try {
        const checkoutState = new URLSearchParams(window.location.search).get('checkout');
        const checkoutSessionId = new URLSearchParams(window.location.search).get('session_id');

        if (checkoutState === 'cancelled') {
            showMessage('Stripe Checkout was cancelled. No Premium access was activated.', 'info');
            clearCheckoutQuery();
        }

        if (checkoutState === 'success' && checkoutSessionId) {
            showMessage('Stripe payment completed. Verifying your subscription securely...', 'info');

            try {
                await stripeSubscriptionApi.verifyCheckout(checkoutSessionId);
                showMessage('Premium subscription verified successfully.', 'success');
            }
            catch (error) {
                // Webhook delivery can finish the synchronization even when this
                // immediate verification request is temporarily unavailable.
                showMessage(
                    error?.message || 'Payment completed. Subscription synchronization is still pending.',
                    'info'
                );
            }

            clearCheckoutQuery();
        }
        else if (checkoutState !== 'cancelled') {
            showMessage('Loading your subscription...', 'info');
        }

        const [config, mineResponse] = await Promise.all([
            stripeSubscriptionApi.getConfig(),
            stripeSubscriptionApi.getMine()
        ]);

        environmentBadge.textContent = `${config?.provider || 'Stripe'} · ${config?.environment || 'Sandbox'}`;
        premiumPlanName.textContent = config?.planName || 'Premium Monthly';
        renderConfiguredPrice(config);

        currentSubscription = mineResponse?.subscription ?? null;
        renderCurrentSubscription(currentSubscription);
        renderActions(currentSubscription);

        if (checkoutState !== 'success' && checkoutState !== 'cancelled') {
            clearMessage();
        }
    }
    catch (error) {
        showMessage(
            error?.message || 'Subscription information could not be loaded.',
            'error'
        );

        if (stripeLoadingText) {
            stripeLoadingText.textContent = 'Stripe billing is currently unavailable.';
        }
    }
}

async function startStripeCheckout() {
    setButtonBusy(stripeCheckoutButton, true, 'Opening Stripe Checkout...');

    try {
        const result = await stripeSubscriptionApi.createCheckoutSession();

        if (!result?.checkoutUrl) {
            throw new Error('Stripe Checkout URL was not returned.');
        }

        window.location.assign(result.checkoutUrl);
    }
    catch (error) {
        setButtonBusy(stripeCheckoutButton, false, 'Upgrade with Stripe');
        showMessage(error?.message || 'Stripe Checkout could not be started.', 'error');
    }
}

async function openStripePortal() {
    setButtonBusy(manageBillingButton, true, 'Opening billing portal...');

    try {
        const result = await stripeSubscriptionApi.createPortalSession();

        if (!result?.portalUrl) {
            throw new Error('Stripe billing portal URL was not returned.');
        }

        window.location.assign(result.portalUrl);
    }
    catch (error) {
        setButtonBusy(manageBillingButton, false, 'Manage Billing');
        showMessage(error?.message || 'Stripe Customer Portal could not be opened.', 'error');
    }
}

function renderConfiguredPrice(config) {
    if (!premiumPriceText || !premiumIntervalText) {
        return;
    }

    const currency = String(config?.currency || '').toUpperCase();
    const amount = Number(config?.unitAmount);
    const interval = String(config?.billingInterval || '').toLowerCase();
    const intervalCount = Number(config?.billingIntervalCount || 1);

    if (currency && Number.isFinite(amount)) {
        try {
            const formatter = new Intl.NumberFormat(undefined, {
                style: 'currency',
                currency
            });

            // Stripe amounts are returned in the currency's smallest unit.
            // Intl gives the normal ISO decimal count; Stripe keeps ISK/UGX
            // charge amounts in two-decimal legacy representation.
            const intlDigits = formatter.resolvedOptions().maximumFractionDigits;
            const stripeDigits = ['ISK', 'UGX'].includes(currency) ? 2 : intlDigits;
            const divisor = 10 ** stripeDigits;

            premiumPriceText.textContent = formatter.format(amount / divisor);
        }
        catch {
            premiumPriceText.textContent = `${currency} ${amount}`;
        }
    }
    else {
        premiumPriceText.textContent = 'Stripe recurring billing';
    }

    if (interval) {
        premiumIntervalText.textContent = intervalCount > 1
            ? `Billed every ${intervalCount} ${interval}s`
            : `Billed every ${interval}`;
    }
    else {
        premiumIntervalText.textContent = 'Recurring price verified from Stripe';
    }
}

function renderActions(subscription) {
    const isPremium = subscription?.isPremium === true;
    const status = String(subscription?.status || '').toUpperCase();
    const billingProblem = ['PAST_DUE', 'UNPAID', 'PAUSED'].includes(status);

    stripeSection.hidden = isPremium || billingProblem;
    premiumActiveBox.hidden = !isPremium;

    manageBillingButton.hidden = !subscription;

    if (isPremium) {
        premiumActiveBox.textContent = subscription?.cancelAtPeriodEnd
            ? '✓ Premium is active until the current billing period ends'
            : '✓ Premium subscription is active';
    }

    if (stripeLoadingText) {
        stripeLoadingText.textContent =
            'Payment details are collected on Stripe-hosted Checkout and never pass through the PDV server.';
    }
}

function renderCurrentSubscription(subscription) {
    if (!subscription) {
        currentPlanName.textContent = 'Free';
        currentStatus.textContent = 'FREE';
        currentStatus.className = 'status-badge status-neutral';

        subscriptionDetails.innerHTML = `
            <div class="detail-item">
                <span>Plan</span>
                <strong>Free</strong>
            </div>
            <div class="detail-item">
                <span>Billing</span>
                <strong>No recurring subscription</strong>
            </div>
        `;
        return;
    }

    const status = String(subscription.status || 'UNKNOWN').toUpperCase();

    currentPlanName.textContent = subscription.planName || 'Premium Monthly';
    currentStatus.textContent = status;
    currentStatus.className = `status-badge ${getStatusClass(status)}`;

    subscriptionDetails.innerHTML = `
        <div class="detail-item">
            <span>Provider</span>
            <strong>${escapeHtml(subscription.provider || 'Stripe')}</strong>
        </div>
        <div class="detail-item">
            <span>Status</span>
            <strong>${escapeHtml(status)}</strong>
        </div>
        <div class="detail-item">
            <span>Next Billing</span>
            <strong>${escapeHtml(formatDate(subscription.nextBillingDate))}</strong>
        </div>
        <div class="detail-item">
            <span>Cancellation</span>
            <strong>${subscription.cancelAtPeriodEnd ? 'Scheduled for period end' : 'Not scheduled'}</strong>
        </div>
    `;
}

function getStatusClass(status) {
    if (status === 'ACTIVE' || status === 'TRIALING') {
        return 'status-active';
    }

    if (status === 'CANCELED' || status === 'INCOMPLETE_EXPIRED') {
        return 'status-cancelled';
    }

    if (['PAST_DUE', 'UNPAID', 'PAUSED', 'INCOMPLETE'].includes(status)) {
        return 'status-suspended';
    }

    return 'status-neutral';
}

function formatDate(value) {
    if (!value) {
        return '-';
    }

    const date = new Date(value);

    return Number.isNaN(date.getTime())
        ? '-'
        : date.toLocaleString();
}

function setButtonBusy(button, busy, text) {
    if (!button) {
        return;
    }

    button.disabled = busy;
    button.textContent = text;
}

function showMessage(message, type = 'info') {
    if (!messageBox) {
        return;
    }

    messageBox.hidden = false;
    messageBox.className = `subscription-message ${type}`;
    messageBox.textContent = message;
}

function clearMessage() {
    if (!messageBox) {
        return;
    }

    messageBox.hidden = true;
    messageBox.textContent = '';
}

function clearCheckoutQuery() {
    const cleanUrl = `${window.location.pathname}${window.location.hash || ''}`;
    window.history.replaceState({}, document.title, cleanUrl);
}

function escapeHtml(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}

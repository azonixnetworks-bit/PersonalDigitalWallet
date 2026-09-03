import {
    requireAuth
} from '../auth/authGuard.js';

import {
    tokenManager
} from '../auth/tokenManager.js';

import {
    authApi
} from '../api/authApi.js';

import {
    adminSubscriptionApi
} from '../api/adminSubscriptionApi.js';


// =========================================================
// ADMIN BILLING PAGE
// =========================================================

const authenticated =
    requireAuth();


const statsContainer =
    document.getElementById(
        'billingStats'
    );


const subscriptionsContainer =
    document.getElementById(
        'billingSubscriptions'
    );


const messageBox =
    document.getElementById(
        'billingMessage'
    );


const logoutButton =
    document.getElementById(
        'adminBillingLogoutButton'
    );


// =========================================================
// PAGE START
// =========================================================

if (authenticated) {

    initializePage();
}


// =========================================================
// INITIALIZE
// =========================================================

async function initializePage() {

    // Frontend convenience role check.
    //
    // Backend [Authorize(Roles="Admin")]
    // dhaan real security.
    const role =
        getCurrentRole();


    if (
        role
        &&
        role.toLowerCase() !==
        'admin'
    ) {

        window.location.replace(
            '/html/dashboard.html'
        );

        return;
    }


    try {

        const results =
            await Promise.all(
                [
                    adminSubscriptionApi
                        .summary(),

                    adminSubscriptionApi
                        .all()
                ]
            );


        renderSummary(
            results[0]
        );


        renderSubscriptions(
            Array.isArray(
                results[1]
            )
                ?
                results[1]
                :
                []
        );

    }
    catch (error) {

        showMessage(
            error.message ||
            'Billing metadata could not be loaded.',
            'error'
        );

    }
}


// =========================================================
// CURRENT ROLE
// =========================================================

function getCurrentRole() {

    const payload =
        tokenManager
            .getPayload();


    if (!payload) {

        return '';
    }


    return String(
        payload.role
        ??
        payload.Role
        ??
        payload[
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
        ]
        ??
        ''
    )
        .trim();
}


// =========================================================
// RENDER SUMMARY
// =========================================================

function renderSummary(
    summary
) {

    statsContainer
        .replaceChildren();


    statsContainer.append(
        createStatCard(
            'Total Users',
            safeNumber(
                summary?.totalUsers),
            'Normal vault users'
        ),

        createStatCard(
            'Premium Users',
            safeNumber(
                summary?.premiumUsers),
            'ACTIVE Premium accounts'
        ),

        createStatCard(
            'Free Users',
            safeNumber(
                summary?.freeUsers),
            'Users without active Premium'
        ),

        createStatCard(
            'Active',
            safeNumber(
                summary?.activeSubscriptions),
            'Active subscription records'
        ),

        createStatCard(
            'Cancelled',
            safeNumber(
                summary?.cancelledSubscriptions),
            'Cancelled subscriptions'
        ),

        createStatCard(
            'Suspended',
            safeNumber(
                summary?.suspendedSubscriptions),
            'Suspended subscriptions'
        ),

        createStatCard(
            'Expired',
            safeNumber(
                summary?.expiredSubscriptions),
            'Expired subscriptions'
        )
    );
}


// =========================================================
// CREATE STAT CARD
// =========================================================

function createStatCard(
    title,
    value,
    description
) {

    const card =
        document.createElement(
            'article'
        );


    card.className =
        'card billing-stat-card';


    const label =
        document.createElement(
            'div'
        );


    label.className =
        'billing-stat-label';


    label.textContent =
        title;


    const number =
        document.createElement(
            'div'
        );


    number.className =
        'billing-stat-number';


    number.textContent =
        String(
            value
        );


    const detail =
        document.createElement(
            'div'
        );


    detail.className =
        'billing-stat-description';


    detail.textContent =
        description;


    card.append(
        label,
        number,
        detail
    );


    return card;
}


// =========================================================
// RENDER SUBSCRIPTIONS
// =========================================================

function renderSubscriptions(
    subscriptions
) {

    subscriptionsContainer
        .replaceChildren();


    if (
        subscriptions.length ===
        0
    ) {

        const row =
            document.createElement(
                'tr'
            );


        const cell =
            document.createElement(
                'td'
            );


        cell.colSpan =
            8;


        cell.textContent =
            'No subscription records found.';


        row.appendChild(
            cell
        );


        subscriptionsContainer
            .appendChild(
                row
            );


        return;
    }


    subscriptions.forEach(
        subscription => {

            const row =
                document.createElement(
                    'tr'
                );


            // =========================================
            // USER
            // =========================================

            appendTextCell(
                row,
                subscription.userName ||
                `User ${subscription.userId ?? '-'}`
            );


            // =========================================
            // MASKED EMAIL
            // =========================================

            appendTextCell(
                row,
                subscription.maskedEmail ||
                '-'
            );


            // =========================================
            // PROVIDER
            // =========================================

            const provider =
                String(
                    subscription.provider ||
                    '-'
                );

            appendTextCell(
                row,
                provider
            );


            // =========================================
            // PLAN
            // =========================================

            appendTextCell(
                row,
                subscription.planName ||
                '-'
            );


            // =========================================
            // STATUS
            // =========================================

            const statusCell =
                document.createElement(
                    'td'
                );


            const statusBadge =
                document.createElement(
                    'span'
                );


            const status =
                String(
                    subscription.status ||
                    'UNKNOWN'
                )
                    .toUpperCase();


            statusBadge.className =
                `billing-status ${getStatusClass(
                    status
                )}`;


            statusBadge.textContent =
                status;


            statusCell.appendChild(
                statusBadge
            );


            row.appendChild(
                statusCell
            );


            // =========================================
            // MASKED PROVIDER ID
            // =========================================

            const providerReference =
                provider.toLowerCase() === 'stripe'
                    ? subscription.stripeSubscriptionReference
                    : subscription.payPalSubscriptionReference;

            appendTextCell(
                row,
                providerReference || '-'
            );


            // =========================================
            // NEXT BILLING
            // =========================================

            appendTextCell(
                row,
                formatDate(
                    subscription.nextBillingDate
                )
            );


            // =========================================
            // LAST VERIFIED
            // =========================================

            appendTextCell(
                row,
                formatDate(
                    subscription.lastVerifiedAt
                )
            );


            subscriptionsContainer
                .appendChild(
                    row
                );
        }
    );
}


// =========================================================
// APPEND SAFE TEXT CELL
// =========================================================
//
// textContent use pannuradhunaala
// HTML injection avoid aagum.
//
function appendTextCell(
    row,
    value
) {

    const cell =
        document.createElement(
            'td'
        );


    cell.textContent =
        String(
            value ?? '-'
        );


    row.appendChild(
        cell
    );
}


// =========================================================
// STATUS CLASS
// =========================================================

function getStatusClass(
    status
) {

    switch (status) {

        case 'ACTIVE':
            return 'billing-active';

        case 'CANCELLED':
            return 'billing-cancelled';

        case 'SUSPENDED':
            return 'billing-suspended';

        case 'EXPIRED':
            return 'billing-expired';

        default:
            return 'billing-neutral';
    }
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
// SAFE NUMBER
// =========================================================

function safeNumber(
    value
) {

    const number =
        Number(
            value ?? 0
        );


    if (
        !Number.isFinite(
            number
        )
    ) {

        return 0;
    }


    return Math.max(
        0,
        Math.trunc(
            number
        )
    );
}


// =========================================================
// SHOW MESSAGE
// =========================================================

function showMessage(
    text,
    type
) {

    messageBox.hidden =
        false;


    messageBox.textContent =
        text;


    messageBox.className =
        `admin-message ${type}`;
}


// =========================================================
// LOGOUT
// =========================================================

logoutButton
    ?.addEventListener(
        'click',
        async () => {

            try {

                await authApi
                    .logout();

            }
            catch {
                // Stateless JWT logout.
                // Local token still clear pannuvom.
            }
            finally {

                tokenManager
                    .clear();


                window.location
                    .replace(
                        '/html/login.html'
                    );
            }
        }
    );
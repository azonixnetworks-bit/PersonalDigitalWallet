import { requireAuth } from '../auth/authGuard.js';
import { initNavbar } from '../components/navbar.js';
import { getDashboard } from '../api/dashboardApi.js';
import { fileSize } from '../utils/formatters.js';

requireAuth();
initNavbar();

const elements = {
    message: document.getElementById('dashboardMessage'),
    greeting: document.getElementById('dashboardGreeting'),
    healthPill: document.getElementById('vaultHealthPill'),
    healthText: document.getElementById('vaultHealthText'),
    folderCount: document.getElementById('folderCount'),
    documentCount: document.getElementById('documentCount'),
    credentialCount: document.getElementById('credentialCount'),
    sharedCount: document.getElementById('sharedCount'),
    storageUsed: document.getElementById('storageUsed'),
    storageLimit: document.getElementById('storageLimit'),
    storageRemaining: document.getElementById('storageRemaining'),
    storagePercentBadge: document.getElementById('storagePercentBadge'),
    storageProgress: document.getElementById('storageProgress'),
    storageBar: document.getElementById('storageBar'),
    documentQuota: document.getElementById('documentQuota'),
    storageWarning: document.getElementById('storageWarning'),
    planBadge: document.getElementById('planBadge'),
    planName: document.getElementById('planName'),
    planMeta: document.getElementById('planMeta'),
    planPrimaryAction: document.getElementById('planPrimaryAction'),
    planSecondaryAction: document.getElementById('planSecondaryAction'),
    recentDocuments: document.getElementById('recentDocuments'),
    recentFolders: document.getElementById('recentFolders'),
    securityBadge: document.getElementById('securityBadge'),
    emailSecurityCheck: document.getElementById('emailSecurityCheck'),
    totpSecurityCheck: document.getElementById('totpSecurityCheck'),
    encryptionSecurityCheck: document.getElementById('encryptionSecurityCheck')
};

function getGreeting() {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 17) return 'Good afternoon';
    return 'Good evening';
}

function formatDate(value) {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    return new Intl.DateTimeFormat(undefined, {
        month: 'short',
        day: 'numeric',
        year: date.getFullYear() !== new Date().getFullYear() ? 'numeric' : undefined,
        hour: 'numeric',
        minute: '2-digit'
    }).format(date);
}

function setMessage(message, type = '') {
    elements.message.textContent = message || '';
    elements.message.className = `dashboard-message ${type}`.trim();
}

function renderSummary(data) {
    elements.folderCount.textContent = data.summary.folderCount;
    elements.documentCount.textContent = data.summary.documentCount;
    elements.credentialCount.textContent = data.summary.credentialCount;
    elements.sharedCount.textContent = data.summary.sharedWithMeCount;
}

function renderStorage(storage) {
    const percent = Math.max(0, Math.min(100, Number(storage.usagePercent) || 0));

    elements.storageUsed.textContent = fileSize(storage.usedBytes || 0);
    elements.storageLimit.textContent = fileSize(storage.limitBytes || 0);
    elements.storageRemaining.textContent = fileSize(storage.remainingBytes || 0);
    elements.storagePercentBadge.textContent = `${percent.toFixed(percent % 1 ? 1 : 0)}% used`;
    elements.storageBar.style.width = `${percent}%`;
    elements.storageProgress.setAttribute('aria-valuenow', String(percent));
    elements.documentQuota.textContent = `${storage.currentDocuments} / ${storage.maxDocuments}`;

    const isNearLimit = percent >= 80 || storage.currentDocuments >= storage.maxDocuments;
    elements.storageWarning.hidden = !isNearLimit;

    if (isNearLimit) {
        elements.storageWarning.textContent = storage.canUpload
            ? 'Your vault is approaching its current plan limit.'
            : 'Your current plan limit has been reached. Upgrade or remove items before uploading more.';
    }
}

function renderPlan(plan) {
    const premium = Boolean(plan.isPremium);
    elements.planBadge.textContent = premium ? 'PREMIUM' : 'FREE';
    elements.planBadge.classList.toggle('premium', premium);
    elements.planName.textContent = plan.planName || (premium ? 'Premium' : 'Free');

    if (premium) {
        const provider = plan.provider || 'Subscription';
        const status = plan.status || 'ACTIVE';
        let meta = `${provider} • ${status}`;

        if (plan.nextBillingDate) {
            meta += plan.cancelAtPeriodEnd
                ? ` • Access until ${formatDate(plan.nextBillingDate)}`
                : ` • Next billing ${formatDate(plan.nextBillingDate)}`;
        }

        elements.planMeta.textContent = meta;
    } else {
        elements.planMeta.textContent = '20 documents • 50 MB storage. Upgrade when you need more space.';
    }

    elements.planPrimaryAction.href = plan.primaryActionUrl || '/html/stripe-subscription.html';
    elements.planPrimaryAction.textContent = plan.primaryActionText || (premium ? 'Manage Billing' : 'Upgrade with Stripe');

    if (plan.secondaryActionUrl && plan.secondaryActionText) {
        elements.planSecondaryAction.hidden = false;
        elements.planSecondaryAction.href = plan.secondaryActionUrl;
        elements.planSecondaryAction.textContent = plan.secondaryActionText;
    } else {
        elements.planSecondaryAction.hidden = true;
    }
}

function renderSecurity(security) {
    const secure = security.overallStatus === 'SECURE';
    elements.securityBadge.textContent = secure ? 'SECURE' : 'ACTION REQUIRED';
    elements.securityBadge.classList.toggle('secure', secure);
    elements.healthPill.classList.toggle('secure', secure);
    elements.healthText.textContent = secure
        ? 'Vault protection active'
        : 'Security action required';

    setSecurityCheck(elements.emailSecurityCheck, security.emailVerified);
    setSecurityCheck(elements.totpSecurityCheck, security.totpEnabled);
    setSecurityCheck(elements.encryptionSecurityCheck, security.encryptedStorageActive);
}

function setSecurityCheck(element, ok) {
    element.classList.toggle('ok', Boolean(ok));
    element.classList.toggle('warning', !ok);
    const icon = element.querySelector('.security-check-icon');
    if (icon) icon.textContent = ok ? '✓' : '!';
}

function renderRecentDocuments(items) {
    elements.recentDocuments.replaceChildren();

    if (!items?.length) {
        const empty = document.createElement('div');
        empty.className = 'dashboard-empty-state';
        empty.innerHTML = '<strong>No documents yet</strong><span>Upload your first file and it will appear here.</span>';
        elements.recentDocuments.append(empty);
        return;
    }

    for (const item of items) {
        const row = document.createElement('a');
        row.className = 'recent-document-row';
        row.href = '/html/documents.html';

        const icon = document.createElement('span');
        icon.className = 'recent-file-icon';
        icon.textContent = '📄';

        const copy = document.createElement('span');
        copy.className = 'recent-file-copy';

        const name = document.createElement('strong');
        name.textContent = item.fileName;

        const meta = document.createElement('span');
        meta.textContent = `${item.folderName || 'Root Vault'} • ${fileSize(item.fileSize || 0)}`;

        copy.append(name, meta);

        const date = document.createElement('time');
        date.dateTime = item.createdAt;
        date.textContent = formatDate(item.createdAt);

        row.append(icon, copy, date);
        elements.recentDocuments.append(row);
    }
}

function renderFolders(items) {
    elements.recentFolders.replaceChildren();

    if (!items?.length) {
        const empty = document.createElement('div');
        empty.className = 'dashboard-empty-state folder-empty';
        empty.innerHTML = '<strong>No folders yet</strong><span>Create folders to keep documents organized.</span>';
        elements.recentFolders.append(empty);
        return;
    }

    for (const item of items) {
        const card = document.createElement('a');
        card.className = 'folder-overview-item';
        card.href = '/html/folders.html';

        const icon = document.createElement('span');
        icon.className = 'folder-overview-icon';
        icon.textContent = '📁';

        const copy = document.createElement('span');
        copy.className = 'folder-overview-copy';

        const name = document.createElement('strong');
        name.textContent = item.name;

        const count = document.createElement('small');
        count.textContent = `${item.documentCount} ${item.documentCount === 1 ? 'document' : 'documents'}`;

        copy.append(name, count);
        card.append(icon, copy);
        elements.recentFolders.append(card);
    }
}

async function loadDashboard() {
    setMessage('');

    try {
        const data = await getDashboard();
        const name = data.user?.fullName?.trim() || 'there';
        elements.greeting.textContent = `${getGreeting()}, ${name} 👋`;

        renderSummary(data);
        renderStorage(data.storage);
        renderPlan(data.subscription);
        renderSecurity(data.security);
        renderRecentDocuments(data.recentDocuments);
        renderFolders(data.recentFolders);
    } catch (error) {
        setMessage(error.message || 'Unable to load your dashboard right now.', 'error');
        elements.healthText.textContent = 'Dashboard unavailable';
        elements.recentDocuments.innerHTML = '<div class="dashboard-empty-state"><strong>Could not load activity</strong><span>Please refresh and try again.</span></div>';
        elements.recentFolders.innerHTML = '<div class="dashboard-empty-state folder-empty"><strong>Could not load folders</strong><span>Please refresh and try again.</span></div>';
    }
}

loadDashboard();

/* Jellysweep Companion Script
 */
(function () {
  'use strict';

  const PLUGIN_NAME = 'Jellysweep';
  const API_BASE = '/Plugins/Jellysweep';
  const LOGO_URL = '/Plugins/Jellysweep/Static/Logo';

  let lastProcessedItemId = null;
  let isProcessing = false;

  function log(...args) {
    console.log(`[${PLUGIN_NAME}]`, ...args);
  }
  function warn(...args) {
    console.warn(`[${PLUGIN_NAME}]`, ...args);
  }
  function error(...args) {
    console.error(`[${PLUGIN_NAME}]`, ...args);
  }

  function waitForApiClient() {
    return new Promise((resolve, reject) => {
      let attempts = 0;
      const maxAttempts = 30;
      function check() {
        if (window.ApiClient && window.ApiClient.accessToken && window.ApiClient.accessToken()) {
          resolve(window.ApiClient);
          return;
        }
        attempts++;
        if (attempts >= maxAttempts) {
          reject(new Error('ApiClient not available'));
          return;
        }
        setTimeout(check, 1000);
      }
      check();
    });
  }

  function isDetailPage() {
    const hash = window.location.hash;
    const path = window.location.pathname;
    return hash.includes('#/details') || /\/details\//i.test(path);
  }

  function getCurrentItemId() {
    const hash = window.location.hash;
    if (hash.includes('#/details')) {
      const parts = hash.split('?');
      if (parts.length > 1) {
        const params = new URLSearchParams(parts[1]);
        return params.get('id');
      }
    }
    const match = window.location.pathname.match(/\/details\/([a-f0-9]+)/i);
    return match ? match[1] : null;
  }

  async function fetchDeletionStatus(itemId) {
    if (!itemId) return null;
    try {
      const client = await waitForApiClient();
      const token = client.accessToken();
      if (!token) return null;
      const resp = await fetch(`${API_BASE}/IsItemMarkedForDeletion/${itemId}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'X-Emby-Token': token,
        },
      });
      if (!resp.ok) {
        warn('Deletion status request failed', resp.status);
        return null;
      }
      return await resp.json();
    } catch (e) {
      error('Failed fetching deletion status', e);
      return null;
    }
  }

  function formatDeletion(deletionStatus) {
    if (!deletionStatus || !deletionStatus.IsMarkedForDeletion) return null;
    return {
      shortText: `in ${deletionStatus.HumanizedTimeUntilDeletion}`,
      tooltipText: `Jellysweep will delete this item in ${deletionStatus.HumanizedTimeUntilDeletion}`,
    };
  }

  function createBadge(textData) {
    const badge = document.createElement('div');
    badge.className = 'jellysweep-deletion-badge mediaInfoItem';
    badge.title = textData.tooltipText;
    badge.style.cssText = ['display:inline-flex', 'align-items:center', 'gap:6px', 'color:#ff5722'].join(';');

    const icon = document.createElement('img');
    icon.src = LOGO_URL;
    icon.alt = 'Jellysweep';
    icon.style.cssText = 'width:16px;height:16px;opacity:.8;flex-shrink:0;';
    icon.onerror = () => {
      icon.remove();
      const fallback = document.createElement('span');
      fallback.textContent = '🗑️';
      fallback.style.fontSize = '12px';
      badge.insertBefore(fallback, badge.firstChild);
    };

    const text = document.createElement('span');
    text.textContent = textData.shortText;
    text.style.cssText = 'font-weight:500;white-space:nowrap;';

    badge.appendChild(icon);
    badge.appendChild(text);
    return badge;
  }

  function clearExistingBadge() {
    const existing = document.querySelector('.jellysweep-deletion-badge');
    if (existing) existing.remove();
  }

  function insertBadge(badge) {
    let target = document.querySelector('.itemMiscInfo-primary') || document.querySelector('.itemMiscInfo-secondary');
    if (target) {
      target.appendChild(badge);
      return;
    }
    const nameContainer = document.querySelector('.nameContainer');
    if (nameContainer && nameContainer.parentNode) {
      nameContainer.parentNode.insertBefore(badge, nameContainer.nextSibling);
      return;
    }
    badge.style.cssText += ';position:fixed;top:20px;right:20px;z-index:10000;';
    document.body.appendChild(badge);
  }

  async function processDetailPage() {
    if (!isDetailPage()) return;
    const itemId = getCurrentItemId();
    if (!itemId) return;
    if (itemId === lastProcessedItemId) return;
    if (isProcessing) return;
    isProcessing = true;
    clearExistingBadge();
    const status = await fetchDeletionStatus(itemId);
    const formatted = formatDeletion(status);
    if (formatted) {
      insertBadge(createBadge(formatted));
      lastProcessedItemId = itemId;
      log('Badge rendered for item', itemId);
    } else {
      lastProcessedItemId = itemId;
      log('Item not marked for deletion', itemId);
    }
    isProcessing = false;
  }

  function setupObservers() {
    if (typeof Events !== 'undefined' && typeof Emby !== 'undefined') {
      Events.on(Emby.Page, 'pageshow', () => setTimeout(processDetailPage, 400));
    }
    window.addEventListener('hashchange', () => setTimeout(processDetailPage, 300));
    let lastHref = location.href;
    new MutationObserver(() => {
      const href = location.href;
      if (href !== lastHref) {
        lastHref = href;
        setTimeout(processDetailPage, 300);
      }
    }).observe(document, { childList: true, subtree: true });
  }

  function init() {
    setupObservers();
    processDetailPage();
    waitForApiClient()
      .then(() => processDetailPage())
      .catch(() => warn('ApiClient unavailable, continuing without token'));
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  window.Jellysweep = {
    refresh: processDetailPage,
    getLastItemId: () => lastProcessedItemId,
    isProcessing: () => isProcessing,
  };
})();

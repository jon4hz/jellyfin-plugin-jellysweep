(function () {
  'use strict';

  // Plugin configuration
  const PLUGIN_NAME = 'Jellysweep';
  const API_BASE = '/Plugins/Jellysweep';
  const LOGO_URL = '/Plugins/Jellysweep/Static/Logo';

  // Cache for API responses to avoid duplicate requests
  const apiCache = new Map();
  const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

  /**
   * Get the current item ID from the page URL
   */
  function getCurrentItemId() {
    const path = window.location.pathname;
    const match = path.match(/\/details\/([a-f0-9]+)/i);
    return match ? match[1] : null;
  }

  /**
   * Make API call to check if item is marked for deletion
   */
  async function checkItemDeletionStatus(itemId) {
    if (!itemId) return null;

    // Check cache first
    const cacheKey = `deletion_status_${itemId}`;
    const cached = apiCache.get(cacheKey);
    if (cached && Date.now() - cached.timestamp < CACHE_DURATION) {
      return cached.data;
    }

    try {
      const response = await fetch(`${API_BASE}/IsItemMarkedForDeletion/${itemId}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'X-Emby-Token': window.ApiClient.accessToken(),
        },
      });

      if (response.ok) {
        const deletionDate = await response.json();

        // Cache the response
        apiCache.set(cacheKey, {
          data: deletionDate,
          timestamp: Date.now(),
        });

        return deletionDate;
      }
    } catch (error) {
      console.error(`${PLUGIN_NAME}: Error checking deletion status for item ${itemId}:`, error);
    }

    return null;
  }

  /**
   * Format the deletion date into human-readable text
   */
  function formatDeletionDate(deletionDateStr) {
    if (!deletionDateStr) return null;

    const deletionDate = new Date(deletionDateStr);
    const now = new Date();
    const diffMs = deletionDate.getTime() - now.getTime();
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      return 'Overdue for deletion';
    } else if (diffDays === 0) {
      return 'Scheduled for deletion today';
    } else if (diffDays === 1) {
      return 'Scheduled for deletion tomorrow';
    } else if (diffDays <= 7) {
      return `Scheduled for deletion in ${diffDays} days`;
    } else if (diffDays <= 30) {
      const weeks = Math.floor(diffDays / 7);
      return `Scheduled for deletion in ${weeks} week${weeks > 1 ? 's' : ''}`;
    } else if (diffDays <= 365) {
      const months = Math.floor(diffDays / 30);
      return `Scheduled for deletion in ${months} month${months > 1 ? 's' : ''}`;
    } else {
      const years = Math.floor(diffDays / 365);
      return `Scheduled for deletion in ${years} year${years > 1 ? 's' : ''}`;
    }
  }

  /**
   * Create the deletion badge element
   */
  function createDeletionBadge(deletionText) {
    const badge = document.createElement('div');
    badge.className = 'jellysweep-deletion-badge';
    badge.style.cssText = `
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background: rgba(255, 87, 34, 0.1);
            border: 1px solid rgba(255, 87, 34, 0.3);
            border-radius: 20px;
            padding: 6px 12px;
            margin: 8px 0;
            font-size: 0.85em;
            color: #ff5722;
            backdrop-filter: blur(10px);
            box-shadow: 0 2px 8px rgba(255, 87, 34, 0.2);
            max-width: fit-content;
        `;

    const icon = document.createElement('img');
    icon.src = LOGO_URL;
    icon.alt = 'Jellysweep';
    icon.style.cssText = `
            width: 16px;
            height: 16px;
            opacity: 0.8;
            flex-shrink: 0;
        `;

    // Handle logo loading errors
    icon.onerror = function () {
      // If logo fails to load, use a simple emoji or text indicator
      this.style.display = 'none';
      const fallbackIcon = document.createElement('span');
      fallbackIcon.textContent = '🗑️';
      fallbackIcon.style.fontSize = '14px';
      badge.insertBefore(fallbackIcon, this);
    };

    const text = document.createElement('span');
    text.textContent = deletionText;
    text.style.cssText = `
            font-weight: 500;
            white-space: nowrap;
        `;

    badge.appendChild(icon);
    badge.appendChild(text);

    return badge;
  }

  /**
   * Add deletion badge to the detail page
   */
  async function addDeletionBadge(deletionDate) {
    // Remove any existing badge
    const existingBadge = document.querySelector('.jellysweep-deletion-badge');
    if (existingBadge) {
      existingBadge.remove();
    }

    const deletionText = formatDeletionDate(deletionDate);
    if (!deletionText) return;

    const badge = createDeletionBadge(deletionText);

    // Find the primary container where we want to add the badge
    const primaryContainer = document.querySelector('.detailPagePrimaryContainer');
    if (primaryContainer) {
      // Look for a good spot to insert the badge - after the title/metadata section
      const titleSection = primaryContainer.querySelector('.detailPageContent, .flex-grow, .itemDetailPage');
      if (titleSection) {
        // Insert after any existing metadata elements
        const metadataSection = titleSection.querySelector('.itemMiscInfo, .overview, .detailPageSecondaryContainer');
        if (metadataSection) {
          metadataSection.parentNode.insertBefore(badge, metadataSection.nextSibling);
        } else {
          titleSection.insertBefore(badge, titleSection.firstChild?.nextSibling || titleSection.firstChild);
        }
      } else {
        // Fallback: add to the beginning of primary container
        primaryContainer.insertBefore(badge, primaryContainer.firstChild);
      }
    }
  }

  /**
   * Check if we're on a detail page and process the item
   */
  async function processDetailPage() {
    // Only run on detail pages
    if (!window.location.pathname.includes('/details/')) {
      return;
    }

    // Wait a bit for the page to load
    await new Promise(resolve => setTimeout(resolve, 500));

    const itemId = getCurrentItemId();
    if (!itemId) return;

    const deletionDate = await checkItemDeletionStatus(itemId);
    if (deletionDate) {
      addDeletionBadge(deletionDate);
    }
  }

  /**
   * Initialize the plugin
   */
  function initialize() {
    // Process the current page
    processDetailPage();

    // Listen for navigation changes
    let lastUrl = window.location.href;
    new MutationObserver(() => {
      const currentUrl = window.location.href;
      if (currentUrl !== lastUrl) {
        lastUrl = currentUrl;
        // Small delay to allow page to load
        setTimeout(processDetailPage, 300);
      }
    }).observe(document, { subtree: true, childList: true });

    // Also listen for popstate events (back/forward navigation)
    window.addEventListener('popstate', () => {
      setTimeout(processDetailPage, 300);
    });

    console.log(`${PLUGIN_NAME}: Client script initialized`);
  }

  // Wait for the page to be ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initialize);
  } else {
    initialize();
  }
})();

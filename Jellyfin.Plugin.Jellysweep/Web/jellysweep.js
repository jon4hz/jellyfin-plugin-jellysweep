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
    const hash = window.location.hash;
    console.log(`${PLUGIN_NAME}: Current path:`, path);
    console.log(`${PLUGIN_NAME}: Current hash:`, hash);

    // Check if we're on a details page using hash-based routing
    if (hash.includes('#/details')) {
      // Extract ID from hash parameters like #/details?id=abc123&serverId=def456
      const urlParams = new URLSearchParams(hash.split('?')[1]);
      const itemId = urlParams.get('id');
      console.log(`${PLUGIN_NAME}: Extracted item ID from hash:`, itemId);
      return itemId;
    }

    // Fallback: try path-based routing
    const match = path.match(/\/details\/([a-f0-9]+)/i);
    const itemId = match ? match[1] : null;
    console.log(`${PLUGIN_NAME}: Extracted item ID from path:`, itemId);
    return itemId;
  }

  /**
   * Wait for ApiClient to be available and have a valid access token (after websocket connection)
   */
  function waitForApiClient(maxWait = 10000, checkInterval = 100) {
    return new Promise((resolve, reject) => {
      const startTime = Date.now();

      function checkApiClient() {
        if (typeof ApiClient !== 'undefined' && ApiClient) {
          // Try to get access token - this will only work after websocket connection is established
          let token = null;

          try {
            token = ApiClient.accessToken();
          } catch (e) {
            console.log(`${PLUGIN_NAME}: ApiClient.accessToken() not ready yet:`, e.message);
          }

          if (token) {
            console.log(`${PLUGIN_NAME}: ApiClient is now available with access token (websocket connected)`);
            resolve({ client: ApiClient, token: token });
            return;
          } else {
            console.log(`${PLUGIN_NAME}: ApiClient available but websocket connection not established yet`);
          }
        }

        if (Date.now() - startTime > maxWait) {
          console.error(`${PLUGIN_NAME}: Timeout waiting for ApiClient with access token`);
          reject(new Error('Timeout waiting for ApiClient with access token'));
          return;
        }

        setTimeout(checkApiClient, checkInterval);
      }

      checkApiClient();
    });
  }

  /**
   * Make API call to check if item is marked for deletion
   */
  async function checkItemDeletionStatus(itemId) {
    if (!itemId) return null;

    console.log(`${PLUGIN_NAME}: Checking deletion status for item:`, itemId);

    // Check cache first
    const cacheKey = `deletion_status_${itemId}`;
    const cached = apiCache.get(cacheKey);
    if (cached && Date.now() - cached.timestamp < CACHE_DURATION) {
      console.log(`${PLUGIN_NAME}: Using cached result for item:`, itemId);
      return cached.data;
    }

    try {
      // Wait for ApiClient to be available with access token
      const { client, token } = await waitForApiClient();

      console.log(`${PLUGIN_NAME}: Making API call with access token (length: ${token?.length || 0})`);

      if (!token) {
        console.error(`${PLUGIN_NAME}: No access token available`);
        return null;
      }

      const response = await fetch(`${API_BASE}/IsItemMarkedForDeletion/${itemId}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'X-Emby-Token': token,
        },
      });

      console.log(`${PLUGIN_NAME}: API response status:`, response.status);

      if (response.ok) {
        const deletionStatus = await response.json();
        console.log(`${PLUGIN_NAME}: Deletion status received:`, deletionStatus);

        // Cache the response
        apiCache.set(cacheKey, {
          data: deletionStatus,
          timestamp: Date.now(),
        });

        return deletionStatus;
      } else {
        console.warn(`${PLUGIN_NAME}: API request failed with status:`, response.status);
      }
    } catch (error) {
      console.error(`${PLUGIN_NAME}: Error checking deletion status for item ${itemId}:`, error);
    }

    return null;
  }

  /**
   * Format the deletion date into human-readable text
   */
  function formatDeletionDate(deletionStatus) {
    if (!deletionStatus || !deletionStatus.IsMarkedForDeletion) return null;

    const shortText = `in ${deletionStatus.HumanizedTimeUntilDeletion}`;
    const tooltipText = `Jellysweep will delete this item in ${deletionStatus.HumanizedTimeUntilDeletion}`;

    return { shortText, tooltipText };
  }

  /**
   * Create the deletion badge element
   */
  function createDeletionBadge(textData) {
    const badge = document.createElement('div');
    badge.className = 'jellysweep-deletion-badge mediaInfoItem';
    badge.title = textData.tooltipText; // Add tooltip
    badge.style.cssText = `
            display: inline-flex;
            align-items: center;
            gap: 6px;
            background: rgba(255, 87, 34, 0.1);
            border: 1px solid rgba(255, 87, 34, 0.3);
            border-radius: 4px;
            padding: 4px 8px;
            font-size: 0.85em;
            color: #ff5722;
            backdrop-filter: blur(10px);
            max-width: fit-content;
            margin-right: 8px;
        `;

    const icon = document.createElement('img');
    icon.src = LOGO_URL;
    icon.alt = 'Jellysweep';
    icon.style.cssText = `
            width: 64px;
            height: 64px;
            opacity: 0.8;
            flex-shrink: 0;
        `;

    // Handle logo loading errors
    icon.onerror = function () {
      // If logo fails to load, use a simple emoji or text indicator
      this.style.display = 'none';
      const fallbackIcon = document.createElement('span');
      fallbackIcon.textContent = '🗑️';
      fallbackIcon.style.fontSize = '12px';
      badge.insertBefore(fallbackIcon, this);
    };

    const text = document.createElement('span');
    text.textContent = textData.shortText;
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
  async function addDeletionBadge(deletionStatus) {
    console.log(`${PLUGIN_NAME}: Adding deletion badge for status:`, deletionStatus);

    // Remove any existing badge
    const existingBadge = document.querySelector('.jellysweep-deletion-badge');
    if (existingBadge) {
      console.log(`${PLUGIN_NAME}: Removing existing badge`);
      existingBadge.remove();
    }

    const textData = formatDeletionDate(deletionStatus);
    if (!textData) {
      console.log(`${PLUGIN_NAME}: No deletion text generated, skipping badge`);
      return;
    }

    console.log(`${PLUGIN_NAME}: Creating badge with text:`, textData.shortText);
    const badge = createDeletionBadge(textData);

    // Target the itemMiscInfo-primary or itemMiscInfo-secondary directly
    let targetContainer = document.querySelector('.itemMiscInfo-primary');

    if (!targetContainer) {
      targetContainer = document.querySelector('.itemMiscInfo-secondary');
    }

    if (targetContainer) {
      // Insert the badge as the last child in the target container
      targetContainer.insertBefore(badge, targetContainer.lastChild.nextSibling);
      console.log(`${PLUGIN_NAME}: Badge inserted into itemMiscInfo container`);
    } else {
      console.warn(`${PLUGIN_NAME}: Could not find itemMiscInfo container, trying fallback locations`);

      // Fallback to other locations
      const nameContainer = document.querySelector('.nameContainer');
      if (nameContainer) {
        const parentContainer = nameContainer.parentNode;
        parentContainer.insertBefore(badge, nameContainer.nextSibling);
        console.log(`${PLUGIN_NAME}: Badge inserted after name container`);
      } else {
        // Final fallback - create floating badge
        badge.style.cssText += `
          position: fixed;
          top: 20px;
          right: 20px;
          z-index: 10000;
        `;
        document.body.appendChild(badge);
        console.log(`${PLUGIN_NAME}: Created floating badge as fallback`);
      }
    }
  }

  /**
   * Wait for the page to be fully loaded and DOM elements to be available
   */
  function waitForPageReady(maxWait = 10000, checkInterval = 200) {
    return new Promise((resolve, reject) => {
      const startTime = Date.now();

      function checkPageReady() {
        // Check if essential Jellyfin elements are present
        const hasBasicStructure =
          document.querySelector('body') &&
          (document.querySelector('.itemDetailPage') ||
            document.querySelector('.detailPageContent') ||
            document.querySelector('.nameContainer'));

        if (hasBasicStructure) {
          console.log(`${PLUGIN_NAME}: Page structure detected, proceeding`);
          resolve();
          return;
        }

        if (Date.now() - startTime > maxWait) {
          console.warn(`${PLUGIN_NAME}: Timeout waiting for page to be ready, proceeding anyway`);
          resolve(); // Don't reject, just proceed
          return;
        }
        setTimeout(checkPageReady, checkInterval);
      }

      checkPageReady();
    });
  }

  // Jellysweep Plugin Main Object
  const JellysweepPlugin = {
    /**
     * Check if we're on a detail page and process the item
     */
    processDetailPage: async function () {
      const path = window.location.pathname;
      const hash = window.location.hash;
      console.log(`${PLUGIN_NAME}: Processing detail page, current path:`, path, 'hash:', hash);

      // Check for hash-based routing (modern Jellyfin) or path-based routing
      const isDetailPage = hash.includes('#/details') || path.includes('/details');

      if (!isDetailPage) {
        console.log(`${PLUGIN_NAME}: Not on a detail page, skipping`);
        return;
      }

      // Wait for the page to be fully ready
      console.log(`${PLUGIN_NAME}: Waiting for page to be ready...`);
      await waitForPageReady();

      // Additional wait to ensure all elements are rendered
      await new Promise(resolve => setTimeout(resolve, 1000));

      const itemId = getCurrentItemId();
      if (!itemId) {
        console.log(`${PLUGIN_NAME}: No item ID found, skipping`);
        return;
      }

      console.log(`${PLUGIN_NAME}: Processing item:`, itemId);
      const deletionStatus = await checkItemDeletionStatus(itemId);
      if (deletionStatus && deletionStatus.IsMarkedForDeletion) {
        console.log(`${PLUGIN_NAME}: Item marked for deletion, adding badge`);
        addDeletionBadge(deletionStatus);
      } else {
        console.log(`${PLUGIN_NAME}: Item not marked for deletion`);
      }
    },

    /**
     * Initialize the plugin with proper timing
     */
    initialize: async function () {
      console.log(`${PLUGIN_NAME}: Initializing plugin`);

      // Wait a moment for any ongoing navigation to complete
      await new Promise(resolve => setTimeout(resolve, 100));

      this.processDetailPage();
    },
  };

  // Enhanced initialization logic
  function initializePlugin() {
    console.log(`${PLUGIN_NAME}: Starting initialization process`);
    JellysweepPlugin.initialize();
  }

  // Wait for document to be ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializePlugin);
  } else {
    // Document already loaded, but wait a bit for Jellyfin to finish rendering
    setTimeout(initializePlugin, 500);
  }

  // Handle single page app navigation for modern Jellyfin
  if (typeof Events !== 'undefined' && typeof Emby !== 'undefined') {
    Events.on(Emby.Page, 'pageshow', function () {
      console.log(`${PLUGIN_NAME}: Page show event triggered`);
      // Add delay to ensure page is fully rendered
      setTimeout(() => {
        JellysweepPlugin.processDetailPage();
      }, 800);
    });
  }

  // Additional event listeners for navigation changes
  window.addEventListener('hashchange', function () {
    console.log(`${PLUGIN_NAME}: Hash change detected`);
    setTimeout(() => {
      JellysweepPlugin.processDetailPage();
    }, 1000);
  });

  // Monitor for navigation in single page apps
  let lastUrl = location.href;
  new MutationObserver(() => {
    const url = location.href;
    if (url !== lastUrl) {
      lastUrl = url;
      console.log(`${PLUGIN_NAME}: URL change detected`);
      setTimeout(() => {
        JellysweepPlugin.processDetailPage();
      }, 1200);
    }
  }).observe(document, { subtree: true, childList: true });
})();

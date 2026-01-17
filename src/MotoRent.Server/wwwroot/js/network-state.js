/**
 * MotoRent Network State Service
 * Provides network connectivity detection and monitoring.
 */

const MotoRentNetwork = (function () {
    let dotNetCallback = null;

    /**
     * Check if the browser reports being online
     */
    function isOnline() {
        return navigator.onLine;
    }

    /**
     * Check actual connectivity by making a network request
     * More reliable than navigator.onLine
     */
    async function checkConnectivity() {
        if (!navigator.onLine) {
            return false;
        }

        try {
            // Try to fetch a small resource to verify actual connectivity
            // Using a HEAD request to minimize data transfer
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 5000);

            const response = await fetch('/favicon.png', {
                method: 'HEAD',
                cache: 'no-store',
                signal: controller.signal
            });

            clearTimeout(timeoutId);
            return response.ok;
        } catch (error) {
            console.log('[MotoRentNetwork] Connectivity check failed:', error.message);
            return false;
        }
    }

    /**
     * Get connection type from Network Information API
     * Returns: wifi, cellular, ethernet, none, unknown
     */
    function getConnectionType() {
        const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        if (connection) {
            return connection.type || 'unknown';
        }
        return null;
    }

    /**
     * Get effective connection type from Network Information API
     * Returns: 4g, 3g, 2g, slow-2g
     */
    function getEffectiveType() {
        const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        if (connection) {
            return connection.effectiveType || 'unknown';
        }
        return null;
    }

    /**
     * Register .NET callback for state changes
     */
    function registerCallback(dotNetRef) {
        dotNetCallback = dotNetRef;

        // Listen for online/offline events
        window.addEventListener('online', handleOnline);
        window.addEventListener('offline', handleOffline);

        // Also listen for connection changes if available
        const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        if (connection) {
            connection.addEventListener('change', handleConnectionChange);
        }

        console.log('[MotoRentNetwork] Callback registered');
    }

    /**
     * Unregister .NET callback
     */
    function unregisterCallback() {
        window.removeEventListener('online', handleOnline);
        window.removeEventListener('offline', handleOffline);

        const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        if (connection) {
            connection.removeEventListener('change', handleConnectionChange);
        }

        dotNetCallback = null;
        console.log('[MotoRentNetwork] Callback unregistered');
    }

    /**
     * Handle online event
     */
    function handleOnline() {
        console.log('[MotoRentNetwork] Online event fired');
        notifyStateChange(true);
    }

    /**
     * Handle offline event
     */
    function handleOffline() {
        console.log('[MotoRentNetwork] Offline event fired');
        notifyStateChange(false);
    }

    /**
     * Handle connection change (Network Information API)
     */
    async function handleConnectionChange() {
        console.log('[MotoRentNetwork] Connection change detected');
        const isConnected = await checkConnectivity();
        notifyStateChange(isConnected);
    }

    /**
     * Notify .NET of state change
     */
    function notifyStateChange(isOnline) {
        if (dotNetCallback) {
            dotNetCallback.invokeMethodAsync('OnStateChanged', isOnline)
                .catch(err => console.error('[MotoRentNetwork] Callback error:', err));
        }
    }

    /**
     * Get connection info summary
     */
    function getConnectionInfo() {
        const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        if (connection) {
            return {
                type: connection.type,
                effectiveType: connection.effectiveType,
                downlink: connection.downlink,
                rtt: connection.rtt,
                saveData: connection.saveData
            };
        }
        return {
            type: 'unknown',
            effectiveType: 'unknown',
            online: navigator.onLine
        };
    }

    // Public API
    return {
        isOnline: isOnline,
        checkConnectivity: checkConnectivity,
        getConnectionType: getConnectionType,
        getEffectiveType: getEffectiveType,
        getConnectionInfo: getConnectionInfo,
        registerCallback: registerCallback,
        unregisterCallback: unregisterCallback
    };
})();

// Explicitly attach to window for Blazor JS interop
// This ensures the API is available immediately when Blazor starts
window.MotoRentNetwork = MotoRentNetwork;

// Export for module systems if available
if (typeof module !== 'undefined' && module.exports) {
    module.exports = MotoRentNetwork;
}

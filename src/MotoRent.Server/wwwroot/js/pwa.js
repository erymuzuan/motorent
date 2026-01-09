// MotoRent PWA Utilities

// Install prompt handling
let deferredPrompt = null;
let installPromptShown = false;

window.addEventListener('beforeinstallprompt', (e) => {
    console.log('[PWA] beforeinstallprompt event fired');
    e.preventDefault();
    deferredPrompt = e;

    // Notify Blazor that install is available
    if (window.DotNet) {
        window.DotNet.invokeMethodAsync('MotoRent.Client', 'OnInstallAvailable');
    }
});

window.addEventListener('appinstalled', () => {
    console.log('[PWA] App was installed');
    deferredPrompt = null;
    installPromptShown = true;

    if (window.DotNet) {
        window.DotNet.invokeMethodAsync('MotoRent.Client', 'OnAppInstalled');
    }
});

// PWA install functions callable from Blazor
window.MotoRentPwa = {
    // Check if PWA is installable
    isInstallable: function () {
        return deferredPrompt !== null;
    },

    // Check if running as standalone PWA
    isStandalone: function () {
        return window.matchMedia('(display-mode: standalone)').matches ||
            window.navigator.standalone === true ||
            document.referrer.includes('android-app://');
    },

    // Show install prompt
    showInstallPrompt: async function () {
        if (!deferredPrompt) {
            console.log('[PWA] No install prompt available');
            return { success: false, outcome: 'no-prompt' };
        }

        deferredPrompt.prompt();
        const result = await deferredPrompt.userChoice;
        console.log('[PWA] User choice:', result.outcome);

        deferredPrompt = null;
        return { success: result.outcome === 'accepted', outcome: result.outcome };
    },

    // Request notification permission
    requestNotificationPermission: async function () {
        if (!('Notification' in window)) {
            return { granted: false, reason: 'not-supported' };
        }

        if (Notification.permission === 'granted') {
            return { granted: true, reason: 'already-granted' };
        }

        if (Notification.permission === 'denied') {
            return { granted: false, reason: 'denied' };
        }

        const permission = await Notification.requestPermission();
        return { granted: permission === 'granted', reason: permission };
    },

    // Get notification permission status
    getNotificationPermission: function () {
        if (!('Notification' in window)) {
            return 'not-supported';
        }
        return Notification.permission;
    },

    // Subscribe to push notifications
    subscribeToPush: async function (vapidPublicKey) {
        try {
            const registration = await navigator.serviceWorker.ready;

            const subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: this.urlBase64ToUint8Array(vapidPublicKey)
            });

            console.log('[PWA] Push subscription:', subscription);
            return { success: true, subscription: JSON.stringify(subscription) };
        } catch (error) {
            console.error('[PWA] Push subscription failed:', error);
            return { success: false, error: error.message };
        }
    },

    // Show local notification
    showNotification: async function (title, options) {
        if (Notification.permission !== 'granted') {
            return false;
        }

        const registration = await navigator.serviceWorker.ready;
        await registration.showNotification(title, {
            body: options.body || '',
            icon: options.icon || '/icons/icon-192x192.png',
            badge: options.badge || '/icons/icon-72x72.png',
            vibrate: options.vibrate || [100, 50, 100],
            data: options.data || {},
            actions: options.actions || []
        });

        return true;
    },

    // Helper to convert VAPID key
    urlBase64ToUint8Array: function (base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    },

    // Check for service worker update
    checkForUpdate: async function () {
        const registration = await navigator.serviceWorker.ready;
        await registration.update();
        return true;
    },

    // Get app display mode
    getDisplayMode: function () {
        if (window.matchMedia('(display-mode: standalone)').matches) {
            return 'standalone';
        }
        if (window.matchMedia('(display-mode: fullscreen)').matches) {
            return 'fullscreen';
        }
        if (window.matchMedia('(display-mode: minimal-ui)').matches) {
            return 'minimal-ui';
        }
        return 'browser';
    }
};

// Camera access for document capture
window.MotoRentCamera = {
    // Check if camera is available
    isAvailable: function () {
        return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
    },

    // Get camera stream
    getStream: async function (facingMode = 'environment') {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: facingMode,
                    width: { ideal: 1920 },
                    height: { ideal: 1080 }
                }
            });
            return { success: true, stream: stream };
        } catch (error) {
            console.error('[Camera] Error getting stream:', error);
            return { success: false, error: error.message };
        }
    },

    // Capture photo from video element
    capturePhoto: function (videoElementId, quality = 0.92) {
        const video = document.getElementById(videoElementId);
        if (!video) {
            return { success: false, error: 'Video element not found' };
        }

        const canvas = document.createElement('canvas');
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        const ctx = canvas.getContext('2d');
        ctx.drawImage(video, 0, 0);

        const dataUrl = canvas.toDataURL('image/jpeg', quality);
        return { success: true, dataUrl: dataUrl };
    },

    // Stop camera stream
    stopStream: function (stream) {
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
        }
    }
};

console.log('[PWA] MotoRent PWA utilities loaded');

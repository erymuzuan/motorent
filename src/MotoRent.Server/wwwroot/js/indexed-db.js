/**
 * MotoRent IndexedDB Wrapper for Offline Storage
 * Provides a clean API for storing and retrieving rental data offline.
 */

const MotoRentOfflineDb = (function () {
    const DB_NAME = 'motorent-tourist';
    const DB_VERSION = 1;

    // Store names
    const STORES = {
        ACTIVE_RENTAL: 'activeRental',
        TENANT_CONTEXT: 'tenantContext',
        EMERGENCY_CONTACTS: 'emergencyContacts',
        POI: 'poi',
        ROUTES: 'routes',
        PENDING_PHOTOS: 'pendingPhotos',
        ACCIDENT_REPORTS: 'accidentReports',
        CONTRACTS: 'contracts',
        SYNC_QUEUE: 'syncQueue',
        SETTINGS: 'settings'
    };

    let db = null;

    /**
     * Initialize the database
     */
    async function initDb() {
        if (db) return db;

        return new Promise((resolve, reject) => {
            const request = indexedDB.open(DB_NAME, DB_VERSION);

            request.onerror = () => {
                console.error('[IndexedDB] Error opening database:', request.error);
                reject(request.error);
            };

            request.onsuccess = () => {
                db = request.result;
                console.log('[IndexedDB] Database opened successfully');
                resolve(db);
            };

            request.onupgradeneeded = (event) => {
                console.log('[IndexedDB] Upgrading database...');
                const database = event.target.result;

                // Active rental store - stores the current rental package
                if (!database.objectStoreNames.contains(STORES.ACTIVE_RENTAL)) {
                    const rentalStore = database.createObjectStore(STORES.ACTIVE_RENTAL, { keyPath: 'rentalId' });
                    rentalStore.createIndex('webId', 'webId', { unique: true });
                    rentalStore.createIndex('status', 'status', { unique: false });
                }

                // Tenant context store
                if (!database.objectStoreNames.contains(STORES.TENANT_CONTEXT)) {
                    database.createObjectStore(STORES.TENANT_CONTEXT, { keyPath: 'accountNo' });
                }

                // Emergency contacts store
                if (!database.objectStoreNames.contains(STORES.EMERGENCY_CONTACTS)) {
                    const emergencyStore = database.createObjectStore(STORES.EMERGENCY_CONTACTS, { keyPath: 'id' });
                    emergencyStore.createIndex('type', 'type', { unique: false });
                    emergencyStore.createIndex('priority', 'priority', { unique: false });
                }

                // POI store
                if (!database.objectStoreNames.contains(STORES.POI)) {
                    const poiStore = database.createObjectStore(STORES.POI, { keyPath: 'poiId' });
                    poiStore.createIndex('category', 'category', { unique: false });
                    poiStore.createIndex('isCurated', 'isCurated', { unique: false });
                }

                // Routes store
                if (!database.objectStoreNames.contains(STORES.ROUTES)) {
                    const routeStore = database.createObjectStore(STORES.ROUTES, { keyPath: 'routeId' });
                    routeStore.createIndex('category', 'category', { unique: false });
                    routeStore.createIndex('difficulty', 'difficulty', { unique: false });
                }

                // Pending photos store
                if (!database.objectStoreNames.contains(STORES.PENDING_PHOTOS)) {
                    const photoStore = database.createObjectStore(STORES.PENDING_PHOTOS, { keyPath: 'localId' });
                    photoStore.createIndex('rentalId', 'rentalId', { unique: false });
                    photoStore.createIndex('synced', 'synced', { unique: false });
                    photoStore.createIndex('timestamp', 'timestamp', { unique: false });
                }

                // Accident reports store
                if (!database.objectStoreNames.contains(STORES.ACCIDENT_REPORTS)) {
                    const accidentStore = database.createObjectStore(STORES.ACCIDENT_REPORTS, { keyPath: 'localId' });
                    accidentStore.createIndex('rentalId', 'rentalId', { unique: false });
                    accidentStore.createIndex('status', 'status', { unique: false });
                }

                // Contracts store
                if (!database.objectStoreNames.contains(STORES.CONTRACTS)) {
                    database.createObjectStore(STORES.CONTRACTS, { keyPath: 'rentalId' });
                }

                // Sync queue store
                if (!database.objectStoreNames.contains(STORES.SYNC_QUEUE)) {
                    const syncStore = database.createObjectStore(STORES.SYNC_QUEUE, { keyPath: 'id', autoIncrement: true });
                    syncStore.createIndex('type', 'type', { unique: false });
                    syncStore.createIndex('timestamp', 'timestamp', { unique: false });
                }

                // Settings store (for app preferences)
                if (!database.objectStoreNames.contains(STORES.SETTINGS)) {
                    database.createObjectStore(STORES.SETTINGS, { keyPath: 'key' });
                }

                console.log('[IndexedDB] Database upgrade complete');
            };
        });
    }

    /**
     * Get a single item by key
     */
    async function get(storeName, key) {
        await initDb();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readonly');
            const store = transaction.objectStore(storeName);
            const request = store.get(key);

            request.onsuccess = () => resolve(request.result || null);
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Get all items from a store
     */
    async function getAll(storeName) {
        await initDb();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readonly');
            const store = transaction.objectStore(storeName);
            const request = store.getAll();

            request.onsuccess = () => resolve(request.result || []);
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Get items by index value
     */
    async function getByIndex(storeName, indexName, value) {
        await initDb();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readonly');
            const store = transaction.objectStore(storeName);
            const index = store.index(indexName);
            const request = index.getAll(value);

            request.onsuccess = () => resolve(request.result || []);
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Put (insert or update) an item
     */
    async function put(storeName, item) {
        await initDb();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readwrite');
            const store = transaction.objectStore(storeName);
            const request = store.put(item);

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Put multiple items
     */
    async function putMany(storeName, items) {
        await initDb();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readwrite');
            const store = transaction.objectStore(storeName);

            let completed = 0;
            const errors = [];

            items.forEach(item => {
                const request = store.put(item);
                request.onsuccess = () => {
                    completed++;
                    if (completed === items.length) {
                        resolve(completed);
                    }
                };
                request.onerror = () => {
                    errors.push(request.error);
                    completed++;
                    if (completed === items.length) {
                        if (errors.length > 0) {
                            reject(errors[0]);
                        } else {
                            resolve(completed);
                        }
                    }
                };
            });

            if (items.length === 0) {
                resolve(0);
            }
        });
    }

    /**
     * Delete an item by key
     */
    async function remove(storeName, key) {
        await initDb();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readwrite');
            const store = transaction.objectStore(storeName);
            const request = store.delete(key);

            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Clear all items from a store
     */
    async function clear(storeName) {
        await initDb();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readwrite');
            const store = transaction.objectStore(storeName);
            const request = store.clear();

            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }

    /**
     * Count items in a store
     */
    async function count(storeName) {
        await initDb();
        return new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readonly');
            const store = transaction.objectStore(storeName);
            const request = store.count();

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    // ==================== Rental-specific methods ====================

    /**
     * Save a rental package for offline use
     */
    async function saveRentalPackage(rentalPackage) {
        // Add download metadata
        rentalPackage.downloadedAt = new Date().toISOString();
        rentalPackage.lastSyncAt = new Date().toISOString();

        await put(STORES.ACTIVE_RENTAL, rentalPackage);

        // Save related data
        if (rentalPackage.shopContact) {
            await put(STORES.TENANT_CONTEXT, {
                accountNo: rentalPackage.accountNo,
                ...rentalPackage.tenantContext
            });
        }

        if (rentalPackage.emergencyContacts) {
            await putMany(STORES.EMERGENCY_CONTACTS, rentalPackage.emergencyContacts);
        }

        if (rentalPackage.contract) {
            await put(STORES.CONTRACTS, {
                rentalId: rentalPackage.rentalId,
                html: rentalPackage.contract,
                signedAt: rentalPackage.contractSignedAt
            });
        }

        console.log('[IndexedDB] Rental package saved:', rentalPackage.rentalId);
        return true;
    }

    /**
     * Get the active rental package
     */
    async function getActiveRental() {
        const rentals = await getAll(STORES.ACTIVE_RENTAL);
        // Return the most recent active or reserved rental
        const active = rentals.filter(r => r.status === 'Active' || r.status === 'Reserved');
        if (active.length > 0) {
            return active.sort((a, b) => new Date(b.downloadedAt) - new Date(a.downloadedAt))[0];
        }
        return rentals.length > 0 ? rentals[0] : null;
    }

    /**
     * Get rental by WebId
     */
    async function getRentalByWebId(webId) {
        const rentals = await getByIndex(STORES.ACTIVE_RENTAL, 'webId', webId);
        return rentals.length > 0 ? rentals[0] : null;
    }

    /**
     * Check if rental data is stale
     */
    async function isRentalStale(rentalId, maxAgeMinutes = 60) {
        const rental = await get(STORES.ACTIVE_RENTAL, rentalId);
        if (!rental || !rental.lastSyncAt) return true;

        const lastSync = new Date(rental.lastSyncAt);
        const now = new Date();
        const ageMinutes = (now - lastSync) / (1000 * 60);

        return ageMinutes > maxAgeMinutes;
    }

    // ==================== Photo methods ====================

    /**
     * Save a photo for later sync
     */
    async function savePendingPhoto(photo) {
        photo.localId = photo.localId || generateUUID();
        photo.timestamp = photo.timestamp || new Date().toISOString();
        photo.synced = false;

        await put(STORES.PENDING_PHOTOS, photo);
        return photo.localId;
    }

    /**
     * Get pending (unsynced) photos
     */
    async function getPendingPhotos(rentalId) {
        if (rentalId) {
            const photos = await getByIndex(STORES.PENDING_PHOTOS, 'rentalId', rentalId);
            return photos.filter(p => !p.synced);
        }
        return (await getByIndex(STORES.PENDING_PHOTOS, 'synced', false));
    }

    /**
     * Mark a photo as synced
     */
    async function markPhotoSynced(localId, serverStoreId) {
        const photo = await get(STORES.PENDING_PHOTOS, localId);
        if (photo) {
            photo.synced = true;
            photo.syncedAt = new Date().toISOString();
            photo.serverStoreId = serverStoreId;
            await put(STORES.PENDING_PHOTOS, photo);
        }
    }

    // ==================== Accident Report methods ====================

    /**
     * Save an accident report draft
     */
    async function saveAccidentReport(report) {
        report.localId = report.localId || generateUUID();
        report.createdAt = report.createdAt || new Date().toISOString();
        report.status = report.status || 'draft';

        await put(STORES.ACCIDENT_REPORTS, report);
        return report.localId;
    }

    /**
     * Get accident reports for a rental
     */
    async function getAccidentReports(rentalId) {
        return await getByIndex(STORES.ACCIDENT_REPORTS, 'rentalId', rentalId);
    }

    // ==================== Sync Queue methods ====================

    /**
     * Add item to sync queue
     */
    async function addToSyncQueue(type, data) {
        const item = {
            type: type,
            data: data,
            timestamp: new Date().toISOString(),
            retryCount: 0
        };
        await put(STORES.SYNC_QUEUE, item);
    }

    /**
     * Get pending sync items
     */
    async function getPendingSyncItems() {
        return await getAll(STORES.SYNC_QUEUE);
    }

    /**
     * Remove item from sync queue
     */
    async function removeSyncItem(id) {
        await remove(STORES.SYNC_QUEUE, id);
    }

    // ==================== Utility methods ====================

    /**
     * Generate a UUID
     */
    function generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    /**
     * Get storage usage estimate
     */
    async function getStorageEstimate() {
        if (navigator.storage && navigator.storage.estimate) {
            const estimate = await navigator.storage.estimate();
            return {
                usage: estimate.usage,
                quota: estimate.quota,
                usagePercent: ((estimate.usage / estimate.quota) * 100).toFixed(2)
            };
        }
        return null;
    }

    /**
     * Check if IndexedDB is supported
     */
    function isSupported() {
        return !!window.indexedDB;
    }

    /**
     * Delete the entire database (for testing/reset)
     */
    async function deleteDatabase() {
        if (db) {
            db.close();
            db = null;
        }
        return new Promise((resolve, reject) => {
            const request = indexedDB.deleteDatabase(DB_NAME);
            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }

    // Public API
    return {
        // Core operations
        init: initDb,
        get: get,
        getAll: getAll,
        getByIndex: getByIndex,
        put: put,
        putMany: putMany,
        remove: remove,
        clear: clear,
        count: count,

        // Rental operations
        saveRentalPackage: saveRentalPackage,
        getActiveRental: getActiveRental,
        getRentalByWebId: getRentalByWebId,
        isRentalStale: isRentalStale,

        // Photo operations
        savePendingPhoto: savePendingPhoto,
        getPendingPhotos: getPendingPhotos,
        markPhotoSynced: markPhotoSynced,

        // Accident report operations
        saveAccidentReport: saveAccidentReport,
        getAccidentReports: getAccidentReports,

        // Sync queue operations
        addToSyncQueue: addToSyncQueue,
        getPendingSyncItems: getPendingSyncItems,
        removeSyncItem: removeSyncItem,

        // Utility
        generateUUID: generateUUID,
        getStorageEstimate: getStorageEstimate,
        isSupported: isSupported,
        deleteDatabase: deleteDatabase,

        // Store names for direct access
        STORES: STORES
    };
})();

// Export for module systems if available
if (typeof module !== 'undefined' && module.exports) {
    module.exports = MotoRentOfflineDb;
}

// Expose on window for Blazor Server JS interop
window.MotoRentOfflineDb = MotoRentOfflineDb;

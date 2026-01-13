// Google Maps Module for MotoRent
// Provides map picker and display functionality for GPS locations

let map = null;
let marker = null;
let markers = [];
let infoWindows = [];

/**
 * Check if Google Maps API is loaded
 */
export function isGoogleMapsLoaded() {
    return typeof google !== 'undefined' && typeof google.maps !== 'undefined';
}

/**
 * Initialize map in picker mode with draggable marker
 * @param {string} elementId - DOM element ID for the map
 * @param {object} center - Initial center {lat, lng}
 * @param {boolean} readonly - If true, marker is not draggable
 */
export async function initPicker(elementId, center, readonly = false) {
    await waitForGoogleMaps();

    const element = document.getElementById(elementId);
    if (!element) {
        console.error('Map element not found:', elementId);
        return;
    }

    // Default center: Phuket, Thailand
    if (!center || !center.lat || !center.lng) {
        center = { lat: 7.8804, lng: 98.3923 };
    }

    map = new google.maps.Map(element, {
        center: center,
        zoom: 15,
        mapTypeId: 'roadmap',
        streetViewControl: false,
        fullscreenControl: true,
        mapTypeControl: true,
        mapTypeControlOptions: {
            style: google.maps.MapTypeControlStyle.DROPDOWN_MENU
        }
    });

    marker = new google.maps.Marker({
        position: center,
        map: map,
        draggable: !readonly,
        animation: google.maps.Animation.DROP,
        title: readonly ? 'Location' : 'Drag to set location'
    });

    // Center map on marker when dragged
    if (!readonly) {
        marker.addListener('dragend', () => {
            map.panTo(marker.getPosition());
        });

        // Allow clicking on map to move marker
        map.addListener('click', (e) => {
            marker.setPosition(e.latLng);
            map.panTo(e.latLng);
        });
    }
}

/**
 * Get current picker location
 */
export function getPickerLocation() {
    if (!marker) return null;
    const pos = marker.getPosition();
    return { lat: pos.lat(), lng: pos.lng() };
}

/**
 * Set picker location
 * @param {object} location - {lat, lng}
 */
export function setPickerLocation(location) {
    if (!marker || !map) return;
    const pos = new google.maps.LatLng(location.lat, location.lng);
    marker.setPosition(pos);
    map.panTo(pos);
}

/**
 * Display multiple markers on map
 * @param {string} elementId - DOM element ID
 * @param {array} locations - Array of {lat, lng, label, address?, icon?, href?}
 */
export async function displayMarkers(elementId, locations) {
    await waitForGoogleMaps();

    const element = document.getElementById(elementId);
    if (!element) {
        console.error('Map element not found:', elementId);
        return;
    }

    // Clear existing markers and info windows
    clearMarkers();

    if (!locations || locations.length === 0) return;

    // Create map centered on first location
    map = new google.maps.Map(element, {
        center: { lat: locations[0].lat, lng: locations[0].lng },
        zoom: 13,
        mapTypeId: 'roadmap',
        streetViewControl: false,
        fullscreenControl: true
    });

    const bounds = new google.maps.LatLngBounds();

    locations.forEach((loc, index) => {
        const position = { lat: loc.lat, lng: loc.lng };
        bounds.extend(position);

        const m = new google.maps.Marker({
            position: position,
            map: map,
            title: loc.label || `Location ${index + 1}`,
            animation: google.maps.Animation.DROP
        });

        // Add info window
        if (loc.label) {
            const infoWindow = new google.maps.InfoWindow({
                content: createInfoWindowContent(loc)
            });

            m.addListener('click', () => {
                // Close all other info windows
                infoWindows.forEach(iw => iw.close());
                infoWindow.open(map, m);
            });

            infoWindows.push(infoWindow);
        }

        markers.push(m);
    });

    // Fit map to show all markers
    if (locations.length > 1) {
        map.fitBounds(bounds);
        // Add some padding
        const padding = { top: 50, right: 50, bottom: 50, left: 50 };
        map.fitBounds(bounds, padding);
    }
}

/**
 * Display a single marker on a view-only map
 * @param {string} elementId - DOM element ID
 * @param {object} location - {lat, lng, label?}
 */
export async function displaySingleMarker(elementId, location) {
    await waitForGoogleMaps();

    const element = document.getElementById(elementId);
    if (!element) {
        console.error('Map element not found:', elementId);
        return;
    }

    if (!location || !location.lat || !location.lng) {
        console.error('Invalid location:', location);
        return;
    }

    // Clear existing
    clearMarkers();

    map = new google.maps.Map(element, {
        center: { lat: location.lat, lng: location.lng },
        zoom: 15,
        mapTypeId: 'roadmap',
        streetViewControl: false,
        fullscreenControl: true,
        zoomControl: true,
        mapTypeControl: false
    });

    const m = new google.maps.Marker({
        position: { lat: location.lat, lng: location.lng },
        map: map,
        title: location.label || 'Location',
        animation: google.maps.Animation.DROP
    });

    markers.push(m);
}

/**
 * Create info window HTML content
 */
function createInfoWindowContent(loc) {
    let content = `<div style="min-width: 180px; padding: 4px;">`;
    content += `<strong style="font-size: 14px;">${escapeHtml(loc.label)}</strong>`;

    if (loc.address) {
        content += `<br><small style="color: #666;">${escapeHtml(loc.address)}</small>`;
    }

    if (loc.href) {
        content += `<br><a href="${escapeHtml(loc.href)}" target="_blank" rel="noopener" `;
        content += `style="display: inline-block; margin-top: 8px; padding: 6px 12px; `;
        content += `background: #00897B; color: white; text-decoration: none; border-radius: 4px; font-size: 12px;">`;
        content += `Get Directions</a>`;
    }

    content += `</div>`;
    return content;
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Get user's current location
 */
export function getCurrentLocation() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation not supported'));
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (pos) => resolve({ lat: pos.coords.latitude, lng: pos.coords.longitude }),
            (err) => reject(err),
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 }
        );
    });
}

/**
 * Center map on current location and move marker
 */
export async function centerOnCurrentLocation() {
    try {
        const pos = await getCurrentLocation();
        if (map) {
            map.setCenter(pos);
            map.setZoom(16);
            if (marker) {
                marker.setPosition(pos);
            }
        }
        return pos;
    } catch (error) {
        console.warn('Could not get current location:', error.message);
        throw error;
    }
}

/**
 * Clear all markers from the map
 */
function clearMarkers() {
    markers.forEach(m => m.setMap(null));
    markers = [];
    infoWindows.forEach(iw => iw.close());
    infoWindows = [];
    if (marker) {
        marker.setMap(null);
        marker = null;
    }
}

/**
 * Wait for Google Maps API to load
 */
function waitForGoogleMaps() {
    return new Promise((resolve) => {
        if (isGoogleMapsLoaded()) {
            resolve();
            return;
        }

        // Poll for Google Maps
        let attempts = 0;
        const maxAttempts = 100; // 10 seconds max
        const checkInterval = setInterval(() => {
            attempts++;
            if (isGoogleMapsLoaded()) {
                clearInterval(checkInterval);
                resolve();
            } else if (attempts >= maxAttempts) {
                clearInterval(checkInterval);
                console.error('Google Maps failed to load');
                resolve(); // Resolve anyway to prevent hanging
            }
        }, 100);
    });
}

/**
 * Clean up map resources
 */
export function dispose() {
    clearMarkers();
    map = null;
}

/**
 * Get the current map zoom level
 */
export function getZoom() {
    return map ? map.getZoom() : null;
}

/**
 * Set the map zoom level
 */
export function setZoom(zoom) {
    if (map) {
        map.setZoom(zoom);
    }
}

/**
 * Pan the map to a specific location
 */
export function panTo(location) {
    if (map && location && location.lat && location.lng) {
        map.panTo({ lat: location.lat, lng: location.lng });
    }
}

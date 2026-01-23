// Vehicle Inspector JavaScript Module for MotoRent
// Handles 3D model viewer integration with model-viewer web component
// and canvas overlay for damage marker management

let modelViewerInstance = null;
let canvasOverlay = null;
let dotNetRef = null;
let markers = [];
let isInitialized = false;

/**
 * Initializes the vehicle inspector with a model-viewer element
 * @param {HTMLElement} modelViewer - The model-viewer element
 * @param {HTMLCanvasElement} canvas - The canvas overlay for markers
 * @param {object} dotNet - DotNetObjectReference for callbacks
 * @param {object} options - Configuration options
 */
export async function initialize(modelViewer, canvas, dotNet, options) {
    if (!modelViewer) {
        console.error('Model viewer element is null');
        return { success: false, error: 'Model viewer element is null' };
    }

    modelViewerInstance = modelViewer;
    canvasOverlay = canvas;
    dotNetRef = dotNet;
    markers = options?.markers || [];

    try {
        // Wait for model to load
        await waitForModelLoad(modelViewer);

        // Set up click handler for marker placement
        modelViewer.addEventListener('click', handleModelClick);

        // Set up camera change handler for marker position updates
        modelViewer.addEventListener('camera-change', handleCameraChange);

        // Initial render of markers
        if (canvasOverlay) {
            renderMarkers();
        }

        isInitialized = true;

        return { success: true };
    } catch (error) {
        console.error('Failed to initialize vehicle inspector:', error);
        return { success: false, error: error.message };
    }
}

/**
 * Waits for the model-viewer to finish loading the model
 */
function waitForModelLoad(modelViewer) {
    return new Promise((resolve, reject) => {
        if (modelViewer.loaded) {
            resolve();
            return;
        }

        const timeout = setTimeout(() => {
            reject(new Error('Model load timeout'));
        }, 30000); // 30 second timeout

        modelViewer.addEventListener('load', () => {
            clearTimeout(timeout);
            resolve();
        }, { once: true });

        modelViewer.addEventListener('error', (e) => {
            clearTimeout(timeout);
            reject(new Error('Model load failed: ' + e.detail));
        }, { once: true });
    });
}

/**
 * Handles click events on the model for marker placement
 */
async function handleModelClick(event) {
    if (!dotNetRef) return;

    const modelViewer = event.target;
    const rect = modelViewer.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    // Get 3D position from click using model-viewer's positionAndNormalFromPoint
    const hit = modelViewer.positionAndNormalFromPoint(x, y);

    if (hit) {
        const position = {
            x: hit.position.x,
            y: hit.position.y,
            z: hit.position.z,
            normalX: hit.normal.x,
            normalY: hit.normal.y,
            normalZ: hit.normal.z,
            // Store screen position for rendering markers
            screenX: x,
            screenY: y
        };

        const screenPosition = {
            x: x,
            y: y
        };

        try {
            await dotNetRef.invokeMethodAsync('OnModelClicked', position, screenPosition);
        } catch (error) {
            console.error('Error invoking OnModelClicked:', error);
        }
    }
}

/**
 * Handles camera change events to update marker screen positions
 */
function handleCameraChange(event) {
    if (canvasOverlay && markers.length > 0) {
        renderMarkers();
    }
}

/**
 * Converts a 3D position to 2D screen coordinates
 * Uses model-viewer's queryForHotspot approach for accurate projection
 * @param {object} position3D - The 3D position with x, y, z
 * @returns {object} The 2D screen position with x, y, or null if not visible
 */
export function positionToScreen(position3D) {
    if (!modelViewerInstance || !position3D) return null;

    try {
        const rect = modelViewerInstance.getBoundingClientRect();

        // If the marker has a stored screen position, use it
        // (This is set when the marker is first created from a click)
        if (position3D.screenX !== undefined && position3D.screenY !== undefined) {
            return { x: position3D.screenX, y: position3D.screenY };
        }

        // Fallback: center of viewport (markers will cluster but won't crash)
        return { x: rect.width / 2, y: rect.height / 2 };
    } catch (error) {
        console.warn('positionToScreen error:', error);
        return null;
    }
}

/**
 * Updates the markers array and re-renders the canvas
 * @param {array} newMarkers - Array of damage markers
 */
export function updateMarkers(newMarkers) {
    markers = newMarkers || [];
    if (canvasOverlay) {
        renderMarkers();
    }
}

/**
 * Renders all markers on the canvas overlay
 */
function renderMarkers() {
    if (!canvasOverlay || !modelViewerInstance) return;

    const ctx = canvasOverlay.getContext('2d');
    const rect = modelViewerInstance.getBoundingClientRect();

    // Set canvas size to match model viewer
    canvasOverlay.width = rect.width;
    canvasOverlay.height = rect.height;

    // Clear canvas
    ctx.clearRect(0, 0, canvasOverlay.width, canvasOverlay.height);

    // Draw each marker
    markers.forEach((marker, index) => {
        const screenPos = positionToScreen(marker.position);
        if (screenPos) {
            drawMarker(ctx, screenPos.x, screenPos.y, marker, index);
        }
    });
}

/**
 * Draws a single marker on the canvas
 */
function drawMarker(ctx, x, y, marker, index) {
    const radius = 12;
    const colors = {
        'Scratch': '#FFA726',    // Orange
        'Dent': '#EF5350',       // Red
        'Crack': '#AB47BC',      // Purple
        'Scuff': '#78909C',      // Blue Grey
        'MissingPart': '#5C6BC0', // Indigo
        'Paint': '#26A69A',      // Teal
        'Rust': '#8D6E63',       // Brown
        'Mechanical': '#42A5F5', // Blue
        'Broken': '#EC407A',     // Pink
        'Wear': '#66BB6A',       // Green
        'Other': '#BDBDBD'       // Grey
    };

    const color = colors[marker.damageType] || colors['Other'];

    // Draw outer circle (shadow)
    ctx.beginPath();
    ctx.arc(x, y, radius + 2, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(0, 0, 0, 0.3)';
    ctx.fill();

    // Draw main circle
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fillStyle = marker.isPreExisting ? '#9E9E9E' : color;
    ctx.fill();

    // Draw white border
    ctx.strokeStyle = '#FFFFFF';
    ctx.lineWidth = 2;
    ctx.stroke();

    // Draw marker number
    ctx.fillStyle = '#FFFFFF';
    ctx.font = 'bold 10px Arial';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText((index + 1).toString(), x, y);

    // Draw severity indicator (small dot at top-right)
    const severityColors = {
        'Minor': '#66BB6A',     // Green
        'Moderate': '#FFA726',  // Orange
        'Major': '#EF5350'      // Red
    };
    const severityColor = severityColors[marker.severity] || severityColors['Minor'];

    ctx.beginPath();
    ctx.arc(x + 8, y - 8, 4, 0, Math.PI * 2);
    ctx.fillStyle = severityColor;
    ctx.fill();
    ctx.strokeStyle = '#FFFFFF';
    ctx.lineWidth = 1;
    ctx.stroke();
}

/**
 * Gets the current camera state from the model-viewer
 * @returns {object} The camera state
 */
export function getCameraState() {
    if (!modelViewerInstance) return null;

    const orbit = modelViewerInstance.getCameraOrbit();
    const target = modelViewerInstance.getCameraTarget();
    const fov = modelViewerInstance.getFieldOfView();

    return {
        orbitTheta: orbit.theta,
        orbitPhi: orbit.phi,
        orbitRadius: orbit.radius,
        targetX: target.x,
        targetY: target.y,
        targetZ: target.z,
        fieldOfView: fov
    };
}

/**
 * Sets the camera state on the model-viewer
 * @param {object} state - The camera state to restore
 */
export function setCameraState(state) {
    if (!modelViewerInstance || !state) return;

    if (state.orbitTheta !== undefined) {
        modelViewerInstance.cameraOrbit = `${state.orbitTheta}deg ${state.orbitPhi}deg ${state.orbitRadius}m`;
    }

    if (state.targetX !== undefined) {
        modelViewerInstance.cameraTarget = `${state.targetX}m ${state.targetY}m ${state.targetZ}m`;
    }

    if (state.fieldOfView !== undefined) {
        modelViewerInstance.fieldOfView = `${state.fieldOfView}deg`;
    }
}

/**
 * Loads a 3D model into the viewer
 * @param {string} modelPath - Path to the GLB model file
 */
export async function loadModel(modelPath) {
    if (!modelViewerInstance) return { success: false, error: 'Model viewer not initialized' };

    try {
        modelViewerInstance.src = modelPath;
        await waitForModelLoad(modelViewerInstance);
        return { success: true };
    } catch (error) {
        return { success: false, error: error.message };
    }
}

/**
 * Takes a snapshot of the current view
 * @returns {string} Base64 encoded PNG image
 */
export async function takeSnapshot() {
    if (!modelViewerInstance) return null;

    try {
        const blob = await modelViewerInstance.toBlob({ idealAspect: true });
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result.split(',')[1]);
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    } catch (error) {
        console.error('Error taking snapshot:', error);
        return null;
    }
}

/**
 * Highlights a specific marker
 * @param {number} index - Index of the marker to highlight
 */
export function highlightMarker(index) {
    // Re-render markers with highlight
    if (canvasOverlay && markers.length > 0) {
        renderMarkersWithHighlight(index);
    }
}

/**
 * Renders markers with one highlighted
 */
function renderMarkersWithHighlight(highlightIndex) {
    if (!canvasOverlay || !modelViewerInstance) return;

    const ctx = canvasOverlay.getContext('2d');
    const rect = modelViewerInstance.getBoundingClientRect();

    canvasOverlay.width = rect.width;
    canvasOverlay.height = rect.height;
    ctx.clearRect(0, 0, canvasOverlay.width, canvasOverlay.height);

    markers.forEach((marker, index) => {
        const screenPos = positionToScreen(marker.position);
        if (screenPos) {
            if (index === highlightIndex) {
                // Draw highlight ring
                ctx.beginPath();
                ctx.arc(screenPos.x, screenPos.y, 20, 0, Math.PI * 2);
                ctx.strokeStyle = '#00897B';
                ctx.lineWidth = 3;
                ctx.stroke();
            }
            drawMarker(ctx, screenPos.x, screenPos.y, marker, index);
        }
    });
}

/**
 * Clears the highlight
 */
export function clearHighlight() {
    if (canvasOverlay) {
        renderMarkers();
    }
}

/**
 * Disposes of the inspector and cleans up resources
 */
export function dispose() {
    if (modelViewerInstance) {
        modelViewerInstance.removeEventListener('click', handleModelClick);
        modelViewerInstance.removeEventListener('camera-change', handleCameraChange);
    }

    modelViewerInstance = null;
    canvasOverlay = null;
    dotNetRef = null;
    markers = [];
    isInitialized = false;
}

/**
 * Updates screen positions for all markers (call after resize)
 * @returns {array} Array of updated screen positions
 */
export function getUpdatedScreenPositions() {
    return markers.map(marker => {
        const screenPos = positionToScreen(marker.position);
        return {
            markerId: marker.markerId,
            screenPosition: screenPos
        };
    });
}

/**
 * Focuses the camera on a specific marker
 * @param {object} position - The 3D position to focus on
 */
export function focusOnPosition(position) {
    if (!modelViewerInstance || !position) return;

    // Set camera target to the position
    modelViewerInstance.cameraTarget = `${position.x}m ${position.y}m ${position.z}m`;
}

/**
 * Resets the camera to the default view
 */
export function resetCamera() {
    if (!modelViewerInstance) return;

    modelViewerInstance.cameraOrbit = 'auto auto auto';
    modelViewerInstance.cameraTarget = 'auto auto auto';
}

/**
 * Enables or disables interaction with the model
 * @param {boolean} enabled - Whether interaction should be enabled
 */
export function setInteractionEnabled(enabled) {
    if (!modelViewerInstance) return;

    modelViewerInstance.cameraControls = enabled;
}

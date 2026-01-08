// Signature Pad JavaScript Module
// Handles canvas drawing operations for signature capture

let canvasContexts = new Map();

export function initCanvas(canvasElement, strokeColor, strokeWidth) {
    if (!canvasElement) return;

    const ctx = canvasElement.getContext('2d');
    if (!ctx) return;

    // Set canvas size to match display size
    const rect = canvasElement.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;

    canvasElement.width = rect.width * dpr;
    canvasElement.height = rect.height * dpr;

    ctx.scale(dpr, dpr);
    ctx.strokeStyle = strokeColor || '#000000';
    ctx.lineWidth = strokeWidth || 2;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';

    // Fill with white background
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, rect.width, rect.height);

    canvasContexts.set(canvasElement, {
        ctx,
        strokeColor,
        strokeWidth,
        dpr
    });
}

export function getCanvasPosition(canvasElement, clientX, clientY) {
    if (!canvasElement) return [0, 0];

    const rect = canvasElement.getBoundingClientRect();
    const x = clientX - rect.left;
    const y = clientY - rect.top;

    return [x, y];
}

export function drawLine(canvasElement, x1, y1, x2, y2) {
    const data = canvasContexts.get(canvasElement);
    if (!data) return;

    const { ctx } = data;
    ctx.beginPath();
    ctx.moveTo(x1, y1);
    ctx.lineTo(x2, y2);
    ctx.stroke();
}

export function clearCanvas(canvasElement) {
    const data = canvasContexts.get(canvasElement);
    if (!data) return;

    const { ctx } = data;
    const rect = canvasElement.getBoundingClientRect();

    // Clear and fill with white
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, rect.width, rect.height);

    // Reset stroke style
    ctx.strokeStyle = data.strokeColor || '#000000';
    ctx.lineWidth = data.strokeWidth || 2;
}

export function getSignatureData(canvasElement) {
    if (!canvasElement) return null;

    // Return base64 PNG data URL
    return canvasElement.toDataURL('image/png');
}

export function isCanvasEmpty(canvasElement) {
    if (!canvasElement) return true;

    const ctx = canvasElement.getContext('2d');
    if (!ctx) return true;

    const rect = canvasElement.getBoundingClientRect();
    const imageData = ctx.getImageData(0, 0, rect.width, rect.height);
    const data = imageData.data;

    // Check if all pixels are white (255, 255, 255, 255)
    for (let i = 0; i < data.length; i += 4) {
        if (data[i] !== 255 || data[i + 1] !== 255 || data[i + 2] !== 255) {
            return false;
        }
    }

    return true;
}

// Handle window resize to reinitialize canvas
window.addEventListener('resize', () => {
    canvasContexts.forEach((data, canvasElement) => {
        const { strokeColor, strokeWidth } = data;
        // Note: This will clear the canvas. In a production app,
        // you might want to save and restore the signature data.
        initCanvas(canvasElement, strokeColor, strokeWidth);
    });
});

// File Upload JavaScript Module for MotoRent
// Handles drag-and-drop uploads, file compression, and EXIF extraction

export function showSelfieFileUpload() {
    const panel = document.getElementById("file-upload-panel");
    if (panel) panel.style.display = "";
}

async function updateProgress(dotNet, message) {
    try {
        await dotNet.invokeMethodAsync('UpdateProgress', message);
    } catch (error) {
        const errorPanel = document.getElementById("upload-error-panel");
        if (errorPanel) {
            errorPanel.classList.remove("d-none");
            errorPanel.innerHTML += `<pre>Error updating progress: ${JSON.stringify(error, null, 2)}</pre>`;
        }
        console.error(error);
    }
}

async function uploadFileAsync(dotNet, data, options) {
    let response = null;
    let result = null;

    try {
        const uri = options.uri || `/api/stores/upload-single?publicAccess=${options.publicAccess || false}`;

        response = await fetch(uri, {
            method: 'POST',
            body: data
        });

        await updateProgress(dotNet, {
            uploading: false,
            message: "Upload response: " + response.status
        });

        result = await response.json();
        await dotNet.invokeMethodAsync('InvokeOnChange', result);

    } catch (error) {
        const errorPanel = document.getElementById("upload-error-panel");
        if (errorPanel) {
            errorPanel.classList.remove("d-none");
            errorPanel.innerHTML += `<pre>Error uploading file: ${JSON.stringify(error, null, 2)}</pre>`;
        }
        console.error(error);

        // Try fallback error URL if provided
        if (options.errorUrl) {
            try {
                const response2 = await fetch(options.errorUrl, {
                    method: 'POST',
                    body: data
                });
                const result2 = await response2.json();
                await dotNet.invokeMethodAsync('InvokeOnChange', result2);
            } catch (e2) {
                console.error("Fallback upload also failed:", e2);
            }
        }
    }
}

async function compressImage(file) {
    // Check if imageCompression library is available
    if (typeof imageCompression === 'undefined') {
        console.warn('Image compression library not loaded, skipping compression');
        return file;
    }

    const options = {
        maxSizeMB: 0.4,
        maxWidthOrHeight: 720,
        useWebWorker: true
    };

    try {
        return await imageCompression(file, options);
    } catch (error) {
        console.warn('Image compression failed, using original:', error);
        return file;
    }
}

async function getExifData(file) {
    // Check if EXIF library is available
    if (typeof EXIF === 'undefined') {
        return null;
    }

    return new Promise((resolve) => {
        EXIF.getData(file, function() {
            resolve(EXIF.getAllTags(this));
        });
    });
}

async function fileChanged(element, file, dotNet, options) {
    const data = new FormData();

    try {
        await updateProgress(dotNet, { uploading: true, message: "Preparing file..." });

        if (file.type.startsWith("image") && options.compress) {
            await updateProgress(dotNet, { uploading: true, message: "Compressing image..." });

            const compressed = await compressImage(file);
            data.append('file', compressed, file.name);

            await updateProgress(dotNet, { uploading: true, message: "Compressed: " + file.name });

            // Extract EXIF data if available
            const exif = await getExifData(file);
            if (exif) {
                if (exif.GPSLatitude) data.append('x-Exif-GPSLatitude', `${exif.GPSLatitude}`);
                if (exif.GPSLongitude) data.append('x-Exif-GPSLongitude', `${exif.GPSLongitude}`);
                if (exif.DateTime) data.append('x-Exif-DateTime', exif.DateTime);
                if (exif.Model) data.append('x-Exif-Model', exif.Model);
                if (exif.Make) data.append('x-Exif-Make', exif.Make);
            }

            await updateProgress(dotNet, { uploading: true, message: "EXIF data collected..." });
        } else {
            data.append('file', file, file.name);
        }

        // Add prepend option if provided
        if (options.prepend) {
            data.append('x-prepend', options.prepend);
        }

        await updateProgress(dotNet, { uploading: true, message: "Uploading file..." });
        await uploadFileAsync(dotNet, data, options);

    } catch (error) {
        const errorPanel = document.getElementById("upload-error-panel");
        if (errorPanel) {
            errorPanel.classList.remove("d-none");
            errorPanel.innerHTML += `<pre>Error processing file: ${JSON.stringify(error, null, 2)}</pre>`;
        }
        console.error(error);
    }
}

export function startFileUpload(selector, dotNet, options) {
    if (!selector) {
        console.warn('File upload selector is null');
        return;
    }

    options = options || {};

    selector.addEventListener('change', async (event) => {
        if (event.currentTarget.files && event.currentTarget.files.length === 1) {
            await fileChanged(selector, event.currentTarget.files[0], dotNet, options);
        }
    });
}

export function startDropZone(element, dotNet, options) {
    if (!element) {
        console.warn('Dropzone element is null');
        return;
    }

    options = options || {};

    // Add magic-drop ignore attribute
    if (element.setAttribute) {
        element.setAttribute('data-magic-drop', 'ignore');
    }

    // Check if Dropzone library is available
    if (typeof Dropzone !== 'undefined') {
        const dropOptions = {
            autoProcessQueue: false,
            url: "/api/stores",
            clickable: true,
            maxFilesize: 10, // 10 MB
            acceptedFiles: "image/*,.pdf",
            dictDefaultMessage: ""
        };

        if (options.capture) {
            dropOptions.capture = options.capture;
        }

        try {
            const drop = new Dropzone(element, dropOptions);
            drop.on("addedfile", async (file) => {
                await fileChanged(element, file, dotNet, options);
            });
        } catch (error) {
            console.warn('Dropzone initialization failed, using fallback:', error);
            useFallbackDrop(element, dotNet, options);
        }
    } else {
        // Fallback: use native drag-and-drop
        useFallbackDrop(element, dotNet, options);
    }
}

function useFallbackDrop(element, dotNet, options) {
    // Add click handler to open file dialog
    element.addEventListener('click', () => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*,.pdf';

        if (options.capture) {
            input.capture = options.capture;
        }

        input.onchange = async (e) => {
            if (e.target.files && e.target.files.length > 0) {
                await fileChanged(element, e.target.files[0], dotNet, options);
            }
        };

        input.click();
    });

    // Drag and drop handlers
    element.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
        element.classList.add('dz-drag-hover');
    });

    element.addEventListener('dragleave', (e) => {
        e.preventDefault();
        e.stopPropagation();
        element.classList.remove('dz-drag-hover');
    });

    element.addEventListener('drop', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        element.classList.remove('dz-drag-hover');

        const files = e.dataTransfer.files;
        if (files && files.length > 0) {
            await fileChanged(element, files[0], dotNet, options);
        }
    });
}

export function setDropZoneOptions(element, dotNet, options) {
    // Reserved for future options updates
}

/**
 * Registers a drop zone that reads dropped files as base64 and sends to Blazor
 * Used for components that need to process files directly rather than upload them
 */
export function registerBase64DropZone(element, dotNet, callbackMethod) {
    if (!element) {
        console.warn('Drop zone element is null');
        return;
    }

    const handleFile = async (file) => {
        if (!file) return;

        try {
            const reader = new FileReader();
            reader.onload = async (e) => {
                const base64 = e.target.result.split(',')[1]; // Remove data:mime;base64, prefix
                await dotNet.invokeMethodAsync(callbackMethod, {
                    name: file.name,
                    contentType: file.type,
                    size: file.size,
                    base64Data: base64
                });
            };
            reader.readAsDataURL(file);
        } catch (error) {
            console.error('Error reading dropped file:', error);
        }
    };

    element.addEventListener('drop', async (e) => {
        e.preventDefault();
        e.stopPropagation();

        const files = e.dataTransfer?.files;
        if (files && files.length > 0) {
            await handleFile(files[0]);
        }
    });

    element.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
    });
}

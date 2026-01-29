// Designer Splitter - drag-to-resize panel handles
let instances = new Map();

export function initSplitter(containerEl, dotNetRef) {
    if (!containerEl || instances.has(containerEl)) return;

    const state = {
        dotNetRef,
        activeHandle: null,
        startX: 0,
        startWidth: 0,
        onMouseMove: null,
        onMouseUp: null
    };

    const handles = containerEl.querySelectorAll('.designer-splitter');
    handles.forEach(handle => {
        handle.addEventListener('mousedown', e => onMouseDown(e, handle, containerEl, state));
        handle.addEventListener('dblclick', e => onDoubleClick(e, handle, containerEl, state));
    });

    instances.set(containerEl, { state, handles });
}

function onMouseDown(e, handle, container, state) {
    e.preventDefault();
    const panel = handle.dataset.panel;
    state.activeHandle = panel;
    state.startX = e.clientX;

    if (panel === 'left') {
        const leftPanel = container.querySelector('.designer-panel-left');
        state.startWidth = leftPanel.getBoundingClientRect().width;
    } else {
        const rightPanel = container.querySelector('.designer-panel-right');
        state.startWidth = rightPanel.getBoundingClientRect().width;
    }

    handle.classList.add('active');
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';

    state.onMouseMove = e2 => onMouseMove(e2, container, state);
    state.onMouseUp = () => onMouseUp(handle, container, state);

    document.addEventListener('mousemove', state.onMouseMove);
    document.addEventListener('mouseup', state.onMouseUp);
}

function onMouseMove(e, container, state) {
    const dx = e.clientX - state.startX;

    if (state.activeHandle === 'left') {
        const newWidth = Math.min(400, Math.max(180, state.startWidth + dx));
        container.style.setProperty('--left-width', newWidth + 'px');
    } else {
        const newWidth = Math.min(450, Math.max(200, state.startWidth - dx));
        container.style.setProperty('--right-width', newWidth + 'px');
    }
}

function onMouseUp(handle, container, state) {
    handle.classList.remove('active');
    document.body.style.cursor = '';
    document.body.style.userSelect = '';

    document.removeEventListener('mousemove', state.onMouseMove);
    document.removeEventListener('mouseup', state.onMouseUp);

    const leftWidth = parseInt(getComputedStyle(container).getPropertyValue('--left-width')) || 220;
    const rightWidth = parseInt(getComputedStyle(container).getPropertyValue('--right-width')) || 280;

    state.dotNetRef.invokeMethodAsync('OnPanelResized', leftWidth, rightWidth);
    state.activeHandle = null;
}

function onDoubleClick(e, handle, container, state) {
    const panel = handle.dataset.panel;
    state.dotNetRef.invokeMethodAsync('OnPanelToggle', panel);
}

export function triggerFileInput(inputElement) {
    if (inputElement) inputElement.click();
}

export function setPanelWidths(containerEl, leftWidth, rightWidth) {
    if (!containerEl) return;
    containerEl.style.setProperty('--left-width', leftWidth + 'px');
    containerEl.style.setProperty('--right-width', rightWidth + 'px');
}

export function dispose(containerEl) {
    const instance = instances.get(containerEl);
    if (!instance) return;

    instance.handles.forEach(handle => {
        handle.replaceWith(handle.cloneNode(true));
    });

    instances.delete(containerEl);
}

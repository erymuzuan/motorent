// designer-editor.js – contenteditable + context-menu management for the template designer
const editors = new Map();

// ─── Editor lifecycle ───────────────────────────────────────────────
export function initEditor(blockEl, dotNetRef, blockId) {
    if (editors.has(blockId)) destroyEditor(blockId);

    const contentDiv = blockEl.querySelector('.block-content > div');
    if (!contentDiv) return;

    contentDiv.setAttribute('contenteditable', 'true');
    contentDiv.style.outline = 'none';
    contentDiv.style.minHeight = '1em';

    const state = {
        el: contentDiv,
        dotNetRef,
        blockId,
        savedRange: null,
    };

    contentDiv.addEventListener('focus', () => onFocus(state));
    contentDiv.addEventListener('blur', (e) => onBlur(state, e));
    contentDiv.addEventListener('contextmenu', (e) => onContextMenu(state, e));
    contentDiv.addEventListener('input', () => onInput(state));

    state._onFocus = () => onFocus(state);
    state._onBlur = (e) => onBlur(state, e);
    state._onContextMenu = (e) => onContextMenu(state, e);
    state._onInput = () => onInput(state);

    editors.set(blockId, state);
}

export function destroyEditor(blockId) {
    const state = editors.get(blockId);
    if (!state) return;

    state.el.removeAttribute('contenteditable');
    state.el.removeEventListener('focus', state._onFocus);
    state.el.removeEventListener('blur', state._onBlur);
    state.el.removeEventListener('contextmenu', state._onContextMenu);
    state.el.removeEventListener('input', state._onInput);

    editors.delete(blockId);
}

// ─── Events ─────────────────────────────────────────────────────────
function onFocus(state) {
    state.dotNetRef.invokeMethodAsync('OnEditingStarted');
}

function onBlur(state, e) {
    // Don't clear editing state if focus moved into the context menu,
    // placeholder picker, or right panel (for placeholder insertion into canvas)
    const target = e.relatedTarget;
    if (target && (
        target.closest('.designer-context-menu') ||
        target.closest('.placeholder-picker') ||
        target.closest('.designer-panel-right')
    )) {
        // Still sync content but keep editing state active
        state.dotNetRef.invokeMethodAsync('OnContentChanged', state.el.innerHTML);
        return;
    }

    state.dotNetRef.invokeMethodAsync('OnContentChanged', state.el.innerHTML);
    state.dotNetRef.invokeMethodAsync('OnEditingStopped');
}

function onInput(state) {
    // Save range after every keystroke so context-menu can restore it
    saveSelection(state);
}

function onContextMenu(state, e) {
    e.preventDefault();
    saveSelection(state);
    // Dispatch custom event so Blazor can pick it up
    state.el.dispatchEvent(new CustomEvent('designer-contextmenu', {
        bubbles: true,
        detail: { x: e.clientX, y: e.clientY, blockId: state.blockId }
    }));
}

// ─── Selection helpers ──────────────────────────────────────────────
function saveSelection(state) {
    const sel = window.getSelection();
    if (sel.rangeCount > 0 && state.el.contains(sel.anchorNode)) {
        state.savedRange = sel.getRangeAt(0).cloneRange();
    }
}

function restoreSelection(state) {
    if (!state.savedRange) return false;
    const sel = window.getSelection();
    sel.removeAllRanges();
    sel.addRange(state.savedRange);
    return true;
}

// ─── Context menu positioning ───────────────────────────────────────
export function showContextMenu(menuEl, x, y) {
    menuEl.style.display = 'block';
    menuEl.style.left = x + 'px';
    menuEl.style.top = y + 'px';

    // Clamp to viewport
    requestAnimationFrame(() => {
        const rect = menuEl.getBoundingClientRect();
        if (rect.right > window.innerWidth) {
            menuEl.style.left = (window.innerWidth - rect.width - 8) + 'px';
        }
        if (rect.bottom > window.innerHeight) {
            menuEl.style.top = (window.innerHeight - rect.height - 8) + 'px';
        }
    });
}

export function hideContextMenu(menuEl) {
    if (menuEl) menuEl.style.display = 'none';
}

// ─── Formatting commands ────────────────────────────────────────────
export function execFormat(blockId, command, value) {
    const state = editors.get(blockId);
    if (!state) return;

    state.el.focus();
    restoreSelection(state);

    if (command === 'fontSize') {
        // execCommand fontSize uses 1-7 scale which is imprecise.
        // Use insertHTML with a span for exact px values.
        const sel = window.getSelection();
        if (sel.rangeCount > 0 && !sel.isCollapsed) {
            const range = sel.getRangeAt(0);
            const span = document.createElement('span');
            span.style.fontSize = value + 'px';
            range.surroundContents(span);
            sel.removeAllRanges();
            sel.addRange(range);
        }
    } else if (command === 'justifyLeft' || command === 'justifyCenter' || command === 'justifyRight') {
        document.execCommand(command, false, null);
    } else {
        document.execCommand(command, false, value || null);
    }

    saveSelection(state);
    // Sync content back
    state.dotNetRef.invokeMethodAsync('OnContentChanged', state.el.innerHTML);
}

// ─── Insert placeholder at caret ────────────────────────────────────
export function insertAtCaret(blockId, text) {
    const state = editors.get(blockId);
    if (!state) return;

    state.el.focus();
    restoreSelection(state);
    document.execCommand('insertText', false, text);
    saveSelection(state);
    state.dotNetRef.invokeMethodAsync('OnContentChanged', state.el.innerHTML);
}

// ─── Sync content for external edits ────────────────────────────────
export function setContent(blockId, html) {
    const state = editors.get(blockId);
    if (!state) return;
    state.el.innerHTML = html;
}

// ─── Global click-away handler for context menu ─────────────────────
let _clickAwayHandler = null;
export function addClickAwayListener(menuEl) {
    if (_clickAwayHandler) document.removeEventListener('mousedown', _clickAwayHandler);
    _clickAwayHandler = (e) => {
        if (!menuEl.contains(e.target)) {
            menuEl.style.display = 'none';
        }
    };
    document.addEventListener('mousedown', _clickAwayHandler);
}

export function removeClickAwayListener() {
    if (_clickAwayHandler) {
        document.removeEventListener('mousedown', _clickAwayHandler);
        _clickAwayHandler = null;
    }
}

# MotoRent CSS Styling Patterns

This skill documents the CSS styling conventions using `mr-` prefixed classes for consistent UI across the application.

## CSS Location
All custom styles are in: `src/MotoRent.Server/wwwroot/css/site.css`

## Naming Convention
- All MotoRent-specific CSS classes use `mr-` prefix
- Tabler/Bootstrap variables use `--tblr-` prefix
- MotoRent CSS variables use `--mr-` prefix

## Brand Colors
```css
--tblr-primary: #00897B;        /* Tropical Teal */
--tblr-primary-darken: #00695C;
--tblr-primary-lighten: #4DB6AC;
--tblr-secondary: #FF7043;      /* Deep Orange */
```

## Gradient Navbar Pattern

The header and menu use a single unified gradient wrapper:

### HTML Structure (MainLayout.razor)
```razor
<div class="mr-navbar-gradient">
    <header class="navbar navbar-expand-md d-print-none sticky-top">
        <!-- Logo and user menu -->
    </header>
    <div class="navbar-expand-md">
        <nav class="navbar navbar-expand">
            <NavMenu />
        </nav>
    </div>
</div>
```

### CSS Classes
```css
/* Single gradient wrapper for both header and menu */
.mr-navbar-gradient {
    background: linear-gradient(180deg, #0d6560 0%, #0f766e 50%, #14b8a6 100%);
    position: sticky;
    top: 0;
    z-index: 1030;
}

/* All text inside gradient should be white */
.mr-navbar-gradient .nav-link {
    color: rgba(255, 255, 255, 0.9) !important;
}

/* Dark mode adjustment */
[data-bs-theme="dark"] .mr-navbar-gradient {
    background: linear-gradient(180deg, #0a4f4b 0%, #0d5d58 50%, #0f766e 100%);
}
```

## Component Classes

### Page Headers
```html
<div class="d-flex justify-content-between align-items-start flex-wrap gap-3 mb-4">
    <div>
        <h1 class="page-title mb-1">
            <i class="ti ti-receipt me-2 text-primary"></i>Page Title
        </h1>
        <p class="text-secondary mb-0">Page description</p>
    </div>
    <a href="/action" class="mr-btn-primary-action">
        <i class="ti ti-plus"></i>
        Action Button
    </a>
</div>
```

### Primary Action Button
```css
.mr-btn-primary-action {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 12px 24px;
    background: linear-gradient(135deg, #0f766e 0%, #14b8a6 100%);
    color: white;
    border-radius: 10px;
    font-weight: 500;
    text-decoration: none;
    box-shadow: 0 4px 14px rgba(0, 137, 123, 0.25);
    transition: all 0.2s ease;
}

.mr-btn-primary-action:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 20px rgba(0, 137, 123, 0.35);
    color: white;
}
```

### Summary Cards
```html
<div class="mr-summary-cards">
    <div class="mr-summary-card">
        <div class="mr-summary-icon mr-summary-icon-primary">
            <i class="ti ti-motorbike"></i>
        </div>
        <div class="mr-summary-content">
            <div class="mr-summary-label">Active</div>
            <div class="mr-summary-value">5</div>
        </div>
    </div>
</div>
```

### Cards with Enhanced Styling
```css
.mr-card {
    background: var(--mr-bg-card);
    border: 1px solid var(--mr-border-default);
    border-radius: 16px;
    box-shadow: 0 1px 3px rgba(0,0,0,0.04), 0 6px 16px rgba(0,0,0,0.06);
    transition: all 0.2s ease;
}

.mr-card:hover {
    box-shadow: 0 4px 12px rgba(0,0,0,0.08), 0 16px 32px rgba(0,0,0,0.1);
}
```

### Filter Tabs
```html
<div class="mr-filter-tabs">
    <button class="mr-filter-tab active">All</button>
    <button class="mr-filter-tab">Active</button>
    <button class="mr-filter-tab">Completed</button>
</div>
```

### Status Badges
```css
.mr-status-badge {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 6px 14px;
    border-radius: 20px;
    font-size: 13px;
    font-weight: 500;
}

.mr-status-badge-success {
    background: linear-gradient(135deg, rgba(34, 197, 94, 0.1) 0%, rgba(34, 197, 94, 0.05) 100%);
    color: #16a34a;
    border: 1px solid rgba(34, 197, 94, 0.2);
}

.mr-status-badge-warning {
    background: linear-gradient(135deg, rgba(245, 158, 11, 0.1) 0%, rgba(245, 158, 11, 0.05) 100%);
    color: #d97706;
    border: 1px solid rgba(245, 158, 11, 0.2);
}

.mr-status-badge-danger {
    background: linear-gradient(135deg, rgba(239, 68, 68, 0.1) 0%, rgba(239, 68, 68, 0.05) 100%);
    color: #dc2626;
    border: 1px solid rgba(239, 68, 68, 0.2);
}
```

### Field Labels and Values
```html
<div class="mr-field-grid">
    <div>
        <div class="mr-field-label">Renter</div>
        <div class="mr-field-value">John Doe</div>
    </div>
    <div>
        <div class="mr-field-label">Amount</div>
        <div class="mr-field-value mr-mono mr-highlight">1,350 THB</div>
    </div>
</div>
```

```css
.mr-field-label {
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    color: var(--mr-text-muted);
    margin-bottom: 4px;
}

.mr-field-value {
    font-size: 14px;
    color: var(--mr-text-primary);
}

.mr-mono {
    font-family: 'JetBrains Mono', monospace;
}

.mr-highlight {
    color: var(--mr-accent-primary);
    font-weight: 600;
}
```

## Animation Classes
```css
.mr-animate-in {
    animation: mr-fade-in-up 0.4s ease-out forwards;
    opacity: 0;
}

@keyframes mr-fade-in-up {
    from {
        opacity: 0;
        transform: translateY(10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.mr-animate-delay-1 { animation-delay: 0.1s; }
.mr-animate-delay-2 { animation-delay: 0.2s; }
.mr-animate-delay-3 { animation-delay: 0.3s; }
```

## Theme Transition
```css
.mr-theme-transition {
    transition: background-color 0.3s ease, color 0.3s ease, border-color 0.3s ease;
}
```

## CSS Variables Reference

### Light Theme (default)
```css
:root {
    --mr-bg-body: linear-gradient(180deg, #f8fafc 0%, #f1f5f9 100%);
    --mr-bg-card: #ffffff;
    --mr-bg-header-gradient: linear-gradient(135deg, #0f766e 0%, #14b8a6 100%);
    --mr-border-default: #e2e8f0;
    --mr-text-primary: #1e293b;
    --mr-text-secondary: #475569;
    --mr-text-muted: #94a3b8;
    --mr-accent-primary: #0f766e;
    --mr-accent-light: #14b8a6;
}
```

### Dark Theme
```css
[data-bs-theme="dark"] {
    --mr-bg-body: linear-gradient(180deg, #0f172a 0%, #1e293b 100%);
    --mr-bg-card: #1e293b;
    --mr-border-default: #334155;
    --mr-text-primary: #f1f5f9;
    --mr-text-secondary: #cbd5e1;
    --mr-text-muted: #64748b;
}
```

## Usage Guidelines

1. **Always use `mr-` prefix** for MotoRent-specific classes
2. **Use CSS variables** for colors to support dark mode
3. **Apply `mr-theme-transition`** to containers for smooth theme switching
4. **Use gradient backgrounds** for primary actions and headers
5. **Follow border-radius conventions**:
   - Cards: `16px`
   - Buttons: `10px`
   - Badges: `20px` (pill) or `8px`
   - Inputs: `10px`
6. **Use Tabler icons** (`ti ti-*`) for consistency

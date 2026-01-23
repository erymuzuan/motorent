# Safe & Go Marketing Website

## Overview
Static marketing website for "Safe & Go" - a motorbike rental SaaS platform targeting rental shops in Thailand (Phuket, Krabi, Samui, etc.).

## Implementation Status: COMPLETE

All files have been created and the website is functional.

---

## Site Structure

```
website/
├── index.html          # Landing page with carousel
├── pricing.html        # Pricing & plans (updated pricing)
├── features.html       # Feature showcase
├── contact.html        # Contact/demo request
├── css/
│   ├── style.css       # Main styles + carousel
│   └── variables.css   # CSS custom properties
├── js/
│   └── main.js         # Navbar, FAQ, carousel, form validation
├── images/
│   ├── logo.svg        # Safe & Go logo (teal)
│   ├── logo-white.svg  # White version for footer
│   ├── hero-pattern.svg # Decorative pattern
│   └── features/       # Feature screenshots
└── fonts/              # (Google Fonts via CDN)
```

---

## Current Pricing (Updated)

| Feature | Free | Pro ฿499/mo | Ultra ฿4,999/mo |
|---------|------|-------------|-----------------|
| Vehicles | 3 | 10 (start) +฿99/each | 20 (start) +฿199/each |
| Shops | 1 | 3 | Unlimited |
| Staff Users | 2 | 10 | Unlimited |
| Tourist Portal | - | ✓ | ✓ + Branded |
| Document OCR | - | 100/mo | Unlimited + API |
| PromptPay QR | - | ✓ | ✓ |
| LINE OA Integration | - | ✓ | ✓ |
| Reports | - | Standard | Advanced |
| Support | Community | Email | Priority Phone |

---

## Design System

### Colors
```css
:root {
  --primary: #00897B;        /* Tropical Teal */
  --primary-dark: #004D40;   /* Dark Teal */
  --accent: #FF7043;         /* Deep Orange - CTAs */
  --bg-light: #F8FAFC;       /* Light background */
  --bg-dark: #0F172A;        /* Footer background */
}
```

### Typography
- **Font**: Kanit (Thai) + Inter (English fallback)
- **Source**: Google Fonts CDN

### Key Components
- Responsive navbar with mobile hamburger menu
- Hero section with gradient background
- Carousel with YouTube embed and image slides
- Pricing cards with "Popular" badge
- Feature comparison table
- FAQ accordion
- Contact form with validation

---

## Features Implemented

### Landing Page (index.html)
- [x] Responsive navbar (transparent → solid on scroll)
- [x] Hero section with bilingual text (TH/EN)
- [x] **Carousel section** with YouTube video + feature images
- [x] Social proof bar
- [x] 6 feature cards
- [x] "How it works" 3-step section
- [x] Pricing preview (3 tiers)
- [x] Testimonials (3 cards)
- [x] CTA section
- [x] Footer with LINE/Facebook links

### Pricing Page (pricing.html)
- [x] 3 pricing cards (Free, Pro, Ultra)
- [x] Per-vehicle pricing model
- [x] Full feature comparison table
- [x] FAQ accordion (6 questions)

### Features Page (features.html)
- [x] 6 feature sections with alternating layouts
- [x] Additional features grid

### Contact Page (contact.html)
- [x] Contact form with validation
- [x] Contact info (LINE, Facebook, Email, Phone)
- [x] FAQ preview

### JavaScript (main.js)
- [x] Navbar scroll effect
- [x] Mobile navigation toggle
- [x] FAQ accordion
- [x] Contact form validation
- [x] Smooth scroll
- [x] Scroll animations
- [x] **Carousel with autoplay**

---

## To View the Website

```bash
cd E:\project\work\motorent.website\website
npx serve .
# or just open index.html in browser
```

---

## Thai Market Considerations

- Bilingual text (Thai primary, English secondary)
- LINE contact prominently displayed (@safeandgo)
- Thai Baht pricing with ฿ symbol
- Kanit font for Thai readability
- Locations: Phuket, Krabi, Samui, Pattaya, Chiang Mai
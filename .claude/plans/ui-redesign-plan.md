# MotoRent UI Redesign Implementation Plan

## Overview

Implement a role-based UI system combining three distinct views optimized for different user types in a Thai tourist motorbike rental shop.

| View | Target Users | Design Philosophy |
|------|--------------|-------------------|
| **Staff Kiosk** | Front-desk staff | Large touch targets, speed-focused, minimal navigation |
| **Tourist Portal** | Tourists/Renters | Visual card grid, self-service browsing, mobile-first |
| **Manager Dashboard** | Shop managers/owners | Data visualization, charts, comprehensive metrics |

---

## Phase 1: Layout Infrastructure

### 1.1 Create Role-Based Layout System

**New Files:**
- `src/MotoRent.Client/Layout/StaffLayout.razor` - Kiosk-style layout with bottom nav
- `src/MotoRent.Client/Layout/TouristLayout.razor` - Minimal header, visual-focused
- `src/MotoRent.Client/Layout/ManagerLayout.razor` - Enhanced sidebar with analytics nav

**Modify:**
- `src/MotoRent.Client/Routes.razor` - Add layout routing logic based on URL prefix

**Layout Routing Strategy:**
```
/staff/*     → StaffLayout (kiosk mode)
/tourist/*   → TouristLayout (browse mode)
/manager/*   → ManagerLayout (dashboard mode)
/browse/*    → TouristLayout (public browse)
/*           → MainLayout (default, redirect based on role)
```

### 1.2 StaffLayout.razor Design

```
┌────────────────────────────────────────────────────┐
│  [≡]  MotoRent - {ShopName}           [?] [👤]    │
│       {CurrentTime}                                │
├────────────────────────────────────────────────────┤
│                                                    │
│                   @Body                            │
│              (Full height content)                 │
│                                                    │
├────────────────────────────────────────────────────┤
│  [🏠]      [🛵]      [👥]      [💰]      [⚙️]     │
│  Home    Check-In  Rentals  Payments  More        │
└────────────────────────────────────────────────────┘
```

**Key Features:**
- Fixed bottom navigation bar (`MudBottomNav` or custom)
- Large clock display in header
- Shop name prominently displayed
- Minimal top bar (no side drawer)
- Full viewport height for content
- Touch-optimized (min 48px targets)

### 1.3 TouristLayout.razor Design

```
┌────────────────────────────────────────────────────┐
│  🛵 MotoRent          [🌐 EN ▼]  [My Rentals]     │
├────────────────────────────────────────────────────┤
│                                                    │
│                   @Body                            │
│                                                    │
└────────────────────────────────────────────────────┘
│  Powered by MotoRent | Contact: +66...            │
└────────────────────────────────────────────────────┘
```

**Key Features:**
- Clean, minimal header
- Language selector (EN/TH/RU/ZH)
- No sidebar navigation
- Footer with contact info
- Mobile-first responsive

### 1.4 ManagerLayout.razor Design

```
┌────────────────────────────────────────────────────┐
│  MotoRent Manager    [🔔 3] [Dark] [👤 Admin ▼]   │
├─────────────┬──────────────────────────────────────┤
│ Dashboard   │                                      │
│ ──────────  │                                      │
│ Analytics   │          @Body                       │
│  ├ Revenue  │                                      │
│  ├ Fleet    │                                      │
│  └ Renters  │                                      │
│ ──────────  │                                      │
│ Operations  │                                      │
│  ├ Rentals  │                                      │
│  ├ Payments │                                      │
│  └ Deposits │                                      │
│ ──────────  │                                      │
│ Settings    │                                      │
└─────────────┴──────────────────────────────────────┘
```

**Key Features:**
- Enhanced sidebar with grouped navigation
- Notification bell with count badge
- User dropdown with role/shop info
- Collapsible nav groups
- Quick date range selector in header

---

## Phase 2: Staff Kiosk View (Option 2)

### 2.1 Staff Home Page

**File:** `src/MotoRent.Client/Pages/Staff/Index.razor`
**Route:** `/staff`

**Layout:**
```
┌────────────────────────────────────────────────────┐
│                                                    │
│   ┌──────────────────┐    ┌──────────────────┐    │
│   │      🛵          │    │       🔙         │    │
│   │   NEW CHECK-IN   │    │   RETURN BIKE    │    │
│   │                  │    │                  │    │
│   │   Start rental   │    │  Process return  │    │
│   └──────────────────┘    └──────────────────┘    │
│                                                    │
│   ┌──────────────────┐    ┌──────────────────┐    │
│   │      📋         │    │       ⚠️          │    │
│   │  ACTIVE (12)    │    │   OVERDUE (2)    │    │
│   │                  │    │                  │    │
│   │  View current    │    │  Needs attention │    │
│   └──────────────────┘    └──────────────────┘    │
│                                                    │
│   ┌─────────────────────────────────────────┐     │
│   │  DUE TODAY: 5 bikes returning           │     │
│   │  Honda Click (A-1234) - John - 2:00 PM  │     │
│   │  Yamaha Aerox (B-5678) - Maria - 4:00 PM│     │
│   └─────────────────────────────────────────┘     │
│                                                    │
└────────────────────────────────────────────────────┘
```

**Components:**
- `StaffActionCard.razor` - Large touch-friendly action card
- `DueTodayList.razor` - Compact list of today's returns
- `OverdueAlert.razor` - Warning banner for overdue rentals

**Implementation Details:**
```csharp
// StaffActionCard.razor
<MudPaper @onclick="OnClick"
          Class="staff-action-card pa-6"
          Elevation="3"
          Style="min-height: 180px; cursor: pointer;">
    <MudStack AlignItems="AlignItems.Center" Justify="Justify.Center" Style="height: 100%;">
        <MudIcon Icon="@Icon" Size="Size.Large" Style="font-size: 3.5rem;" Color="@IconColor" />
        <MudText Typo="Typo.h5" Align="Align.Center">@Title</MudText>
        <MudText Typo="Typo.body2" Color="Color.Secondary" Align="Align.Center">@Subtitle</MudText>
        @if (BadgeCount > 0)
        {
            <MudBadge Content="@BadgeCount" Color="@BadgeColor" Overlap="false" Class="mt-2">
                <span></span>
            </MudBadge>
        }
    </MudStack>
</MudPaper>
```

**CSS (staff.css):**
```css
.staff-action-card {
    min-height: 180px;
    border-radius: 16px;
    transition: transform 0.2s, box-shadow 0.2s;
}

.staff-action-card:hover,
.staff-action-card:active {
    transform: scale(1.02);
    box-shadow: 0 8px 24px rgba(0,0,0,0.15);
}

.staff-action-card:active {
    transform: scale(0.98);
}

/* Touch-friendly minimum sizes */
.staff-touch-target {
    min-width: 48px;
    min-height: 48px;
}
```

### 2.2 Staff Quick Check-In

**File:** `src/MotoRent.Client/Pages/Staff/QuickCheckIn.razor`
**Route:** `/staff/checkin`

Simplified 3-step flow (vs. current 5-step):

**Step 1: Scan/Select**
- Large search box for renter (phone/passport)
- QR code scanner option (future)
- Recent renters grid (6 cards)
- "New Renter" prominent button

**Step 2: Pick Bike**
- Visual grid of available bikes only
- Large cards with photos
- Quick filters (scooter/manual, cc range)
- Tap to select, tap again to confirm

**Step 3: Confirm & Pay**
- Combined deposit + confirmation
- Large summary card
- Quick payment buttons (Cash/Card/QR)
- One-tap complete

### 2.3 Staff Active Rentals

**File:** `src/MotoRent.Client/Pages/Staff/ActiveRentals.razor`
**Route:** `/staff/rentals`

**Layout:**
```
┌────────────────────────────────────────────────────┐
│ [🔍 Search...]  [All ▼] [Due Today ▼] [Sort ▼]    │
├────────────────────────────────────────────────────┤
│ ┌────────────────────────────────────────────────┐ │
│ │ 🟢 John Smith          Honda Click 125        │ │
│ │    +66 812 345 6789    A-1234                 │ │
│ │    Due: Today 4:00 PM  Day 3 of 5            │ │
│ │    [📞 Call]  [💬 Line]  [🔙 Return]         │ │
│ └────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────┐ │
│ │ 🟠 Maria Garcia        Yamaha Aerox 155       │ │
│ │    +66 898 765 4321    B-5678                 │ │
│ │    Due: Tomorrow       Day 1 of 3            │ │
│ │    [📞 Call]  [💬 Line]  [➕ Extend]          │ │
│ └────────────────────────────────────────────────┘ │
│ ┌────────────────────────────────────────────────┐ │
│ │ 🔴 Alex Brown          Honda PCX 160          │ │
│ │    +66 876 543 2109    C-9012     ⚠️ OVERDUE │ │
│ │    Due: Yesterday      Day 8 of 7            │ │
│ │    [📞 Call]  [💬 Line]  [🔙 Return]         │ │
│ └────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────┘
```

**Features:**
- Swipe actions on cards (extend, return, call)
- Color-coded status (green=ok, orange=due soon, red=overdue)
- Quick action buttons inline
- Pull-to-refresh on mobile
- Tap card for full details

### 2.4 Staff Return (Check-Out)

**File:** `src/MotoRent.Client/Pages/Staff/Return.razor`
**Route:** `/staff/return`

**Flow:**
1. Scan/search bike (by plate number)
2. Condition checklist with photo capture
3. Calculate charges (damage, late fees, fuel)
4. Process refund or collect additional payment
5. Release deposit

**Simplified UI:**
```
┌────────────────────────────────────────────────────┐
│          RETURNING: Honda Click 125               │
│               Plate: A-1234                        │
├────────────────────────────────────────────────────┤
│  Renter: John Smith                               │
│  Started: Jan 5, 2026                             │
│  Days: 4 (1 extra day)                            │
├────────────────────────────────────────────────────┤
│  ☑ Bike condition OK                              │
│  ☐ Fuel level checked                             │
│  ☐ Keys returned                                  │
│  ☐ Helmet returned                                │
├────────────────────────────────────────────────────┤
│  Rental Total:     ฿1,400                         │
│  Extra Day:        ฿350                           │
│  Damage:           ฿0                             │
│  ─────────────────────────                        │
│  TOTAL DUE:        ฿1,750                         │
│  Deposit Held:     ฿3,000                         │
│  REFUND:           ฿1,250                         │
├────────────────────────────────────────────────────┤
│      [  COMPLETE RETURN & REFUND ฿1,250  ]       │
└────────────────────────────────────────────────────┘
```

---

## Phase 3: Tourist Portal Enhancement (Option 4)

### 3.1 Enhanced Browse Page

**File:** `src/MotoRent.Client/Pages/Tourist/Browse.razor` (modify existing)

**Enhancements:**
- Larger card images (16:9 aspect ratio)
- Prominent availability badge overlay
- Price displayed as "From ฿XXX/day"
- Heart/favorite button (localStorage)
- Quick compare feature (select up to 3)
- Filter chips below search (sticky on scroll)

**Card Design:**
```
┌─────────────────────────┐
│  ┌───────────────────┐  │
│  │                   │  │
│  │   [BIKE PHOTO]    │  │
│  │                   │  │
│  │  🟢 Available     │  │
│  └───────────────────┘  │
│  Honda Click 125        │
│  ⭐ 4.8 (24 reviews)    │
│  125cc • Automatic      │
│                         │
│  From ฿350/day          │
│                         │
│  [♡]  [  RESERVE  ]    │
└─────────────────────────┘
```

### 3.2 New: Comparison View

**File:** `src/MotoRent.Client/Pages/Tourist/Compare.razor`
**Route:** `/compare`

Side-by-side comparison of 2-3 selected bikes:

```
┌───────────────┬───────────────┬───────────────┐
│  Honda Click  │ Yamaha Aerox  │  Honda PCX    │
│   [PHOTO]     │   [PHOTO]     │   [PHOTO]     │
├───────────────┼───────────────┼───────────────┤
│  ฿350/day     │  ฿450/day     │  ฿500/day     │
│  125cc        │  155cc        │  160cc        │
│  Automatic    │  Automatic    │  Automatic    │
│  2023         │  2024         │  2024         │
│  ฿3,000 dep   │  ฿5,000 dep   │  ฿5,000 dep   │
├───────────────┼───────────────┼───────────────┤
│  [RESERVE]    │  [RESERVE]    │  [RESERVE]    │
└───────────────┴───────────────┴───────────────┘
```

### 3.3 Enhanced Rental History

**File:** `src/MotoRent.Client/Pages/Tourist/RentalHistory.razor` (modify existing)

**Enhancements:**
- QR code for current rental (show to staff)
- Digital receipt download
- Extend rental button
- Report issue button
- Rating/review after completion

---

## Phase 4: Manager Dashboard (Option 1 Enhanced)

### 4.1 Manager Home with Charts

**File:** `src/MotoRent.Client/Pages/Manager/Index.razor`
**Route:** `/manager`

**Layout:**
```
┌────────────────────────────────────────────────────────────┐
│  [Today ▼]  [This Week]  [This Month]  [Custom Range]     │
├────────────────────────────────────────────────────────────┤
│ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │
│ │ ฿45,200 │ │   18    │ │   12    │ │   85%   │          │
│ │ Revenue │ │ Active  │ │ Returns │ │ Utiliz. │          │
│ │ +12% ▲  │ │ Rentals │ │  Today  │ │  Fleet  │          │
│ └─────────┘ └─────────┘ └─────────┘ └─────────┘          │
├────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────┐ ┌──────────────────────────┐ │
│ │   Revenue Trend (7 days) │ │   Fleet Status           │ │
│ │   📈                     │ │   🟢 Available: 24       │ │
│ │   ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄    │ │   🟠 Rented: 18          │ │
│ │   M  T  W  T  F  S  S    │ │   🔴 Maintenance: 3      │ │
│ └──────────────────────────┘ └──────────────────────────┘ │
├────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────┐ ┌──────────────────────────┐ │
│ │   Top Performing Bikes   │ │   Recent Activity        │ │
│ │   1. Honda Click - 85%   │ │   • Check-in: John...    │ │
│ │   2. Yamaha Aerox - 78%  │ │   • Return: Maria...     │ │
│ │   3. Honda PCX - 72%     │ │   • Payment: ฿1,400      │ │
│ └──────────────────────────┘ └──────────────────────────┘ │
└────────────────────────────────────────────────────────────┘
```

**MudBlazor Charts:**
- `MudChart` with `ChartType.Line` for revenue trend
- `MudChart` with `ChartType.Donut` for fleet status
- `MudChart` with `ChartType.Bar` for bike utilization

### 4.2 Revenue Analytics

**File:** `src/MotoRent.Client/Pages/Manager/Revenue.razor`
**Route:** `/manager/revenue`

**Features:**
- Revenue by day/week/month
- Revenue by bike type
- Revenue by payment method
- Average rental value
- Deposit vs. rental breakdown
- Export to Excel/PDF

### 4.3 Fleet Analytics

**File:** `src/MotoRent.Client/Pages/Manager/Fleet.razor`
**Route:** `/manager/fleet`

**Features:**
- Utilization rate by bike
- Maintenance schedule
- Damage history
- Mileage tracking
- Revenue per bike
- Depreciation estimates

### 4.4 Renter Analytics

**File:** `src/MotoRent.Client/Pages/Manager/Renters.razor`
**Route:** `/manager/renters`

**Features:**
- Repeat customer rate
- Nationality breakdown
- Average rental duration
- Customer lifetime value
- Booking source tracking

---

## Phase 5: Shared Components

### 5.1 New Reusable Components

| Component | Purpose |
|-----------|---------|
| `StaffActionCard.razor` | Large touch-friendly action button |
| `BikeCard.razor` | Unified bike display card |
| `StatusBadge.razor` | Consistent status display |
| `PriceSummary.razor` | Rental price breakdown |
| `QuickFilters.razor` | Filter chip row |
| `BottomNavBar.razor` | Mobile bottom navigation |
| `MetricCard.razor` | Dashboard KPI card |
| `ActivityFeed.razor` | Recent activity list |
| `DateRangeSelector.razor` | Quick date range picker |

### 5.2 Component Library Structure

```
src/MotoRent.Client/
├── Components/
│   ├── Staff/
│   │   ├── StaffActionCard.razor
│   │   ├── DueTodayList.razor
│   │   └── QuickRentalCard.razor
│   ├── Tourist/
│   │   ├── BikeCard.razor
│   │   ├── CompareBar.razor
│   │   └── RentalQRCode.razor
│   ├── Manager/
│   │   ├── MetricCard.razor
│   │   ├── RevenueChart.razor
│   │   └── FleetDonut.razor
│   └── Shared/
│       ├── StatusBadge.razor
│       ├── PriceSummary.razor
│       └── QuickFilters.razor
```

---

## Phase 6: Navigation & Routing

### 6.1 URL Structure

| Role | Base Path | Pages |
|------|-----------|-------|
| Staff | `/staff` | `/staff`, `/staff/checkin`, `/staff/return`, `/staff/rentals`, `/staff/payments` |
| Tourist | `/browse`, `/my-rentals` | `/browse`, `/browse/{shop}`, `/my-rentals`, `/compare` |
| Manager | `/manager` | `/manager`, `/manager/revenue`, `/manager/fleet`, `/manager/renters`, `/manager/settings` |

### 6.2 Role-Based Redirects

**Logic in `Routes.razor` or `App.razor`:**
```csharp
// After login, redirect based on role
if (user.IsInRole("Staff"))
    NavigationManager.NavigateTo("/staff");
else if (user.IsInRole("Manager") || user.IsInRole("OrgAdmin"))
    NavigationManager.NavigateTo("/manager");
else
    NavigationManager.NavigateTo("/browse");
```

### 6.3 Layout Assignment

```razor
@* In Routes.razor or use RouteView with custom logic *@
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        @{
            var layout = routeData.PageType.FullName switch
            {
                string s when s.Contains(".Staff.") => typeof(StaffLayout),
                string s when s.Contains(".Tourist.") => typeof(TouristLayout),
                string s when s.Contains(".Manager.") => typeof(ManagerLayout),
                _ => typeof(MainLayout)
            };
        }
        <RouteView RouteData="@routeData" DefaultLayout="@layout" />
    </Found>
</Router>
```

---

## Phase 7: Mobile Optimization

### 7.1 Responsive Breakpoints

| Breakpoint | Target | Layout Adjustments |
|------------|--------|-------------------|
| xs (0-600px) | Mobile | Single column, bottom nav, full-width cards |
| sm (600-960px) | Tablet Portrait | 2 columns, larger touch targets |
| md (960-1280px) | Tablet Landscape | 3 columns, side-by-side panels |
| lg (1280px+) | Desktop | Full layout with sidebar |

### 7.2 Touch Optimizations

- Minimum touch target: 48x48px
- Spacing between targets: 8px minimum
- Swipe gestures for common actions
- Pull-to-refresh on lists
- Long-press for context menus

### 7.3 PWA Enhancements

- Offline mode for viewing current rentals
- Push notifications for:
  - Return reminders (to tourists)
  - Overdue alerts (to staff)
  - New reservations (to staff)
- Camera access for document scanning
- Geolocation for shop finder

---

## Implementation Order

### Sprint 1: Layout Infrastructure
1. Create `StaffLayout.razor`
2. Create `TouristLayout.razor`
3. Create `ManagerLayout.razor`
4. Update `Routes.razor` with layout routing
5. Create `BottomNavBar.razor` component

### Sprint 2: Staff Kiosk Core
1. Create `Staff/Index.razor` (home with action cards)
2. Create `StaffActionCard.razor` component
3. Create `Staff/ActiveRentals.razor`
4. Create `Staff/Return.razor` (check-out flow)
5. Add staff-specific CSS

### Sprint 3: Staff Kiosk Polish
1. Simplify check-in flow for staff
2. Add swipe actions to rental cards
3. Implement quick payment buttons
4. Add overdue alerts and notifications
5. Mobile testing and touch optimization

### Sprint 4: Tourist Portal Enhancement
1. Enhance `Browse.razor` with larger cards
2. Create `BikeCard.razor` component
3. Add compare functionality
4. Enhance `RentalHistory.razor` with QR codes
5. Add favorites (localStorage)

### Sprint 5: Manager Dashboard
1. Create `Manager/Index.razor` with metrics
2. Implement `MudChart` visualizations
3. Create `Manager/Revenue.razor`
4. Create `Manager/Fleet.razor`
5. Add date range filtering

### Sprint 6: Integration & Polish
1. Wire up real data to all dashboards
2. Implement role-based navigation
3. Add loading states and skeletons
4. Performance optimization
5. Cross-browser testing

---

## Technical Considerations

### State Management
- Use `CascadingValue` for user role/context
- Local component state for UI interactions
- Service-level state for shared data
- Consider `Fluxor` for complex state (optional)

### Performance
- Virtualize long lists (`MudVirtualize`)
- Lazy load images
- Debounce search inputs
- Cache dashboard data (5-minute refresh)

### Accessibility
- ARIA labels on all interactive elements
- Keyboard navigation support
- High contrast mode consideration
- Screen reader testing

### Security
- Role-based route guards
- API authorization checks
- Input validation
- XSS prevention in user content

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Check-in time | < 2 minutes | Time from start to complete |
| Touch accuracy | > 95% | First-tap success rate |
| Page load time | < 2 seconds | Time to interactive |
| Mobile usability | 90+ score | Lighthouse audit |
| Staff adoption | 100% | Usage tracking |

---

## Dependencies

- MudBlazor 6.x+ (current)
- No additional packages required
- Optional: `QRCoder` for QR code generation
- Optional: `ChartJs.Blazor` for advanced charts (MudChart may suffice)

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Staff resistance to new UI | Gradual rollout, training sessions |
| Touch targets too small | Rigorous mobile testing |
| Performance on slow devices | Progressive enhancement |
| Offline connectivity | PWA with cached essentials |
| Multi-language support | Use existing localization system |

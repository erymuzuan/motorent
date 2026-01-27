# Plan: Enhance Public Landing Page with Hero Carousel and Pain Points

## Goal
Improve the PublicLanding.razor page with:
1. **Hero Carousel** - Replace static hero with a carousel matching the original website pattern
2. **Pain Points Section** - Replace "Transform Your Shop in Minutes" with compelling pain-point messaging that resonates with motorbike rental operators

---

## Research Findings: Pain Points for Motorbike Rental Operators

Based on industry research, the key challenges facing operators are:

### 1. Revenue Leakages
- **Staff-related issues**: Cash mishandling, unauthorized discounts, unreported rentals
- **Inconsistent pricing**: No standard pricing policy, negotiation-based pricing varies wildly
- **Manual cash handling**: Paper-based systems prone to errors and theft
- **Deposit disputes**: Subjective damage assessments lead to conflicts

### 2. Fleet Maintenance Challenges
- **Depreciation tracking**: No visibility into vehicle lifecycle costs
- **Reactive maintenance**: Missing scheduled services leads to breakdowns (~18% downtime)
- **Utilization blind spots**: Can't identify underperforming vehicles
- **Repair cost accumulation**: No tracking of when vehicles become "money pits"

### 3. Customer Acquisition & Experience
- **Walk-in only mindset**: Missing 60% of customers who book online
- **No online presence**: Can't reach tourists before they arrive
- **Manual booking process**: Time-consuming for both staff and customers
- **Limited reach**: Only local foot traffic, missing the global tourist market

---

## Files to Modify/Create

### 1. Update PublicLanding.razor
**File:** `src/MotoRent.Client/Pages/PublicLanding.razor`

**Changes:**
- Replace static hero with carousel component
- Replace generic features section with pain-point cards
- Add Showcase Carousel section (YouTube + feature images)
- Maintain existing CTA section

### 2. Create/Update Marketing Images
**Copy from original website to Blazor:**
- `website/images/features/eco-system.png` → `wwwroot/images/marketing/`
- `website/images/features/experience.png` → `wwwroot/images/marketing/`
- `website/images/features/offline.png` → `wwwroot/images/marketing/`
- `website/images/features/rental.png` → `wwwroot/images/marketing/`
- `website/images/features/trourist.png` → `wwwroot/images/marketing/`

### 3. Update Localization Files
**Files:**
- `src/MotoRent.Client/Resources/Pages/PublicLanding.resx`
- `src/MotoRent.Client/Resources/Pages/PublicLanding.th.resx`

---

## UI Design Details

### Hero Section (Static - Same Style)
Keep the existing hero but with improved messaging. The carousel is for the **Showcase Section** below hero.

### Showcase Carousel Section (NEW - After Hero)
Following original website pattern at lines 101-161 of `website/index.html`:

```
┌──────────────────────────────────────────────────────────────────────┐
│                    See Our System in Action                          │
│     Watch how rental shops are transforming their operations         │
├──────────────────────────────────────────────────────────────────────┤
│  [<]                                                            [>]  │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │              [YouTube Video / Feature Images]                  │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                       ● ○ ○ ○ ○ ○                                    │
└──────────────────────────────────────────────────────────────────────┘
```

**Carousel Slides:**
1. YouTube Demo Video (embed)
2. Eco-system overview image
3. User experience image
4. Offline mode capabilities
5. Rental operations
6. Tourist portal

### Pain Points Section (Replacing "Transform Your Shop")

```
┌──────────────────────────────────────────────────────────────────────┐
│              Stop Losing Money to These Common Problems              │
│       Every day your shop faces challenges that drain profits        │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐      │
│  │  💸 REVENUE     │  │  🔧 FLEET       │  │  🌍 CUSTOMERS   │      │
│  │    LEAKAGE      │  │    BLIND SPOTS  │  │    OUT OF REACH │      │
│  │                 │  │                 │  │                 │      │
│  │ Cash goes       │  │ Bikes break     │  │ Tourists book   │      │
│  │ missing. Staff  │  │ down. Repairs   │  │ online. You're  │      │
│  │ give discounts. │  │ pile up. You    │  │ waiting for     │      │
│  │ Deposits vanish │  │ don't know      │  │ walk-ins that   │      │
│  │ into "repairs". │  │ which bikes     │  │ never come.     │      │
│  │                 │  │ cost you money. │  │                 │      │
│  │ ✓ Track every   │  │ ✓ Schedule      │  │ ✓ Online        │      │
│  │   transaction   │  │   maintenance   │  │   booking 24/7  │      │
│  │ ✓ Digital       │  │ ✓ Real-time     │  │ ✓ Reach global  │      │
│  │   payments      │  │   fleet status  │  │   tourists      │      │
│  │ ✓ Photo proof   │  │ ✓ Cost per      │  │ ✓ LINE/WhatsApp │      │
│  │   of damage     │  │   vehicle       │  │   integration   │      │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘      │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Details

### Carousel Component (Blazor Implementation)
```razor
@* Showcase Carousel - following original website pattern *@
<section class="section py-6 bg-light">
    <div class="container-xl">
        <div class="text-center mb-5">
            <h2 class="display-5 fw-bold mb-3">@Localizer["ShowcaseTitle"]</h2>
            <p class="fs-3 text-secondary">@Localizer["ShowcaseSubtitle"]</p>
        </div>

        <div class="carousel-container position-relative mx-auto" style="max-width: 1000px;">
            <button class="carousel-btn prev" @onclick="PrevSlide">
                <i class="ti ti-chevron-left"></i>
            </button>

            <div class="carousel-track-container rounded-4 shadow-lg overflow-hidden">
                @foreach (var (slide, index) in m_slides.Select((s, i) => (s, i)))
                {
                    <div class="carousel-slide @(index == m_currentSlide ? "active" : "")">
                        @if (slide.IsVideo)
                        {
                            <iframe src="@slide.Url" allowfullscreen></iframe>
                        }
                        else
                        {
                            <img src="@slide.Url" alt="@slide.Title" />
                        }
                    </div>
                }
            </div>

            <button class="carousel-btn next" @onclick="NextSlide">
                <i class="ti ti-chevron-right"></i>
            </button>

            <div class="carousel-nav d-flex justify-content-center gap-2 mt-4">
                @for (int i = 0; i < m_slides.Count; i++)
                {
                    var index = i;
                    <button class="carousel-indicator @(index == m_currentSlide ? "active" : "")"
                            @onclick="() => GoToSlide(index)"></button>
                }
            </div>
        </div>
    </div>
</section>
```

### Pain Points Card Structure
```razor
<div class="col-md-4">
    <div class="card h-100 border-0 shadow-sm pain-point-card">
        <div class="card-body p-4">
            <div class="pain-icon mb-3">
                <i class="ti ti-cash-off text-danger fs-1"></i>
            </div>
            <h3 class="text-danger mb-2">@Localizer["RevenueLeakage"]</h3>
            <p class="text-secondary pain-description mb-4">
                @Localizer["RevenueLeakageDesc"]
            </p>
            <ul class="list-unstyled solution-list">
                <li><i class="ti ti-check text-success me-2"></i>@Localizer["TrackTransactions"]</li>
                <li><i class="ti ti-check text-success me-2"></i>@Localizer["DigitalPayments"]</li>
                <li><i class="ti ti-check text-success me-2"></i>@Localizer["PhotoProofDamage"]</li>
            </ul>
        </div>
    </div>
</div>
```

---

## Localization Keys to Add

### English (PublicLanding.resx)
```
ShowcaseTitle = See Our System in Action
ShowcaseSubtitle = Watch how rental shops are transforming their operations

PainPointsTitle = Stop Losing Money to These Common Problems
PainPointsSubtitle = Every day your shop faces challenges that drain profits

RevenueLeakage = Revenue Leakage
RevenueLeakageDesc = Cash goes missing. Staff give unauthorized discounts. Deposits vanish into questionable "repairs". Without digital tracking, you'll never know how much you're really losing.
TrackTransactions = Track every baht, every transaction
DigitalPayments = Digital payments with PromptPay
PhotoProofDamage = Photo evidence of vehicle condition

FleetBlindSpots = Fleet Blind Spots
FleetBlindSpotsDesc = Bikes break down unexpectedly. Repair costs pile up. You don't know which vehicles are costing you money or sitting idle. Maintenance becomes reactive, not proactive.
ScheduleMaintenance = Automated maintenance scheduling
RealtimeFleetStatus = Real-time availability tracking
CostPerVehicle = Cost-per-vehicle analytics

CustomersOutOfReach = Customers Out of Reach
CustomersOutOfReachDesc = 60% of tourists book online before arriving. While you wait for walk-ins, your competitors are capturing bookings from their hotel rooms worldwide.
OnlineBooking247 = Accept bookings 24/7 online
ReachGlobalTourists = Reach tourists before they arrive
LineWhatsappIntegration = LINE & WhatsApp notifications
```

### Thai (PublicLanding.th.resx)
```
ShowcaseTitle = ดูระบบของเราในการใช้งานจริง
ShowcaseSubtitle = ดูว่าร้านเช่ารถกำลังเปลี่ยนแปลงการดำเนินงานอย่างไร

PainPointsTitle = หยุดเสียเงินจากปัญหาเหล่านี้
PainPointsSubtitle = ทุกวันร้านของคุณเผชิญกับความท้าทายที่กินกำไร

RevenueLeakage = รายได้รั่วไหล
RevenueLeakageDesc = เงินสดหายไป พนักงานให้ส่วนลดเอง เงินมัดจำหายไปกับ "ค่าซ่อม" ที่น่าสงสัย หากไม่มีระบบติดตามดิจิทัล คุณจะไม่มีทางรู้ว่าสูญเสียไปเท่าไหร่
TrackTransactions = ติดตามทุกบาท ทุกธุรกรรม
DigitalPayments = ชำระเงินผ่าน PromptPay
PhotoProofDamage = ภาพถ่ายหลักฐานสภาพรถ

FleetBlindSpots = จุดบอดการจัดการรถ
FleetBlindSpotsDesc = รถเสียโดยไม่คาดคิด ค่าซ่อมสะสม ไม่รู้ว่ารถคันไหนกินเงินหรือจอดเฉยๆ การบำรุงรักษากลายเป็นการแก้ไขปัญหา ไม่ใช่การป้องกัน
ScheduleMaintenance = กำหนดการบำรุงรักษาอัตโนมัติ
RealtimeFleetStatus = ติดตามสถานะรถแบบเรียลไทม์
CostPerVehicle = วิเคราะห์ต้นทุนต่อคัน

CustomersOutOfReach = ลูกค้าเข้าไม่ถึง
CustomersOutOfReachDesc = 60% ของนักท่องเที่ยวจองออนไลน์ก่อนมาถึง ขณะที่คุณรอลูกค้าเดินเข้าร้าน คู่แข่งกำลังรับจองจากห้องโรงแรมทั่วโลก
OnlineBooking247 = รับจอง 24 ชั่วโมงออนไลน์
ReachGlobalTourists = เข้าถึงนักท่องเที่ยวก่อนมาถึง
LineWhatsappIntegration = แจ้งเตือนผ่าน LINE และ WhatsApp
```

---

## CSS Additions (Inline or Component Style)

```css
/* Carousel Styles */
.carousel-container { position: relative; }
.carousel-track-container {
    aspect-ratio: 16/9;
    background: var(--tblr-dark);
}
.carousel-slide {
    position: absolute;
    inset: 0;
    opacity: 0;
    transition: opacity 0.5s ease;
}
.carousel-slide.active { opacity: 1; }
.carousel-slide img, .carousel-slide iframe {
    width: 100%;
    height: 100%;
    object-fit: cover;
    border: none;
}
.carousel-btn {
    position: absolute;
    top: 50%;
    transform: translateY(-50%);
    width: 48px;
    height: 48px;
    border-radius: 50%;
    background: rgba(255,255,255,0.9);
    border: none;
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    cursor: pointer;
    z-index: 2;
}
.carousel-btn.prev { left: -24px; }
.carousel-btn.next { right: -24px; }
.carousel-indicator {
    width: 12px;
    height: 12px;
    border-radius: 50%;
    background: #cbd5e1;
    border: none;
    cursor: pointer;
}
.carousel-indicator.active {
    background: var(--tblr-primary);
    width: 24px;
    border-radius: 12px;
}

/* Pain Point Cards */
.pain-point-card {
    transition: transform 0.3s ease, box-shadow 0.3s ease;
}
.pain-point-card:hover {
    transform: translateY(-8px);
    box-shadow: 0 12px 40px rgba(0,0,0,0.12);
}
.pain-icon {
    width: 64px;
    height: 64px;
    border-radius: 16px;
    display: flex;
    align-items: center;
    justify-content: center;
}
.solution-list li {
    padding: 0.5rem 0;
    font-size: 0.9rem;
}
```

---

## Verification

1. **Build**: `dotnet build` - no errors
2. **Copy images**: Ensure feature images are copied to `wwwroot/images/marketing/`
3. **Navigate**: Go to `/` (PublicLanding page)
4. **Carousel**: Verify carousel navigates between slides, autoplay works
5. **Pain Points**: Verify 3 cards display with correct messaging
6. **Mobile**: Test responsive layout on mobile viewport
7. **Localization**: Switch to Thai, verify all translations
8. **YouTube embed**: Verify video loads correctly (may need to test with actual URL)

---

## Image Note

The original website uses these images in the carousel:
- `eco-system.png` - System overview
- `experience.png` - User experience
- `offline.png` - Offline capabilities
- `rental.png` - Rental operations
- `trourist.png` - Tourist portal

These should be copied from `website/images/features/` to `src/MotoRent.Client/wwwroot/images/marketing/` for the Blazor app.

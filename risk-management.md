# Fleet Management Module Specification
## For Thailand Vehicle Rental SaaS

---

## Executive Summary

Fleet Management is the **operational backbone** of any rental business. For Thai operators managing 10-50 vehicles, the daily challenges are:

- **Theft:** Bikes loaded into trucks and disappear
- **Staff abuse:** Unauthorized rentals, personal use, fuel skimming
- **Maintenance neglect:** Breakdowns during rentals = refunds + bad reviews
- **Damage disputes:** "That scratch was already there" arguments
- **Fuel fraud:** Customers return empty, staff pocket fuel money

A proper fleet management system can **reduce losses by 15-25%** and **extend vehicle lifespan by 20-30%**.

---

## 1. GPS Tracking Integration

### 1.1 Thailand GPS Providers (DLT-Compliant)

| Provider | Device Cost | Monthly Fee | API Available | Best For |
|----------|-------------|-------------|---------------|----------|
| **GPS2GO (BioWatch)** | ฿3,000-5,000 | ฿300-500 | ✅ Yes | Small-mid fleets |
| **Heliot** | ฿3,500-6,000 | ฿350-500 | ✅ Yes | DLT compliance |
| **Fifotrack** | ฿2,500-4,000 | ฿250-400 | ✅ Yes | Motorcycles |
| **AIS Smart Vehicle** | ฿4,000-8,000 | ฿400-600 | ✅ Yes | Enterprise |
| **Forth Tracking** | ฿3,000-5,000 | ฿300-450 | ✅ Yes | General fleet |

### 1.2 Device Requirements for Motorbikes

```
MOTORCYCLE GPS DEVICE SPECS
├── Waterproof: IP66 or higher (essential for Thailand)
├── Power input: 8-92V (compatible with 12V bikes)
├── Internal battery: 4+ hours backup (theft detection)
├── Size: Compact (<80mm) for hidden installation
├── Network: 4G LTE (2G/3G being phased out in Thailand)
├── Antennas: Internal GPS/GSM (no external wires to cut)
└── NBTC certified: Required for legal operation in Thailand
```

### 1.3 Core GPS Features

```
┌─────────────────────────────────────────────────────────────┐
│  🗺️ LIVE FLEET MAP                                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  [Map View showing Phuket with vehicle markers]              │
│                                                              │
│  🟢 Active rentals: 38                                       │
│  🔵 Available at shop: 8                                     │
│  🟡 Returning today: 4                                       │
│  🔴 Alert (geofence breach): 1                              │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  🔴 ALERT: Honda Click #23                             │ │
│  │  Left Phuket province boundary                         │ │
│  │  Current location: Phang Nga (45km from shop)          │ │
│  │  Renter: John Smith (+66 98 xxx xxxx)                  │ │
│  │  [Call Renter] [View History] [Send Warning]           │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  [List View] [Map View] [Alerts Only]                       │
└─────────────────────────────────────────────────────────────┘
```

### 1.4 Geofencing Rules

**Pre-configured Zones for Thai Rental Operators:**

```
GEOFENCE TEMPLATES
├── Province boundary (e.g., Phuket island only)
├── Tourist area safe zone (Patong, Kata, Karon)
├── Airport pickup/dropoff zone
├── Shop location (100m radius)
├── Prohibited zones (dangerous roads, restricted areas)
└── Custom polygons (operator-defined)

ALERT TRIGGERS
├── Exit province: 🔴 High priority alert
├── Exit tourist zone: 🟡 Medium alert
├── Enter prohibited zone: 🔴 High + auto-warning SMS
├── Overnight parking outside safe zone: 🟡 Morning alert
└── Approaching border (Malaysia): 🔴 Critical alert
```

### 1.5 Trip History & Analytics

```
┌─────────────────────────────────────────────────────────────┐
│  📊 VEHICLE TRIP HISTORY - Honda Click #23                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Rental Period: Dec 15-18, 2025 (John Smith)                │
│                                                              │
│  DAILY BREAKDOWN                                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Dec 15: 45km | Max speed: 62km/h | 3.2 hrs moving      │ │
│  │ Dec 16: 78km | Max speed: 71km/h | 4.8 hrs moving      │ │
│  │ Dec 17: 23km | Max speed: 55km/h | 1.5 hrs moving      │ │
│  │ Dec 18: 12km | Max speed: 48km/h | 0.8 hrs moving      │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  Total Distance: 158km                                       │
│  Fuel Estimate: 3.2 liters consumed                         │
│                                                              │
│  BEHAVIOR FLAGS                                              │
│  ⚠️ 2 harsh braking events                                  │
│  ⚠️ 1 overspeed event (71km/h in 50km/h zone)              │
│  ✅ No geofence violations                                  │
│                                                              │
│  [View Route Map] [Export Report] [Add to Damage Record]    │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Theft Prevention System

### 2.1 Multi-Layer Security Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    THEFT PREVENTION LAYERS                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  LAYER 1: DETERRENCE                                         │
│  ├── Visible GPS sticker on vehicle                         │
│  ├── Warning in rental contract                             │
│  └── Security deposit hold                                  │
│                                                              │
│  LAYER 2: DETECTION                                          │
│  ├── Motion sensor alerts (bike moved when parked)          │
│  ├── Geofence breach notifications                          │
│  ├── Unusual movement patterns (loaded into truck)          │
│  ├── GPS signal jamming detection                           │
│  └── Power disconnection alert (internal battery backup)    │
│                                                              │
│  LAYER 3: RESPONSE                                           │
│  ├── Automatic warning SMS to renter                        │
│  ├── Push notification to operator                          │
│  ├── Remote engine immobilization (kill switch)             │
│  └── One-click police report generation                     │
│                                                              │
│  LAYER 4: RECOVERY                                           │
│  ├── Real-time tracking during theft                        │
│  ├── Location sharing with police                           │
│  ├── Historical route for evidence                          │
│  └── Insurance claim documentation                          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Remote Kill Switch Integration

**How It Works:**

```
KILL SWITCH ACTIVATION FLOW

1. TRIGGER EVENT DETECTED
   ├── Geofence breach (left province)
   ├── GPS jamming detected
   ├── Manual activation by operator
   └── Rental overdue by X hours

2. CONFIRMATION STEP (prevent accidents)
   ├── System checks: Is vehicle moving?
   ├── If moving: Queue kill for next stop
   ├── If stopped: Immediate activation available
   └── Require operator PIN confirmation

3. KILL SWITCH ACTIVATED
   ├── Fuel pump relay disabled
   ├── Engine cannot restart
   ├── GPS continues tracking
   └── Alarm triggered (optional)

4. NOTIFICATION SENT
   ├── SMS to renter: "Vehicle disabled. Contact shop."
   ├── Push to operator: "Kill switch activated on #23"
   └── Log entry created with timestamp
```

**Safety Protocols:**

```
⚠️ KILL SWITCH SAFETY RULES

NEVER activate while vehicle is:
├── Moving over 5 km/h (danger to rider)
├── On highway or major road
└── In emergency response mode

ALWAYS require:
├── Operator PIN confirmation
├── 30-second delay for stopped vehicles
└── Automatic deactivation if false alarm

LEGAL COMPLIANCE:
├── Clear disclosure in rental contract
├── Renter signature acknowledging feature
└── Use only for theft/contract breach
```

### 2.3 Theft Alert Dashboard

```
┌─────────────────────────────────────────────────────────────┐
│  🚨 THEFT ALERT - IMMEDIATE ACTION REQUIRED                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Vehicle: Honda PCX 150 #12                                  │
│  Alert Type: GPS JAMMING DETECTED + MOVEMENT                │
│  Time: 2:34 AM (Dec 18, 2025)                               │
│                                                              │
│  Last Known Location: Kata Beach parking                     │
│  Current Status: Signal lost (jamming suspected)            │
│                                                              │
│  RENTER INFO                                                 │
│  Name: [No active rental - returned yesterday]              │
│  ⚠️ HIGH THEFT PROBABILITY                                  │
│                                                              │
│  RECOMMENDED ACTIONS                                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ [🔴 ACTIVATE KILL SWITCH]  [📞 CALL POLICE]           │ │
│  │ [📍 SHARE LIVE LOCATION]   [📋 GENERATE REPORT]       │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  Signal recovery will auto-notify. Internal battery: 87%    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Staff Abuse Prevention

### 3.1 Common Staff Abuse Patterns

| Abuse Type | Description | Financial Impact |
|------------|-------------|------------------|
| **Ghost rentals** | Staff rents bike for cash, no record | ฿300-800/day lost |
| **Personal use** | Staff uses bike after hours | Wear + fuel + risk |
| **Fuel skimming** | Charges customer, pockets difference | ฿50-200/rental |
| **Kickbacks** | Steering customers to competitors | Lost revenue |
| **Damage cover-up** | Hides damage, blames next customer | Repair costs |
| **Extended hours** | Tells customer to return late, keeps extra | ฿100-300/day |

### 3.2 Staff Access Control System

```
┌─────────────────────────────────────────────────────────────┐
│  👤 STAFF MANAGEMENT                                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ROLE-BASED PERMISSIONS                                      │
│                                                              │
│  👑 OWNER                                                    │
│  ├── Full access to all features                            │
│  ├── View financial reports                                 │
│  ├── Approve refunds/discounts                              │
│  ├── Manage staff accounts                                  │
│  └── Kill switch activation                                 │
│                                                              │
│  👔 MANAGER                                                  │
│  ├── Create/edit bookings                                   │
│  ├── Process check-in/check-out                             │
│  ├── View daily reports                                     │
│  ├── Approve small discounts (<10%)                         │
│  └── Cannot delete records                                  │
│                                                              │
│  👷 STAFF                                                    │
│  ├── View available vehicles                                │
│  ├── Create new bookings (requires manager approval)        │
│  ├── Process check-in/check-out                             │
│  ├── Cannot modify prices                                   │
│  └── Cannot view financial data                             │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 Comprehensive Audit Trail

```
┌─────────────────────────────────────────────────────────────┐
│  📋 AUDIT LOG - Today (Dec 18, 2025)                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  14:32 | Somchai (Staff) | Created booking #1247            │
│         Vehicle: Honda Click #15 | Customer: John D.        │
│         Price: ฿350/day x 3 days = ฿1,050                   │
│                                                              │
│  14:45 | Somchai (Staff) | Processed check-out #1245        │
│         Vehicle: Yamaha NMax #08 | Returned on time         │
│         Fuel level: Full ✅ | Damage: None ✅               │
│                                                              │
│  15:10 | ⚠️ Somchai (Staff) | ATTEMPTED price modification  │
│         Booking #1247 | Tried to change ฿350 → ฿300         │
│         STATUS: BLOCKED (insufficient permissions)          │
│                                                              │
│  15:22 | Nong (Manager) | Approved discount on #1247        │
│         Original: ฿1,050 → New: ฿945 (10% discount)         │
│         Reason: "Returning customer"                        │
│                                                              │
│  [Filter by Staff] [Filter by Action] [Export Report]       │
└─────────────────────────────────────────────────────────────┘
```

### 3.4 After-Hours Vehicle Movement Monitoring

```
┌─────────────────────────────────────────────────────────────┐
│  🌙 AFTER-HOURS VEHICLE ACTIVITY                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Shop Hours: 8:00 AM - 8:00 PM                              │
│  Monitoring Period: 8:00 PM - 8:00 AM                       │
│                                                              │
│  VEHICLES THAT MOVED AFTER HOURS (Not on rental)            │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ ⚠️ Honda Click #31                                     │ │
│  │ Movement: 9:45 PM - 11:30 PM                           │ │
│  │ Distance: 12.3 km                                      │ │
│  │ Route: Shop → Big C → Patong → Shop                    │ │
│  │ Last logged user: Somchai (Staff)                      │ │
│  │ [View Route] [Flag for Review] [Dismiss]               │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ✅ All other vehicles stationary                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. Maintenance Scheduling with Alerts

### 4.1 Maintenance Rules Engine

```
MAINTENANCE TRIGGERS (Configurable per vehicle type)

SCOOTER (Honda Click, Yamaha Filano, etc.)
├── Oil change: Every 2,000 km OR 2 months
├── Brake check: Every 5,000 km OR 6 months
├── Belt replacement: Every 15,000 km
├── Battery check: Every 6 months
├── Tire inspection: Every 3,000 km
└── Full service: Every 10,000 km OR 12 months

BIG BIKE (Honda CB500, Kawasaki Z400, etc.)
├── Oil change: Every 3,000 km OR 3 months
├── Chain adjustment: Every 1,000 km
├── Brake fluid: Every 2 years
├── Coolant: Every 2 years
└── Full service: Every 12,000 km OR 12 months

CAR (Honda City, Toyota Vios, etc.)
├── Oil change: Every 5,000 km OR 6 months
├── Tire rotation: Every 10,000 km
├── Brake inspection: Every 15,000 km
├── Air filter: Every 20,000 km
└── Full service: Every 20,000 km OR 12 months
```

### 4.2 Maintenance Dashboard

```
┌─────────────────────────────────────────────────────────────┐
│  🔧 MAINTENANCE OVERVIEW                                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  URGENT (Overdue)                               2 vehicles  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 🔴 Honda Click #08 - Oil change overdue (2,450 km)     │ │
│  │ 🔴 Yamaha NMax #15 - Belt replacement due (15,200 km)  │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  DUE THIS WEEK                                  5 vehicles  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 🟡 Honda Click #03 - Oil change in 150 km              │ │
│  │ 🟡 Honda Click #12 - Brake check in 200 km             │ │
│  │ 🟡 Honda PCX #07 - Oil change in 300 km                │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  [Schedule Maintenance] [View History] [Export Report]      │
└─────────────────────────────────────────────────────────────┘
```

### 4.3 Automatic Rental Blocking

When a vehicle is overdue for maintenance, the system automatically blocks it from being rented until service is completed. This prevents breakdowns during rentals and protects customer safety.

---

## 5. Digital Vehicle Inspection Checklist

### 5.1 Check-Out Inspection Flow

```
┌─────────────────────────────────────────────────────────────┐
│  📷 CHECK-OUT INSPECTION - Honda Click #23                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  STEP 1: EXTERIOR PHOTOS (Required)                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  [📷 Front]  [📷 Rear]  [📷 Left]  [📷 Right]         │ │
│  │     ✅          ✅         ✅          ✅              │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  STEP 2: DAMAGE MARKING                                      │
│  [Interactive vehicle diagram - tap to mark existing damage] │
│                                                              │
│  STEP 3: VEHICLE CONDITION                                   │
│  ├── Fuel level: [████████░░] 80%                          │
│  ├── Odometer: [12,456] km                                 │
│  ├── Brakes: [✓ Working]                                   │
│  ├── Lights: [✓ Working]                                   │
│  └── Helmet included: [✓ Yes]                              │
│                                                              │
│  STEP 4: CUSTOMER SIGNATURE                                  │
│  [Digital signature pad]                                     │
│  "I confirm vehicle condition as documented above"          │
│                                                              │
│  [ Complete Check-Out ] (generates timestamped PDF)         │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 Photo Requirements

```
PHOTO STANDARDS

MANDATORY ANGLES (Minimum 4 photos)
├── Front: Full front view, license plate visible
├── Rear: Full rear view, license plate visible
├── Left side: Full profile, wheel to handlebar
└── Right side: Full profile, wheel to handlebar

PHOTO VALIDATION
├── Minimum resolution: 1280x720
├── GPS coordinates embedded (auto from phone)
├── Timestamp embedded (cannot be modified)
├── Blur detection: Reject blurry photos
└── Vehicle recognition: AI verifies correct bike

STORAGE
├── Cloud storage: 90 days minimum
├── Dispute cases: Stored until resolved
└── Export for insurance claims
```

### 5.3 Dispute Resolution with Evidence

The system provides timestamped, GPS-tagged photos from both check-out and check-in, creating an irrefutable evidence chain for damage disputes.

---

## 6. QR Code-Based Vehicle Check-In/Check-Out

### 6.1 QR Code System

Each vehicle has a unique QR code containing:
- Vehicle ID
- Encrypted validation token
- Link to vehicle status page

**Placement:**
- Under seat (hidden, primary)
- On key tag (visible, backup)
- Laminated card in document holder

### 6.2 Staff Workflow

1. Open app → Scan QR Code
2. System auto-loads vehicle details, current booking, maintenance status
3. Choose action: Check-Out, Check-In, Quick Status, Report Issue

### 6.3 Self-Service Check-In (Advanced)

For after-hours returns, customers can:
1. Scan QR code on bike
2. Take required photos (guided by app)
3. Confirm fuel level
4. Park and lock bike
5. Drop key in secure box
6. Receive receipt via LINE

---

## 7. Fuel Management

### 7.1 Fuel Policy Options

| Policy | Best For | How It Works |
|--------|----------|--------------|
| **Full-to-Full** | Cars | Return full tank or pay ฿40/liter + ฿100 fee |
| **Same-to-Same** | Motorbikes | Return at same level (±10% tolerance) |
| **Pre-paid** | Short rentals | Pay for full tank upfront, no refund |

### 7.2 Fuel Tracking

- Photo of fuel gauge at check-out and check-in
- GPS-based distance tracking to estimate consumption
- Automatic charge calculation for shortfall
- Fraud detection for discrepancies

### 7.3 Fuel Fraud Detection

```
FRAUD PATTERNS TO DETECT

1. STAFF FUEL SKIMMING
   → Compare photo evidence vs recorded level

2. SIPHONING
   → Fuel drops without GPS movement

3. CUSTOMER UNDERFILL
   → GPS distance vs fuel return doesn't match
```

---

## 8. Cost Analysis & ROI

### Hardware Costs (Per Vehicle)

| Component | Cost (THB) |
|-----------|-----------|
| GPS device (4G, waterproof) | 2,500-4,000 |
| Installation | 300-500 |
| Monthly subscription | 250-400 |

### ROI for 30-Bike Fleet

| Category | Annual Value |
|----------|-------------|
| Theft prevention (1 bike saved) | ฿40,000 |
| Staff abuse reduction | ฿6,000 |
| Maintenance optimization | ฿72,000 |
| Fuel fraud prevention | ฿12,000 |
| Faster damage resolution | ฿6,000 |
| **Total Savings** | **฿136,000** |

**Year 2+ ROI: 127%**

---

## 9. Implementation Roadmap

### Phase 1 (Month 1-3): Core Fleet Management
- Vehicle database with QR codes
- Basic check-in/check-out with photos
- Maintenance scheduling
- Staff accounts

### Phase 2 (Month 3-5): GPS Integration
- GPS2GO API integration
- Real-time fleet map
- Basic geofencing
- Trip history

### Phase 3 (Month 5-7): Advanced Security
- Kill switch integration
- Theft alert system
- Comprehensive audit trail

### Phase 4 (Month 7-9): Analytics
- Maintenance cost tracking
- Fuel efficiency analytics
- Staff performance metrics

### Phase 5 (Month 9-12): Self-Service
- Customer self-check-in via LINE
- AI-assisted damage detection
- Predictive maintenance

---

## 10. Key Differentiators for Thai Market

| Feature | Why It Matters |
|---------|---------------|
| **Motorbike-first** | 70%+ rentals are scooters |
| **Waterproof GPS** | Monsoon + beach areas |
| **LINE integration** | Primary communication |
| **Thai language forms** | Staff convenience |
| **Province geofencing** | Inter-provincial rules |
| **DLT compliance** | Future-proofing |

This Fleet Management module provides **full visibility and control**, reduces theft and abuse losses, and creates evidence for damage disputes - solving the top pain points for Thai rental operators.

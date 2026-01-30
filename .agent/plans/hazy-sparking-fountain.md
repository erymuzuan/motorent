# Plan: CountryPicker Component with Tabler Flag Icons

## Summary

Create a reusable `CountryPicker` Blazor component that renders a `<select>` with country flag icons (using Tabler's built-in `flag-country-XX` CSS classes) powered by the Tom Select JS library. Replace nationality/country inputs across the project.

---

## Task 1: Add Tom Select JS Library

Copy `tom-select.base.min.js` from `E:\project\work\tabler-demo\dist\libs\tom-select\dist\js\` into our wwwroot.

**Files:**
- Copy to: `src/MotoRent.Server/wwwroot/lib/tom-select/tom-select.base.min.js`
- Edit: `src/MotoRent.Server/Components/App.razor` — add `<script src="lib/tom-select/tom-select.base.min.js"></script>` before tabler.min.js

---

## Task 2: Create CountryPicker Component

**File:** `src/MotoRent.Client/Controls/CountryPicker.razor`

A reusable Blazor component that:
- Renders a `<select>` with `<option>` elements for each country
- Each option has `data-custom-properties` with a `<span class="flag flag-xs flag-country-XX">` for the flag
- Uses JS interop to initialize Tom Select on the element (with `render.item` and `render.option` showing flags)
- Supports two-way binding via `Value` / `ValueChanged` parameters
- Includes top 25 Thailand tourist countries, sorted with most common first
- Has a `Placeholder` parameter for the empty option text
- Country data: list of `(string Code, string Name)` tuples — code is ISO 2-letter (`th`, `my`, `us`, etc.), name is display name

**JS interop file:** `src/MotoRent.Client/wwwroot/js/country-picker.js`
- `initCountryPicker(elementId, dotNetRef)` — creates TomSelect instance with flag rendering
- `destroyCountryPicker(elementId)` — cleanup on dispose
- Calls `dotNetRef.invokeMethodAsync('OnValueChanged', value)` when selection changes

**Parameters:**
```csharp
[Parameter] public string? Value { get; set; }
[Parameter] public EventCallback<string?> ValueChanged { get; set; }
[Parameter] public string? Placeholder { get; set; }
```

**Country list** (top 25 Thailand tourist source countries + Thailand itself):
China, Malaysia, India, Russia, South Korea, Japan, Laos, Singapore, United States, Vietnam, United Kingdom, Germany, Australia, France, Cambodia, Indonesia, Taiwan, Myanmar, Philippines, Sweden, Canada, Netherlands, Switzerland, Denmark, United Arab Emirates, Thailand

---

## Task 3: Replace Nationality/Country Fields

### 3a. CreateBooking.razor
- Replace `<input type="text" list="countries" @bind="m_request.CustomerNationality">` with `<CountryPicker @bind-Value="m_request.CustomerNationality" Placeholder="...">`
- Remove the `<datalist id="countries">` block entirely

### 3b. RenterDialog.razor
- Replace Nationality `<select>` (line 74-80) with `<CountryPicker @bind-Value="Entity.Nationality">`
- Replace DrivingLicenseCountry `<select>` (line 108-114) with `<CountryPicker @bind-Value="Entity.DrivingLicenseCountry">`
- Remove the `m_nationalities` static array (no longer needed)

### 3c. ReservationDialog.razor (Tourist)
- Replace `<input type="text" @bind="m_contactInfo.Nationality">` (line 146) with `<CountryPicker @bind-Value="m_contactInfo.Nationality">`

---

## Files to Create

| File | Description |
|------|-------------|
| `src/MotoRent.Server/wwwroot/lib/tom-select/tom-select.base.min.js` | Tom Select JS library |
| `src/MotoRent.Client/Controls/CountryPicker.razor` | Reusable country picker component |
| `src/MotoRent.Client/wwwroot/js/country-picker.js` | JS interop for Tom Select init |

## Files to Modify

| File | Change |
|------|--------|
| `src/MotoRent.Server/Components/App.razor` | Add Tom Select script + country-picker.js |
| `src/MotoRent.Client/Pages/Bookings/CreateBooking.razor` | Replace nationality input + remove datalist |
| `src/MotoRent.Client/Pages/RenterDialog.razor` | Replace 2 selects, remove m_nationalities array |
| `src/MotoRent.Client/Pages/Tourist/ReservationDialog.razor` | Replace nationality input |

## Verification

1. `dotnet build` — zero errors
2. CreateBooking Step 1 — Nationality field shows country picker with flags, selecting a country updates the form
3. RenterDialog — Both Nationality and License Country fields use the picker with flags
4. ReservationDialog (Tourist) — Nationality shows the picker
5. Verify the selected values persist through form submission (booking creation, renter save)

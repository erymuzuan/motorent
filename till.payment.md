● I found it. Here's the summary:

  File Location

  The Payment Terminal UI is in PaymentTerminalPanel.razor:
  src\MotoRent.Client\Components\Till\PaymentTerminalPanel.razor

  The numeric keypad (THB entry) is in a separate component ThbKeypadPanel.razor (referenced at line 223-225).

  How to Access from Till.razor

  ## The flow to reach this Payment Terminal is:

  1. Till.razor (line 339) → Click the "New Transaction" button
  2. This opens TillTransactionDialog.razor as a fullscreen dialog (line 879)
  3. Step 1: Search for a booking or rental
  4. Step 2: Select one and confirm line items
  5. Step 3: Click "Proceed to Payment" → Shows PaymentTerminalPanel (line 440-448)

  Component Hierarchy
```
  Till.razor
    └── TillTransactionDialog.razor (fullscreen dialog)
          └── Step 3: PaymentTerminalPanel.razor
                └── ThbKeypadPanel.razor (for THB amount entry)
                └── DenominationEntryPanel.razor (for foreign currency)

```

  The button that triggers it in Till.razor is at line 339:
  ```html
  <button type="button" class="mr-btn-primary-action w-100 py-3" @onclick="this.NewTransactionDialog">
      <i class="ti ti-receipt-2 me-2"></i>
      @Localizer["NewTransaction"]
  </button>

  ```
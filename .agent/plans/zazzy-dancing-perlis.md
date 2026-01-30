# Port Inline Content Editing + Context Menu from rodin.contract-canvas to MotoRent Designer

## Goal
Add contenteditable inline editing and a right-click formatting context menu to the MotoRent document template designer, porting the UX from the rodin.contract-canvas React app to Blazor.

## Architecture
- **JS module** (`designer-editor.js`) owns all contenteditable state, selection tracking, context menu positioning, and formatting commands
- **Blazor component** (`DesignerContextMenu.razor`) renders the localized menu HTML; JS handles show/hide/positioning
- **DotNetObjectReference** callback pushes content changes back to Blazor on blur
- `ShouldRender()` override prevents Blazor from destroying contenteditable state during editing

## Implementation Steps

### 1. Create `wwwroot/js/designer-editor.js`
New ES module with:
- `initEditor(blockElement, dotNetRef, blockId)` - attach contenteditable, event listeners
- `destroyEditor(blockId)` - cleanup
- `showContextMenu(menuEl, x, y)` / `hideContextMenu(menuEl)` - position & toggle
- `execFormat(blockId, command, value)` - restore saved selection, run `document.execCommand()`, sync content
- `insertAtCaret(blockId, text)` - insert placeholder text at cursor via `execCommand('insertText')`
- Selection saved as `Range` on `contextmenu` event, restored before any formatting command
- Font size uses `execCommand('insertHTML', ...)` with `<span style="font-size:Npx">` for precise px values
- On `blur` (if relatedTarget not in context menu), invoke `dotNetRef.invokeMethodAsync('OnContentChanged', innerHTML)`

### 2. Create `Components/Templates/DesignerContextMenu.razor`
Context menu component with:
- **Formatting**: Bold, Italic, Underline, Strikethrough
- **Next Line**: Insert `<br>`
- **Font Size submenu**: 10, 12, 14, 16, 18, 20, 24, 28, 32 px
- **Align submenu**: Left, Center, Right
- **Insert Field submenu**: Reuse placeholder groups from shared `PlaceholderDefinitions`
- **Block operations**: Move Up, Move Down, Copy, Delete Element
- Parameters: `OnMoveUp`, `OnMoveDown`, `OnCopy`, `OnDelete`, `OnFormatCommand`, `OnInsertPlaceholder`
- All labels localized

### 3. Modify `Components/Templates/CanvasBlock.razor`
- Add `contenteditable="true"` to TextBlock and HeadingBlock content divs
- Add `@oncontextmenu` + `@oncontextmenu:preventDefault` to suppress browser menu
- Add `data-block-id` attribute for JS identification
- New parameters: `OnContentChanged`, `OnContextMenu`
- Override `ShouldRender()` to return `false` when `m_isEditing` is true
- `[JSInvokable] OnContentChanged(string html)` - updates block Content, clears editing flag
- `[JSInvokable] OnEditingStarted()` / `OnEditingStopped()` - manages `m_isEditing` flag
- Implement `IAsyncDisposable` to call `destroyEditor`

### 4. Add CSS to `wwwroot/css/designer.css`
- `.designer-context-menu` - fixed position, z-index 10000, white bg, box-shadow, border-radius, min-width 200px
- Menu items as block buttons with Tabler hover colors
- `.submenu-trigger` with `.submenu` positioned to the right
- `.divider` separator
- `[contenteditable]:focus` outline style (subtle blue matching `#206bc4`)

### 5. Modify `Pages/Settings/DocumentTemplateDesigner.razor`
- Import `designer-editor.js` module in `OnAfterRenderAsync`
- Place `<DesignerContextMenu>` once at root level
- Wire up block operation callbacks (operate on `DesignerState.SelectedBlock`)
- Pass `m_editorModule` reference to child components via CascadingValue or parameter
- Dispose editor module on page dispose

### 6. Modify `Services/DesignerState.cs`
- Add `string? ActiveBlockId` property for tracking which block is being edited

### 7. Extract shared placeholder data
- Create `Services/PlaceholderDefinitions.cs` with static dictionary from `PlaceholderPicker.razor`
- Update `PlaceholderPicker.razor` to reference shared definitions

### 8. Localization files
Create `.resx` files for `DesignerContextMenu`:
- `Resources/Components/Templates/DesignerContextMenu.resx` (default)
- `Resources/Components/Templates/DesignerContextMenu.th.resx` (Thai)
- `Resources/Components/Templates/DesignerContextMenu.ms.resx` (Malay)

Keys: Bold, Italic, Underline, Strikethrough, NextLine, FontSize, Alignment, Left, Center, Right, InsertField, MoveUp, MoveDown, Copy, DeleteElement

## Key Design Decisions
- **execCommand over Range API**: Simpler, still works everywhere, provides built-in undo/redo. Future migration path exists.
- **Block-level vs inline formatting**: Block properties (IsBold, HorizontalAlignment, FontSize) set the outer div style. Context menu formatting produces inline HTML tags within Content. Both coexist.
- **Right panel remains**: BlockPropertyEditor textarea still works for raw HTML editing as fallback.
- **Single context menu instance**: Placed once in designer page, not per-block, to avoid DOM bloat.

## Files Summary
| File | Action |
|------|--------|
| `wwwroot/js/designer-editor.js` | Create |
| `wwwroot/css/designer.css` | Modify (append) |
| `Components/Templates/CanvasBlock.razor` | Modify |
| `Components/Templates/DesignerContextMenu.razor` | Create |
| `Pages/Settings/DocumentTemplateDesigner.razor` | Modify |
| `Services/DesignerState.cs` | Modify |
| `Services/PlaceholderDefinitions.cs` | Create |
| `Components/Templates/PlaceholderPicker.razor` | Modify |
| `Resources/Components/Templates/DesignerContextMenu.resx` | Create |
| `Resources/Components/Templates/DesignerContextMenu.th.resx` | Create |
| `Resources/Components/Templates/DesignerContextMenu.ms.resx` | Create |

## Verification
1. Right-click a TextBlock on canvas - context menu appears with all formatting options
2. Select text, apply Bold/Italic/Underline - formatting visible immediately
3. Change font size via submenu - text resizes
4. Change alignment via submenu - text aligns
5. Insert Field from submenu - `{{placeholder}}` inserted at cursor
6. Click away - content synced to model, right panel textarea reflects changes
7. Right panel textarea editing still works as fallback
8. Non-text blocks (Divider, Spacer, etc.) show only Move/Copy/Delete in context menu
9. Save template - HTML content with formatting persists and renders in preview

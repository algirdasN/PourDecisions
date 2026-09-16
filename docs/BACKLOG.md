# PourDecisions — Backlog

> Tickets are ordered by priority. Work top-to-bottom. Each ticket is sized for a single focused chat session.

---

## EPIC 1 — Shell & Navigation

### PD-001 · App Shell & Side Navigation ✅
**Priority:** P0 — do this first, everything else plugs into it

**Goal:** Get a working Avalonia window with a persistent side navigation panel and placeholder pages for each section.

**Acceptance criteria:**
- Side nav is always visible with entries: **Cocktails** (browser), **My Bar** (inventory), **Edit Cocktails**, **Settings**
- Clicking a nav item swaps the main content area to the correct view
- Active nav item is visually highlighted
- Navigation state is managed in a root `MainWindowViewModel`; child ViewModels are resolved via DI
- App launches without errors

**Implementation notes:**
- Shell uses a two-row, two-column `Grid` — header row spanning both columns, content row split into nav panel (left) and main content area (right). `SplitView` was considered and rejected — plain `Grid` is simpler and sufficient for a persistent panel.
- Nav items are data-driven via a collection on `MainWindowViewModel`
- `IAsyncLoadable` interface used for ViewModels that need async initialisation on navigation — `MainWindowViewModel` checks for it and fires `_ = loadable.LoadAsync()` after setting `CurrentPage`
- `DesignMainWindowViewModel` subclass guards against null service provider for XAML previewer compatibility

---

## EPIC 2 — Cocktail Browser

### PD-002 · Availability Engine (Application layer) ✅
**Priority:** P0 — browser depends on this

**Goal:** Implement the availability calculation logic as a service in `PourDecisions.Application`.

**Acceptance criteria:**
- `AvailabilityService` accepts a list of cocktails and returns a batch availability result keyed by cocktail ID
- Logic matches the rule in `ARCHITECTURE.md`: untracked types assumed available; optional ingredients excluded
- Core calculator (`AvailabilityCalculator`) is unit-testable with no dependency on EF or Avalonia
- `AvailabilityResult` exposes `MissingRequiredCount` for sort order in the browser

**Implementation notes:**
- `AvailabilityCalculator` is a pure static class — no DI, no infrastructure dependencies
- `IAvailabilityService` / `AvailabilityService` registered as Scoped — matches DbContext lifetime
- Returns `Dictionary<int, AvailabilityResult>` keyed by cocktail ID
- Available type IDs loaded via pure `IQueryable` projection — no `Include` needed

---

### PD-003 · Cocktail Browser + Detail View ✅
**Priority:** P1

**Goal:** Main cocktail browser — the app's default landing screen. Master/detail layout; no separate page navigation between list and detail.

**Acceptance criteria:**
- Master/detail split view: left panel (list), right panel (detail), user-resizable via `GridSplitter`, default ratio 1/3 : 2/3
- Detail panel always shows the selected cocktail; auto-selects first item after every filter rebuild
- Each list card shows: name, favorite indicator, availability badge with missing count ("✔️ available" / "❌ missing N")
- Search by name, filter toggles: **Available only**, **Favorites only** — combinable, all filter in-memory
- Default sort: available first → fewest missing required ingredients → alphabetical
- Two empty states: no cocktails in DB; no results for current filters/search (with "Clear filters" action)
- Detail panel shows: name, ingredients (visually distinguished by availability status via converter), instructions, favorite toggle
- Favorite toggle persisted immediately; list card updates reactively without full reload
- `ImagePath` layout slot reserved in detail panel — image loading not yet implemented

**Implementation notes:**
- PD-004 absorbed into this ticket — no separate detail page or back navigation
- `CocktailsSummaryViewModel` is a pure display model: no infrastructure dependencies, exposes `event Action<int, bool>? FavoriteToggled` raised in `OnIsFavoriteChanged`
- `CocktailsViewModel` owns persistence — subscribes to `FavoriteToggled` on each summary VM after construction, calls `ICocktailService.SetFavoriteAsync` via fire-and-forget (`_ =`)
- `LoadAsync` fires `GetAllWithIngredientsAsync` and `GetCocktailAvailabilityAsync` concurrently via `Task.WhenAll`
- `ICocktailService.GetAllWithIngredientsAsync` uses `.Include(c => c.CocktailIngredients).ThenInclude(ci => ci.Type)`
- Availability status colour coded via `AvailabilityStatusToColorConverter` in `Converters/`
- `CheckBox.Content` used for inline labels — no nested Grid required
- Avalonia supports native binding negation (`!HasResults`) — no inverse property needed on the ViewModel

---

### ~~PD-004 · Cocktail Detail View~~ — absorbed into PD-003
Detail view is the right panel of the master/detail layout implemented in PD-003.

---

### PD-010 · Filter Cocktails by Ingredient Type
**Priority:** P2 — after inventory management is in place (depends on tracked ingredient types existing)

**Goal:** Allow the user to filter the cocktail browser by one or more ingredient types, so they can answer "what can I make with my gin?"

**Acceptance criteria:**
- A multi-select control (e.g. `ListBox` with multiple selection, or a dropdown with checkboxes) shows all tracked `IngredientType` records
- Selecting one or more types filters the cocktail list to show only cocktails that use **at least one** of the selected types as a required ingredient
- Filter is combinable with existing search, Available only, and Favorites only filters
- Selecting no types is equivalent to no filter (all cocktails shown)
- "Clear filters" action resets ingredient type selection alongside other filters
- Ingredient type list is loaded fresh on page activation alongside cocktails (same `LoadAsync` call)

**Notes:**
- Ingredient types should be loaded via a new `ICocktailService.GetTrackedIngredientTypesAsync()` method (or equivalent query service) — do not load them from the summary VMs
- Filter logic lives in `FilterCocktails()` on `CocktailsViewModel` alongside existing filter predicates
- Multi-select in Avalonia uses `ListBox` with `SelectionMode="Multiple"` — binding selected items requires care; `SelectedItems` is not directly bindable in Avalonia, so an `SelectionChanged` event handler or a wrapper approach will be needed
- Consider whether untracked types should appear — they shouldn't, since untracked types are assumed always available and filtering by them would be meaningless

---

## EPIC 3 — Inventory Management

### PD-005 · Inventory List View ✅
**Priority:** P1

**Goal:** "My Bar" screen — shows all tracked ingredient types that have at least one bottle, grouped in a collapsible accordion.

**Acceptance criteria:**
- Lists only tracked `IngredientType` records that have at least one bottle
- Each accordion header shows: type name + bottle count e.g. *"Dry Gin (2)"* — collapsed by default
- Expanding reveals bottles; each bottle shows: name, volume, fill level display
- Clicking fill level cycles `Full → Half → Quarter → Full`, persisted immediately (fire-and-forget)
- Delete bottle: confirmation prompt → hard delete. If last bottle of a type, that type disappears from the list — type remains tracked
- Global "Add bottle" button at top of page
- Per-type "Add bottle" button in each accordion header — pre-fills ingredient type, expands accordion
- Empty state (no bottles anywhere): *"Your bar is empty. Add your first bottle to get started."*
- Page reloads on every activation via `IAsyncLoadable`

---

### PD-006 · Add / Edit Bottle Flow ✅
**Priority:** P1

**Goal:** Inline panel to add a new bottle to the bar.

**Acceptance criteria:**
- Inline panel (not modal) with fields: **Bottle name**, **Ingredient type** (autocomplete — select existing or type new name to create inline), **Volume** (ml), **Fill level** (dropdown)
- If a new type name is entered, an `IngredientType` record is created automatically on save with `IsTracked = true`; existing types are also marked `IsTracked = true` on bottle add
- Validation: name required, type name min 3 characters, volume must be a positive number — submit button disabled until all fields valid; no modal fallback
- After successful add: all fields cleared and form resets to default state; new type inserted in sorted position without full list reload; accordion expanded
- Cancel: form cleared and hidden
- Edit mode: not yet implemented — delete and re-add is the current workflow

---

## EPIC 4 — Custom Cocktail Management

### PD-007 · Add / Edit Cocktail Form ✅
**Priority:** P2

**Goal:** Allow the user to create, edit, and delete cocktail recipes from the Edit Cocktails nav section.

**Decisions made during implementation:**
- `IsOptional` removed from `CocktailIngredient` — optional ingredients expressed in free-text instructions instead. Removed UI complexity without losing expressiveness.
- `AmountValue` stays `int` — fractional amounts handled by extending `AmountUnit` (e.g. `Half`) rather than using floats. Reads more naturally: "1 half of lime" vs "0.5 piece of lime".
- Image upload descoped — `ImagePath` column remains in schema ready for a future session.
- Edit-existing confirmation dialog removed — added unnecessary friction to the normal save flow.
- Undo button removed — dirty-check-on-navigation already protects against accidental loss; explicit undo added ceremony without value.
- `SortOrder` added to `CocktailIngredient` (migration `AddCocktailIngredientSortOrder`). In-memory list order is the source of truth; `SortOrder` assigned as 0-based index at save time — no tracking needed in the ViewModel.
- Edit uses delete-all-and-reinsert for `CocktailIngredient` rows — diffing rejected as unnecessary complexity for a small, whole-valued collection.
- Sentinel `<New Cocktail>` item pattern used for the add flow — eliminates separate `IsAddingNew` state.

### ~~PD-008 · Delete Cocktail~~ — absorbed into PD-007 ✅

### PD-007a · Success Notifications (Toast / InfoBar)
**Priority:** P2 — follow-on from PD-007

**Goal:** Provide transient non-blocking feedback after save, add, and delete operations. Currently these operations complete silently.

**Acceptance criteria:**
- A success banner appears after: cocktail added, cocktail saved, cocktail deleted
- Banner is transient — dismisses automatically after a short delay
- Does not block interaction while visible
- Applied to Edit Cocktails screen first; extend to inventory operations (bottle add/delete) in the same session if straightforward

**Notes:**
- FluentAvalonia's `InfoBar` control is the natural fit — already a dependency
- Banner state (message + visibility) lives on the page ViewModel, not in `IDialogService` — it is not a blocking dialog

---

### PD-009 · Settings Screen
**Priority:** P3

**Goal:** Placeholder settings screen wired into the nav.

**Acceptance criteria:**
- Accessible from side nav
- For now: app version display and a "Manage Ingredient Types" sub-section (rename/delete types not yet covered elsewhere)

---

### PD-009a · Manage Ingredient Types (Settings)
**Priority:** P3 — depends on PD-009 shell

**Goal:** Allow the user to manage ingredient types from the Settings screen.

**Acceptance criteria:**
- List all ingredient types (tracked and untracked)
- Rename a type
- Delete a type (with confirmation; warn if type is referenced by cocktail recipes)
- Toggle `IsTracked` per type

**Notes:**
- Untracked types created via the cocktail recipe editor (PD-007) will accumulate over time; this screen is the escape hatch for managing them
- Deleting a type that is referenced by cocktail ingredients should warn the user — hard delete would silently break those recipes

---

## Post-MVP Parking Lot
*(Not ticketed yet — revisit after v1.0)*

- Shopping list (auto-generate from "one ingredient away" cocktails)
- Cocktail tagging (spirit-forward, sour, tiki, etc.)
- Import / export (JSON or CSV)
- Serving history / logbook
- Recipe scaling
- Cocktail images — `ImagePath` column already in schema, UI placeholder reserved in detail panel; needs file picker, copy-on-select, thumbnail preview in form, wiring in detail panel
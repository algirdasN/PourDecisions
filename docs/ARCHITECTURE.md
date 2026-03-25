# PourDecisions — Architecture Reference

> Living document. Update as decisions evolve.

---

## Stack

| Layer | Technology |
|---|---|
| UI Framework | Avalonia 11 (cross-platform XAML) |
| UI Pattern | MVVM via CommunityToolkit.Mvvm |
| Data Access | EF Core 8 + SQLite |
| DI Container | Microsoft.Extensions.DependencyInjection |

---

## Solution Structure

```
PourDecisions.sln
└── src
    ├── PourDecisions.Core          # Entities, DbContext, Migrations
    ├── PourDecisions.Application   # Business logic, query services, availability engine
    ├── PourDecisions.Shared        # Cross-cutting utilities (ListExtensions, etc.)
    └── PourDecisions.Desktop       # Avalonia app, ViewModels, Views
        ├── Converters/             # IValueConverter implementations
        ├── Models/                 # UI-only data structures (e.g. NavigationItem, IngredientAvailabilityInfo)
        ├── Services/               # DialogService for UI dialogs
        ├── ViewModels/             # One per page + MainWindowViewModel, plus design-time stubs
        └── Views/                  # UserControls, one per page
```

**Dependency direction:** `Desktop` → `Application` → `Core`, and both `Desktop` and `Application` → `Shared`. Core has no dependencies on other projects.

The separation of `Core` and `Application` from `Desktop` is intentional — a future `PourDecisions.Api` project can plug into the same business logic without touching the UI layer. The `Shared` project holds utilities needed by multiple layers without infrastructure coupling.

---

## Data Model

### Enums

```
FillLevel:        Full | Half | Quarter
AmountUnit:       Ml | Dash | Splash | Piece | ToTaste
AvailabilityStatus: Available | Unavailable
```

Enums are stored as **strings** in the database (configured via `HasConversion<string>()` in DbContext) to avoid silent data corruption if enum values are reordered.

### Entities

```
IngredientType
├── Id
├── Name (unique index)
├── IsTracked
└── Bottles[]

Bottle
├── Id
├── Name (brand/product name)
├── Volume (ml, integer)
├── FillLevel (enum)
└── TypeId → IngredientType

Cocktail
├── Id
├── Name
├── IsFavorite
├── Instructions (plain text)
├── ImagePath (nullable)
└── CocktailIngredients[]

CocktailIngredient  (join table)
├── Id
├── CocktailId → Cocktail
├── TypeId → IngredientType   ← links to Type, not specific brand
├── AmountValue
├── AmountUnit (enum)
└── SortOrder (integer, 0-based)
```

### Key Design Decisions

- **Availability is Type-based, not brand-based.** A cocktail ingredient links to `IngredientType` (e.g. "Dry Gin"), not a specific `Bottle` (e.g. "Tanqueray"). This matches how recipes are written and avoids having to add every brand to every recipe.
- **`IngredientType` is a lookup table**, not a code enum. New types can be added from the UI without a code change or migration.
- **Bottle directly references `IngredientType`.** The `Ingredient` intermediate entity was removed (migration `20260312175103_CollapsingIngredientAndBottle`). Previously, `Bottle` → `Ingredient` → `IngredientType` created unnecessary indirection. Bottles now belong directly to types. Brand name (e.g., "Tanqueray") and volume are properties of the `Bottle` entity itself, allowing multiple bottles of the same type with different brands/volumes.
- **Tracking lives on `IngredientType`.** Whether something is tracked is a property of the type (e.g. "Dry Gin"), not the bottle. `IsTracked` defaults to `true` for any type created via the bottle add flow. Types created via the cocktail recipe editor are not automatically tracked — the assumption is perishables and garnishes added to recipes don't need inventory tracking until the user explicitly adds a bottle.
- **Adding a bottle marks its type as tracked.** If an existing untracked type gains a bottle, `IsTracked` is set to `true` by `BottleService.AddBottleAsync`. Deleting all bottles of a type does not revert it to untracked — it stays tracked and will appear as missing in the cocktail browser until a new bottle is added.
- **`FillLevel` is an enum** with three values: `Full`, `Half`, `Quarter`. Removed `ThreeQuarters` and `AlmostEmpty` — they added precision without practical benefit for casual cocktail estimation. Tracking a float fill level adds data-entry overhead with little practical benefit for personal use.
- **Untracked ingredients are assumed available.** Perishables (juice, garnish) are not worth tracking for a personal bar app. The expected usage pattern is: plan cocktails first, buy perishables after.
- **No soft deletes.** Hard deletes are used throughout. This is a single-user personal app with no concurrent writers and no audit/undo requirements. Revisit if multi-user support is ever added.
- **Navigation properties use `= null!`** on EF entities. EF guarantees population via change tracking; database foreign keys enforce integrity. This is idiomatic EF and avoids constructor friction in unit tests.

---

## Availability Rule

A cocktail is considered **available** when, for every `CocktailIngredient`:

- The linked `IngredientType.IsTracked = false` → assumed available, OR
- At least one `Bottle` of that type exists (any `FillLevel`)

**`IsOptional` was removed from `CocktailIngredient`.** Optional ingredients are expressed in free-text instructions instead. This removed UI complexity (a per-row checkbox) without losing expressiveness — recipe language like "add a twist of lemon if available" reads more naturally in instructions than a checkbox.

### Availability Engine

Lives in `PourDecisions.Application/AvailabilityEngine/`.

**`AvailabilityResult`**
- `Status` — `AvailabilityStatus` enum (`Available` / `Unavailable`)
- `MissingIngredients` — `List<CocktailIngredient>` of ingredients not available
- Untracked types are invisible — they never appear in the missing list

**`AvailabilityCalculator`** (pure static class, no DI)
- Accepts a `Cocktail` and a `HashSet<int>` of available `IngredientType` IDs
- Returns `AvailabilityResult`
- No EF or Avalonia dependencies — fully unit testable with plain object initialisation

**`IAvailabilityService` / `AvailabilityService`** (registered as `Scoped`)
- Loads available type IDs via a pure `IQueryable` projection (no `Include` needed — EF translates navigation traversal to SQL `WHERE EXISTS`)
- Loads all cocktails with `CocktailIngredients` eagerly loaded via `Include` (required — entities are materialised before passing to the calculator)
- Delegates to `AvailabilityCalculator` per cocktail
- Returns `Dictionary<int, AvailabilityResult>` keyed by cocktail ID
- Called fresh on every Cocktails page activation — no cross-navigation caching

---

## Application Services

### `ICocktailService` / `CocktailService`

Lives in `PourDecisions.Application/Services/`. Registered as `Scoped`.

- `GetWithIngredientsAsync(int cocktailId)` — loads a single cocktail with `.Include(c => c.CocktailIngredients.OrderBy(ci => ci.SortOrder)).ThenInclude(ci => ci.Type)`. Used when opening the edit form.
- `GetAllWithIngredientsAsync()` — loads all cocktails with `.Include(c => c.CocktailIngredients.OrderBy(ci => ci.SortOrder)).ThenInclude(ci => ci.Type)`. Full graph required — entities are materialised before passing to the Desktop layer.
- `GetAllSummariesAsync()` — thin projection (`CocktailEditSummary` record: `Id`, `Name`, `IsFavorite`). No includes. Used by the Edit Cocktails list — does not load availability data.
- `AddCocktailAsync(string name, IList<CocktailIngredientSummary> ingredientSummaries, string instructions)` — owns entity construction via `BuildCocktailIngredients`. Returns the new cocktail's ID.
- `EditCocktailAsync(int id, string name, IList<CocktailIngredientSummary> ingredientSummaries, string instructions)` — delete-all-and-reinsert for `CocktailIngredient` rows. Does not diff the old list — replaces the `CocktailIngredients` collection.
- `SetFavoriteAsync(int cocktailId, bool isFavorite)` — single entity fetch + `SaveChangesAsync`.
- `DeleteCocktailAsync(int cocktailId)` — entity fetch + `Remove` + `SaveChangesAsync`.

**`BuildCocktailIngredients` (private)** — batches ingredient type lookup into a single query, handles inline type creation for new names, deduplicates types appearing multiple times in one recipe via a local `typeCache` dictionary. New types created here have `IsTracked = false` — types created via the recipe editor are not automatically tracked. Assigns `SortOrder` as the 0-based index of each ingredient in the list.

**Edit ingredient strategy — delete-all-and-reinsert.** When editing a cocktail, all existing `CocktailIngredient` rows are cleared and reinserted from the form state. Diffing (detecting adds, removes, reorders) was rejected — the child collection is only meaningful as a whole, no other table references individual `CocktailIngredient` rows, and lists are small. The in-memory list order in the form VM is the source of truth for `SortOrder`; no order tracking is needed in the ViewModel.

### `IBottleService` / `BottleService`

Lives in `PourDecisions.Application/Services/`. Registered as `Scoped`.

- `AddBottleAsync(string typeName, string bottleName, int volume, FillLevel fillLevel)` — owns all entity construction. Looks up `IngredientType` by name (case-insensitive); creates it with `IsTracked = true` if not found; sets `IsTracked = true` on existing types. Normalizes type name to title case. Returns the created `Bottle` with `Type` populated.
- `GetBottlesWithTypeAsync()` — loads all bottles with `.Include(b => b.Type)`.
- `GetBottlesOfTypeAsync(int typeId)` — loads bottles filtered by type, used for surgical list refresh after add/delete.
- `UpdateBottleFillLevelAsync(int bottleId, FillLevel newFill)` — single entity fetch + save.
- `DeleteBottleAsync(int bottleId)` — entity fetch + `Remove` + `SaveChangesAsync`.

### `IIngredientService` / `IngredientService`

Lives in `PourDecisions.Application/Services/`. Registered as `Scoped`.

- `GetIngredientTypeNamesAsync()` — returns all ingredient type names ordered alphabetically. Used for autocomplete in the cocktail ingredient row form.
- `GetTrackedIngredientTypeNamesAsync()` — loads all tracked ingredient type names, ordered alphabetically. Used for autocomplete in the add bottle form.

### Shared Utilities

**`ListExtensions`** lives in `PourDecisions.Shared/Extensions/` and provides:

- `InsertIntoSorted<T>(this IList<T> list, T item, Comparer<T>? comparer = null)` — binary search insert maintaining sorted order. Used in `InventoryViewModel` and `EditCocktailsViewModel` to maintain sorted lists after additions.

**Do not use `DbContext` directly from ViewModels.** All data access goes through Application layer services. The `Desktop` layer depends on `Application`; it does not reference `Core` directly for data access.

---

## UI & Navigation

### Services

**`IDialogService` / `DialogService`** lives in `PourDecisions.Desktop/Services/`.

- `ShowConfirmationDialogAsync(string title, string message, string primaryButtonText, string cancelButtonText)` — two-button dialog; returns `ContentDialogResult.Primary` or `ContentDialogResult.Secondary`.
- `ShowChoiceDialogAsync(string title, string message, string primaryButtonText, string secondaryButtonText, string cancelButtonText)` — three-button dialog; returns `Primary`, `Secondary`, or `None` (cancel).
- `ShowInformationDialogAsync(string title, string message)` — single-button informational dialog. Used to surface async operation failures caught in `try/catch` blocks in ViewModels.

Registered as `Singleton` — stateless UI service, safe as singleton.

### Shell Layout

The app uses a **persistent side navigation panel** (always visible). The shell is a two-row, two-column `Grid` — a header row spanning both columns (app title), and a content row split into nav panel (left) and main content area (right).

**Decided against `SplitView`** in favour of a plain `Grid` — simpler, more predictable, and sufficient for a desktop-only app with no need for a collapsible panel.

### Top-level Navigation Sections

| Section | Description |
|---|---|
| Cocktails | Cocktail browser — the home/default view |
| My Bar | Bottle inventory management |
| Edit Cocktails | Cocktail creation and editing |
| Settings | App info, ingredient type management |

Nav items are data-driven (a collection on `MainWindowViewModel`) so new entries can be added without XAML changes.

### ViewModel-to-View Dispatch

`ContentControl` in the right column binds to `CurrentPage : object` on `MainWindowViewModel`. Avalonia resolves the correct `UserControl` via `Application.DataTemplates` in `App.axaml` — one `DataTemplate` entry per ViewModel type with a matching `DataType`. **Do not use `Window.Resources` or `ResourceDictionary` for this** — Avalonia requires `Application.DataTemplates` for implicit type-based dispatch.

### Async ViewModel Initialisation

ViewModels that require async loading on navigation implement `IAsyncLoadable` (defined in `PourDecisions.Desktop`):

```csharp
public interface IAsyncLoadable
{
    Task LoadAsync();
}
```

`MainWindowViewModel` checks for this interface after setting `CurrentPage` and fires the load:

```csharp
partial void OnSelectedNavItemChanged(NavigationItem? value)
{
    if (_services is null) return;
    CurrentPage = _services.GetRequiredService(value.ViewModelType);
    if (CurrentPage is IAsyncLoadable loadable)
        _ = loadable.LoadAsync();
}
```

The `_ =` discard is intentional — this is fire-and-forget from a non-async context. Error handling must live inside `LoadAsync()` itself, surfaced via a ViewModel property. **Do not put DB calls or service calls in ViewModel constructors** — constructors cannot be async and blocking them would freeze the UI thread.

### Design-Time ViewModels

`MainWindowViewModel` requires `IServiceProvider` and cannot be instantiated by the XAML previewer directly. A `DesignMainWindowViewModel` subclass passes `null!` for the service provider and guards against it in `OnSelectedNavItemChanged`. All `Design.DataContext` declarations in Views should use the design-time variant.

### Home Screen — Cocktail Browser

The cocktail browser is the default landing screen. It uses a **master/detail layout** — a single `CocktailsView` with a horizontal `Grid` (`1*` | `auto` | `2*`), `GridSplitter` in the middle column. There is no separate detail page or back navigation.

**Layout:**

```
CocktailsView (UserControl)
└── Grid (3 rows)
    ├── Row 0: Title "Cocktails" (TextBlock)
    ├── Row 1: Horizontal Separator
    └── Row 2: Master/Detail Grid (3 columns: 1*, Auto, 2*)
        ├── Column 0 (1*): List Panel
        │   ├── Search box (TextBox bound to SearchText)
        │   ├── Filter toggles (2 CheckBoxes: ShowAvailableOnly, ShowFavoriteOnly)
        │   ├── Cocktail list (ListBox)
        │   │   └── Items: CocktailsSummaryViewModel cards
        │   │       Display: FavoriteLabel | Name | AvailabilityLabel
        │   └── Empty state (TextBlock + HyperlinkButton for ClearFilters)
        ├── Column 1 (Auto): GridSplitter (user-resizable)
        └── Column 2 (2*): Detail Panel
            └── ContentControl (auto-renders SelectedCocktail → CocktailsSummaryView)
```

**List Panel Features:**

- Search by name (TextBox, case-insensitive, real-time filtering)
- Two toggleable filters: "Available only" and "Favorites only" — filters are combinable (AND logic)
- Scrollable `ListBox` of cocktails
- Two empty states:
  - No cocktails in DB: `"No cocktails available. Please add some cocktails in the Edit Cocktails page."`
  - No results after filtering: `"No cocktails match the current filters. Try adjusting the filters or search text."` + "Clear filters" button
- `HasResults` property tracks whether list is non-empty
- `ShowClearButton` only visible when filters are active and result is empty (i.e., user actively filtered into no results)

**List Card Display (ItemTemplate):**

Each card shows three columns:
- Favorite indicator: `FavoriteLabel` (`"⭐️ "` if `IsFavorite`, else empty string)
- Name: Cocktail name (center column, left-aligned)
- Availability badge: `AvailabilityLabel` + color coding (right-aligned)
  - `"✔️ available"` in `ForestGreen` if available
  - `"❌ missing N"` in `OrangeRed` if unavailable (N = missing ingredients count)

**Detail Panel (Right Column):**

Displays the selected cocktail's detail view. Auto-selects the first item after filter rebuild. If all items are filtered out, detail panel remains empty (`Content = null`).

### Cocktail Browser ViewModel Pattern

**`CocktailSummaryViewModel`** (pure display model)

Wraps a single cocktail entity plus its availability result. **No infrastructure dependencies** — never inject services into this ViewModel.

```csharp
public partial class CocktailSummaryViewModel(Cocktail cocktail, AvailabilityResult availabilityResult) : ViewModelBase
{
    private readonly int _id = cocktail.Id;

    [ObservableProperty]
    private bool _isFavorite = cocktail.IsFavorite;

    public string Name { get; } = cocktail.Name;
    public string Instructions { get; } = cocktail.Instructions;

    public List<IngredientAvailabilityInfo> Ingredients { get; } = cocktail.CocktailIngredients
        .Select(ci => new IngredientAvailabilityInfo(IngredientDisplayText(ci),
            availabilityResult.MissingIngredients.Any(missing => missing.TypeId == ci.TypeId)))
        .ToList();

    public AvailabilityStatus AvailabilityStatus { get; } = availabilityResult.Status;

    public string AvailabilityLabel { get; } = availabilityResult.Status switch
    {
        AvailabilityStatus.Available => "✔️ available",
        AvailabilityStatus.Unavailable => $"❌ missing {availabilityResult.MissingIngredients.Count}",
        _ => throw new ArgumentOutOfRangeException()
    };

    public event Action<int, bool>? FavoriteToggled;

    partial void OnIsFavoriteChanged(bool value)
    {
        FavoriteToggled?.Invoke(_id, value);
    }

    private static string IngredientDisplayText(CocktailIngredient ci)
    {
        return $"{ci.AmountValue} {ci.AmountUnit.ToString().ToLowerInvariant()} of {ci.Type.Name.ToLowerInvariant()}";
    }
}
```

Display properties:
- `Name`, `Instructions`, `AvailabilityStatus` — direct from entity/result
- `AvailabilityLabel` — computed from `AvailabilityStatus` with emoji and missing count
- `Ingredients` — `List<IngredientAvailabilityInfo>` mapped from `CocktailIngredients` with missing flag set via `availabilityResult.MissingIngredients` lookup

Event-driven favorite toggle:
- `IsFavorite` is a mutable `[ObservableProperty]`
- When changed, `OnIsFavoriteChanged` is auto-generated by MVVM Toolkit
- Raises `FavoriteToggled` event (not subscribed within the summary VM — parent owns the subscription)

**`CocktailsViewModel`** (owns persistence and list lifecycle)

Implements `IAsyncLoadable` for async initialization on navigation.

```csharp
public partial class CocktailsViewModel(
    IAvailabilityService availabilityService,
    ICocktailService cocktailService,
    IDialogService dialogService)
    : ViewModelBase, IAsyncLoadable
{
    private List<CocktailSummaryViewModel> _allCocktails = [];
    
    [ObservableProperty] private ObservableCollection<CocktailSummaryViewModel> _filteredCocktails;
    [ObservableProperty] private string _searchText;
    [ObservableProperty] private CocktailSummaryViewModel? _selectedCocktail;
    [ObservableProperty] private bool _showAvailableOnly;
    [ObservableProperty] private bool _showFavoriteOnly;
}
```

Key methods and lifecycle:

- **`LoadAsync()`**: Fires `GetAllWithIngredientsAsync` and `GetCocktailAvailabilityAsync` **concurrently** via `Task.WhenAll`. Clears `_allCocktails` first (prevents duplicates on re-navigation). Wraps each cocktail in a `CocktailsSummaryViewModel` and subscribes `OnFavoriteToggled` to each summary VM's `FavoriteToggled` event. Populates `FilteredCocktails` (initially unfiltered). Auto-selects first item.

- **`OnFavoriteToggled(int cocktailId, bool isFavorite)`**: Event handler fired by summary VM when favorite is toggled. Calls `cocktailService.SetFavoriteAsync` via `_ =` (fire-and-forget). Immediately calls `FilterCocktails()` to re-evaluate sort/filter order.

- **`FilterCocktails()`**: Core filtering logic. Called from all property-changed hooks (`OnSearchTextChanged`, `OnShowAvailableOnlyChanged`, `OnShowFavoriteOnlyChanged`) and from the favorite toggle handler. Applies:
  - Search predicate: `vm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)` (case-insensitive)
  - Availability filter: `(!ShowAvailableOnly || vm.AvailabilityStatus == AvailabilityStatus.Available)`
  - Favorite filter: `(!ShowFavoriteOnly || vm.IsFavorite)`
  - Results are combined via AND logic. Re-selects `SelectedCocktail` if it's no longer in filtered results.

- **`ClearFiltersCommand`**: Resets `SearchText = string.Empty` and both filter toggles to `false`.

Pattern notes:
- Services injected into parent ViewModel, never into summary VMs — summary VMs have no infrastructure dependencies
- Subscribe to child events (`FavoriteToggled`) after construction — do not pass event handlers as constructor parameters
- Use `FirstOrDefault()` for selection (never index access)
- Clear `_allCocktails` at start of `LoadAsync` to prevent stale data on re-navigation
- Use `ObservableCollection` to allow UI to observe adds/removes — do not mutate in place

### Cocktail Detail View

**`CocktailsSummaryView`** (UserControl)

Displays detail for the selected cocktail. Automatically resolved by Avalonia's `DataTemplate` dispatch when `SelectedCocktail` changes.

```
CocktailsSummaryView (UserControl)
└── Grid (3 rows)
    ├── Row 0: Cocktail name (TextBlock, centered, bold)
    ├── Row 1: Image + Metadata Grid (2 columns: *, *)
    │   ├── Column 0: Image placeholder (Viewbox with colored Rectangle)
    │   │   └── Reserved for future image binding (ImagePath)
    │   └── Column 1: Metadata Grid (2 rows)
    │       ├── Row 0: Favorite toggle (CheckBox with "Favorite" label)
    │       └── Row 1: Ingredients list (ItemsControl)
    │           └── Items: IngredientAvailabilityInfo
    │               Display: DisplayText with conditional OrangeRed foreground if IsMissing
    └── Row 2: Instructions (TextBlock, plain text)
```

View features:
- Title (cocktail name)
- Image placeholder (Viewbox with Aquamarine rectangle) — ready for future image binding to `ImagePath`
- Favorite toggle (CheckBox) — binding updates `CocktailsSummaryViewModel.IsFavorite`, triggering `FavoriteToggled` event
- Ingredients list (ItemsControl) — shows amount, unit, ingredient name; missing ingredients highlighted in OrangeRed via class selector
- Instructions (TextBlock) — plain text display

### UI Models

**`IngredientAvailabilityInfo`** (record)

```csharp
public record IngredientAvailabilityInfo(string DisplayText, bool IsMissing);
```

Lightweight struct mapping ingredients to display + missing flag. Used in detail view to show which ingredients are unavailable. Mapped from `CocktailIngredients` with `IsMissing` set by checking if the ingredient type appears in `availabilityResult.MissingIngredients`.

Example display: `"60 ml of Dry Gin"` or `"1 piece of Lime"`

### Inventory Screen — My Bar

Shows all tracked `IngredientType` records that have at least one bottle, grouped in a collapsible accordion. Types with no bottles are hidden (but remain tracked — they will appear as missing in the cocktail browser).

**Layout:**

```
InventoryView (UserControl)
└── Grid (2 columns: accordion list | add bottle panel)
    ├── Column 0-1 (span depends on IsAddingBottle): Accordion list (always visible)
    │   ├── Empty state (IsEmpty = true): "Your bar is empty. Click the ✚ button to add your first bottle."
    │   ├── Global "Add bottle" button (➕)
    │   └── ItemsControl → IngredientTypeViewModel items (sorted alphabetically)
    │       └── Expander per type (collapsed by default)
    │           ├── Header: DisplayName ("Dry Gin (3)") + per-type "Add" button (➕)
    │           └── Content: Grid with header row + ItemsControl → BottleViewModel items
    │               └── Per bottle row: Name | Volume (ml) | Fill Level (button) | Delete (hyperlink)
    └── Column 2-3 (only visible when IsAddingBottle = true): AddBottleView panel
        └── AddBottleViewModel (inline form)
```

**List Features:**

- Empty state message when no bottles have been added
- Global "Add bottle" button at top; per-type button in each accordion header
- Each type shows count: e.g., `"Dry Gin (3)"`
- Types sorted alphabetically via binary search insertion (preserves expanded state on add)
- Per-bottle actions: cycle fill level inline or delete with confirmation
- Fill level displayed as Unicode block character string (█ for full levels, ░ for empty)
- Delete action triggers confirmation dialog with bottle and type name

**`IngredientTypeViewModel`**
- `Id`, `Name` — from entity
- `Bottles` — `ObservableCollection<BottleViewModel>`, initially loaded from service, surgically updated on add/delete
- `DisplayName` — `"{Name} ({Bottles.Count})"`, updated via `[NotifyPropertyChangedFor]` when `Bottles` changes
- `IsExpanded` — mutable `[ObservableProperty]`, defaults to `false`; set to `true` programmatically after add
- `NameComparer` — static `Comparer<IngredientTypeViewModel>` for binary search insert in parent
- `LoadBottles(ICollection<Bottle> bottles)` — reloads bottle collection without triggering collection-changed event multiple times
- Bubbles `FillLevelChanged` and `DeleteBottleClicked` events upward from child `BottleViewModel`s

**`BottleViewModel`**
- `Name`, `Volume` — display strings, immutable after construction
- `Fill` — mutable `[ObservableProperty]`, drives `FillDisplay`
- `FillDisplay` — block character string (`█░`) computed from fill level via static lookup; updated via `[NotifyPropertyChangedFor]`
- `CycleFillLevelCommand` — cycles `FillLevel` enum in order, raises `FillLevelChanged` event with new level
- `DeleteBottleCommand` — raises `DeleteBottleClicked` event; parent handles confirmation and persistence

**`InventoryViewModel`**
- Implements `IAsyncLoadable`
- Depends on `IBottleService` and `IDialogService`
- `IngredientTypes` — `ObservableCollection<IngredientTypeViewModel>`; subscribes to `CollectionChanged` via `OnIngredientTypesChanged` partial to keep `IsEmpty` in sync when types are added/removed
- `IsEmpty` — drives empty state visibility
- `AddBottleForm` — `AddBottleViewModel?` or null; when non-null and `IsAddingBottle = true`, inline form panel is visible
- `IsAddingBottle` — toggle controlling right-side panel visibility; also drives `ListColumn` for layout responsiveness
- Parallel loading: `Task.WhenAll(bottleTask, ingredientTypeTask)` to fetch bottles and ingredient type names concurrently
- On add: inserts new type at sorted position via binary search (`NameComparer`) rather than reloading — preserves accordion expanded/collapsed state for other types
- On delete: shows confirmation dialog; removes type or reloads its bottles; removes type from collection if it has no bottles left
- Surgical refresh: `GetBottlesOfTypeAsync(typeId)` is used after add/delete, not `GetBottlesWithTypeAsync()` — reduces load on large inventories

**`AddBottleViewModel`**
- Form fields: `IngredientTypeName`, `BottleName`, `VolumeText`, `FillLevel`
- Validation:
  - `IngredientTypeName` — `[Required]` + `[MinLength(3)]`
  - `BottleName` — `[Required]`
  - `VolumeText` — `[CustomValidation]` ensures positive integer
- `IngredientTypeNames` — `ObservableCollection<string>` for autocomplete; passed from parent, updated as new types are added
- Submit disabled until all fields valid
- Submit parses values and fires `OnAddButtonClicked` event with primitives (`string typeName`, `string bottleName`, `int volume`, `FillLevel fillLevel`)
- Cancel fires `OnCancelButtonClicked` event and clears form

### Bottle Add Flow

Adding a bottle uses an inline right-side panel that appears alongside the inventory list — not a modal dialog. The form contains:
- **Ingredient type** (AutoCompleteBox) — select existing or type a new name to create inline
- **Bottle name** (TextBox) — brand/product name
- **Volume** (TextBox + unit label) — numeric input in milliliters
- **Fill level** (ComboBox) — select from `Full`, `Half`, `Quarter`

Auto-created `IngredientType` records default `IsTracked` to `true`. Existing types are also marked `IsTracked = true` when a bottle is added.

The add panel can be triggered two ways:
- Global "Add bottle" button (type field empty, user fills it in)
- Per-type "Add" button in accordion header (type field pre-filled with that type's name)

After a successful add, all fields are cleared and the form resets to its default state. The fill level resets to Full.

### Edit Cocktails Screen

Allows creating, editing, and deleting cocktail recipes. Uses the same **inline right-panel pattern** as the inventory screen — cocktail list on the left, form on the right.

**Layout:**

```
EditCocktailsView (UserControl)
└── Grid (2 columns: list | form panel)
    ├── Column 0: Cocktail list
    │   ├── Empty state (HasExistingCocktails = false)
    │   └── ListBox → CocktailEditSummary items
    │       └── Per row: favorite indicator + cocktail name
    │           (first item is always "<New Cocktail>" sentinel)
    └── Column 1: CocktailEditItemView (always visible, content driven by SelectedCocktail)
        └── CocktailEditItemViewModel
```

**Sentinel item pattern.** `<New Cocktail>` is prepended to the list as a `CocktailEditSummary` with `Id = null`. Selecting it opens a blank form. This means the add flow is identical to the edit flow — no separate `IsAddingNew` boolean or panel visibility toggle needed. `HasExistingCocktails` is `CocktailSummaries.Count > 1` (accounting for the sentinel).

**Unsaved changes guard.** When the form is dirty and the user selects a different list item, a confirmation dialog is shown ("Discard changes?"). On confirm, the new cocktail is loaded. On cancel, the selection reverts to the previously editing item. Implemented via `_isBusy` flag + `_lastSelectedCocktail` snapshot in `OnSelectedCocktailChanging` / `OnSelectedCocktailChanged`. The `_isBusy` flag is always reset in a `finally` block to prevent the VM from locking up if an exception occurs mid-flow.

**After save**, the form closes (`CocktailEditItemViewModel = null`), the list item is updated in place (no full reload), and the saved item is reselected — which triggers the form to reload from the updated summary. Display name normalisation (title case) is applied in the ViewModel before passing to the service, so the updated summary can be constructed without a DB round-trip.

**`EditCocktailsViewModel`**
- Implements `IAsyncLoadable`
- Depends on `ICocktailService`, `IDialogService`, `IIngredientService`
- `CocktailSummaries` — `ObservableCollection<CocktailEditSummary>` including sentinel; sorted alphabetically; new items inserted via `InsertIntoSorted`
- `HasExistingCocktails` — `Count > 1`; notified on `CollectionChanged`
- `SelectedCocktail` — drives form load via `OnSelectedCocktailChanged`
- `CocktailEditItemViewModel` — nullable; null when no form is visible
- Async error handling: all async operations wrapped in `try/catch`; failures surfaced via `ShowInformationDialogAsync`

**`CocktailEditItemViewModel`** (form orchestrator)
- Constructed with `ObservableCollection<string> ingredientTypeNames` (shared reference from parent — autocomplete stays current as new types are added) and an optional `Cocktail` entity
- `IsDirty` — set to `true` by `OnNameChanged`, `OnInstructionsChanged`, and child VM `OnChanged` events; reset to `false` on successful save
- `SaveCocktailCommand` — `CanExecute = IsDirty`; triggers `TriggerValidation()` on all ingredient rows before firing `SaveCocktailClicked` event
- `DeleteCocktailCommand` — `CanExecute = CanDeleteCocktail` (false for new cocktail); fires `OnDeleteCocktailClicked` event
- Fires typed events upward (`SaveCocktailClicked`, `OnDeleteCocktailClicked`); parent owns all DB calls
- `_isDirty` set via backing field in constructor to prevent dirty-marking during initial property population

**`CocktailIngredientViewModel`** (ingredient row, no infrastructure deps)
- Fields: `AmountText` (string, validated as positive integer), `Unit` (AmountUnit enum), `Name` (string, validated)
- Up/Down navigation via single `NavigateCommand` — passes `moveDown` as a `bool` through `OnNavigationIconClicked` event so parent knows direction; `NavigationIcon` (`︿`/`﹀`) derived from `IsFirst`
- `IsFirst` maintained by parent on every `CollectionChanged` — parent iterates and sets `Ingredients[i].IsFirst = i == 0`
- Events: `OnChanged`, `OnNavigationIconClicked(vm, moveDown)`, `OnDeleteClicked`, `OnValidationChanged`, `OnDuplicateCheckNeeded`
- `TriggerValidation()` — public wrapper exposing `protected ValidateAllProperties()` for parent to call at save time
- `GetIngredientData()` — returns `CocktailIngredientSummary`; throws `InvalidOperationException` if `AmountText` is not a valid integer (should never occur if save guard works correctly)

**Child VM validation aggregation — error map pattern.**
`CocktailEditItemViewModel` maintains a `Dictionary<object, (string? AmountError, string? NameError)> _errorMap` keyed by child VM identity. Each `CocktailIngredientViewModel` fires `OnValidationChanged` when its errors change; the parent updates the map entry and calls `RefreshErrors()` to surface the first error of each type as `AmountError` / `NameError` properties. This presents a single consolidated error surface regardless of how many ingredient rows exist, without the parent needing to re-implement validation logic. The map entry is removed when a row is deleted.

**Duplicate ingredient name validation.** `CocktailIngredientViewModel` fires `OnDuplicateCheckNeeded` on name change and on delete. The parent re-evaluates all rows for duplicates and sets `HasDuplicateName` on each. `HasDuplicateName` triggers `ValidateProperty` on `Name`, which invokes the `ValidateUniqueName` custom validator.

**Do not use `WeakReferenceMessenger` for sibling or parent-child communication within a single form.** The direct event pattern (`OnValidationChanged`, `OnDuplicateCheckNeeded`) is scoped to specific instances and has no cross-instance leakage risk. Messenger was considered and rejected for this use case — if two form instances exist briefly during a swap, both would receive and process broadcast messages, causing stale error state.

### Form ViewModel Pattern

Form ViewModels (e.g. `AddBottleViewModel`) follow a self-contained pattern:

- Inherit from `ViewModelBase` which inherits `ObservableValidator` (not just `ObservableObject`) — required for `[NotifyDataErrorInfo]` and `ValidateAllProperties()`
- Validation attributes (`[Required]`, `[MinLength]`, `[CustomValidation]`) on `[ObservableProperty]` fields decorated with `[NotifyDataErrorInfo]`
- `ValidateAllProperties()` called in `AddBottle()` method before command execution to ensure validation runs before any database operations
- `HasErrors` property drives submit button disabled state
- String fields used for numeric inputs (e.g. `VolumeText`) to avoid raw binding cast errors; parsed in `[CustomValidation]` methods
- Submit command fires a typed event with already-validated, already-parsed values — the parent ViewModel receives clean primitives and never needs to interrogate form field state
- No modal dialog validation fallback — the disabled button is the only guard needed

This pattern keeps form logic self-contained and the parent ViewModel as a pure orchestrator.

**Display normalisation ownership.** When the ViewModel constructs an in-place UI update after a save (without re-querying the DB), it owns the normalisation — e.g. calling `name.ToTitleCase()` before passing to both the service and the summary constructor. The service receives an already-normalised value and saves it as-is. This avoids applying the same transformation twice and keeps the single source of truth in the caller that also owns the display update.

---

## Value Converters

Live in `PourDecisions.Desktop/Converters/`. Registered as resources in `App.axaml` for app-wide availability.

**Use converters over ViewModel color/brush properties.** Returning Avalonia types (e.g. `IBrush`) from a ViewModel is a layer violation — it imports UI framework types into what should be UI-agnostic logic.

**Use converters over XAML styles with conditions** for anything beyond trivial visual toggles. Converters are plain C# classes — debuggable, testable, and refactor-friendly in a way that XAML selector logic is not.

Current converters:
- `AvailabilityStatusToColorConverter` — maps `AvailabilityStatus` enum to `IBrush`. Returns `ForestGreen` for `Available`, `OrangeRed` for `Unavailable`. Used in the cocktail browser list to color-code availability badges and in the detail ingredients list to highlight missing items. Returns `null` for unrecognised values (Avalonia falls back to default brush gracefully).
- `BoolToFavoriteLabelConverter` — maps `bool` to string (`"⭐️ "` for `true`, empty for `false`). Used in the cocktail browser list and edit list.

### Avalonia Binding Notes

- **Binding negation** (`!HasResults`) is supported natively in Avalonia — no inverse property or converter needed.
- **`CheckBox.Content`** accepts inline label text — no nested `Grid` + `TextBlock` required. Same applies to `RadioButton`, `Button`, `ToggleButton` — these controls use `Content`, not `Text`.
- **`[RelayCommand]`** is required for commands that need `CanExecute` support, async execution, or `IsRunning` state. Plain public methods bound to `Command` work for simple synchronous cases but cannot be disabled declaratively.

---

## EF Core Notes

- **Design-time factory** (`CocktailDbContextFactory`) lives in `PourDecisions.Core` so EF tooling can run migrations without booting the Avalonia app.
- **Migrations** are in `PourDecisions.Core/Migrations/`. Name migrations descriptively (e.g. `AddCocktailImagePath`, not `Update3`). Commit all migration files including `*.Designer.cs` — they are not reproducible and are required for EF to calculate future migration deltas.
- **Auto-migration on startup** — `db.Database.Migrate()` is called in `App.axaml.cs` so the schema is always up to date on launch.
- **DB file location** — stored next to the executable (`AppContext.BaseDirectory`). Add `*.db` to `.gitignore`.
- **Always use async EF methods** (`SingleAsync`, `ToListAsync`, `SaveChangesAsync`) inside service methods. `Single`, `ToList` etc. are synchronous and block the thread.
- **SQLite structural migrations** may emit a warning about `PRAGMA foreign_keys = 0` not being transactional. This is a SQLite limitation when renaming or restructuring columns — EF handles it correctly. The warning is informational; if a migration of this kind fails mid-run, check `__EFMigrationsHistory` and the schema manually as EF cannot roll it back automatically.

### Migration Commands

```bash
# Add a new migration
dotnet ef migrations add MigrationName \
  --project src/PourDecisions.Core \
  --startup-project src/PourDecisions.Desktop

# Apply to database
dotnet ef database update \
  --project src/PourDecisions.Core \
  --startup-project src/PourDecisions.Desktop

# Undo last migration (only if not yet applied)
dotnet ef migrations remove \
  --project src/PourDecisions.Core \
  --startup-project src/PourDecisions.Desktop
```

---

## DI Setup

Configured in `App.axaml.cs` → `OnFrameworkInitializationCompleted()`.

- `DbContext` registered with `AddDbContext<>` (scoped lifetime)
- `IAvailabilityService` registered as `Scoped` — matches `DbContext` lifetime; avoids captured-dependency bugs that would occur with `Singleton`
- `ICocktailService` registered as `Scoped` — matches `DbContext` lifetime
- `IBottleService` registered as `Scoped` — matches `DbContext` lifetime
- `IIngredientService` registered as `Scoped` — matches `DbContext` lifetime
- `IDialogService` registered as `Singleton` — stateless UI service; safe as singleton
- ViewModels registered as `Transient` — new instance per navigation, appropriate for UI state holders
- `DisableAvaloniaDataAnnotationValidation()` is called to prevent duplicate validation errors when using CommunityToolkit.Mvvm alongside Avalonia's binding pipeline — CommunityToolkit owns validation entirely via `[NotifyDataErrorInfo]` on `ObservableValidator`

---

## Dev Tooling

### Seed Data

`DevSeeder` lives in `PourDecisions.Application/Data/` and is called from `App.axaml.cs` after `db.Database.Migrate()`:

```csharp
#if DEBUG
DevSeeder.Seed(db);
#endif
```

The seeder is idempotent — checks `db.Cocktails.Any()` before inserting. The `#if DEBUG` guard ensures it never runs in release builds.

### SQLite Inspection

Point a SQLite client (DB Browser for SQLite, DBeaver, or VS Code SQLite Viewer) at the `.db` file next to the executable (`bin/Debug/net8.0/`). Useful for verifying availability logic against raw data independently of the application.

### Other Tooling

- `dotnet-ef` installed as a local tool — restore with `dotnet tool restore`
- Commit `.config/dotnet-tools.json` to version control
- Do not commit `*.db` files

---

## Testing

- Test project uses **xUnit**
- `AvailabilityCalculator` is the primary unit-tested component — pure static class with no infrastructure dependencies
- Entity construction in tests uses object initialiser syntax with `= null!` navigation properties; only properties relevant to the test case need to be set
- A `TestData` factory class in the test project provides minimal valid entity construction helpers to keep test setup concise

---

## Future Considerations

- **Network hosting** — adding a `PourDecisions.Api` project (ASP.NET Core) against the same `Core`/`Application` layers is viable without architectural changes
- **Instructions** — currently plain text; could be enriched with per-step timers or ingredient references later
- **Perishable tracking** — optionally trackable in a future iteration if the use case shifts from "plan then buy" to "see what's in the fridge"
- **Soft deletes** — not implemented; would be needed if multi-user/shared bar support is added in the future
- **Shopping list** — auto-generate from cocktails that are one ingredient away from available
- **Cocktail tagging** — spirit-forward, sour, tiki, low-ABV, etc.
- **Import / export** — JSON or CSV for recipe sharing and backup
- **Serving history / logbook**
- **Recipe scaling** — `AmountValue`/`AmountUnit` already support this
- **Cocktail images** — `ImagePath` is nullable and ready; UI placeholder is reserved in the detail panel layout

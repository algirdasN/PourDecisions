# PourDecisions Architecture

> Living document. Update as decisions evolve.

PourDecisions is a desktop application for managing a cocktail recipe database and tracking personal bottle inventory.

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

**Dependency direction:** `Desktop` → `Application` → `Core`, and both `Desktop` and `Application` → `Shared`. `Core` has no dependencies on other projects. Do not reference `Core` directly from `Desktop` for data access — all data access goes through `Application` services.

The separation of `Core` and `Application` from `Desktop` is intentional — a future `PourDecisions.Api` project can plug into the same business logic without touching the UI layer.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Platform | .NET 10.0 |
| UI Framework | Avalonia UI (v11.3) |
| UI Pattern | MVVM via CommunityToolkit.Mvvm (v8.4) |
| Data Access | Entity Framework Core (v10.0) + SQLite |
| DI Container | Microsoft.Extensions.DependencyInjection (v10.0) |

---

## Domain Model

### Enums

```
FillLevel:          Full | Half | Quarter
AmountUnit:         Ml | Dash | Splash | Piece | ToTaste
AvailabilityStatus: Available | Unavailable
```

Enums are stored as **strings** in the database (configured via `HasConversion<string>()` in `DbContext`) to avoid silent data corruption if enum values are reordered.

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
- **Bottle directly references `IngredientType`.** The `Ingredient` intermediate entity was removed (migration `20260312175103_CollapsingIngredientAndBottle`). `Bottle` now belongs directly to a type. Brand name and volume are properties of `Bottle` itself, allowing multiple bottles of the same type with different brands/volumes.
- **Tracking lives on `IngredientType`.** `IsTracked` defaults to `true` for any type created via the bottle add flow. Types created via the cocktail recipe editor are not automatically tracked — the assumption is perishables and garnishes added to recipes don't need inventory tracking until the user explicitly adds a bottle.
- **Adding a bottle marks its type as tracked.** If an existing untracked type gains a bottle, `IsTracked` is set to `true` by `BottleService.AddBottleAsync`. Deleting all bottles of a type does **not** revert it to untracked — it stays tracked and will appear as missing in the cocktail browser until a new bottle is added.
- **`FillLevel` has three values: `Full`, `Half`, `Quarter`.** `ThreeQuarters` and `AlmostEmpty` were removed — they added precision without practical benefit for casual cocktail estimation.
- **Untracked ingredients are assumed available.** Perishables (juice, garnish) are not worth tracking for a personal bar app. The expected usage pattern is: plan cocktails first, buy perishables after.
- **`IsOptional` was removed from `CocktailIngredient`.** Optional ingredients are expressed in free-text instructions instead. This removed UI complexity without losing expressiveness.
- **No soft deletes.** Hard deletes are used throughout. Revisit if multi-user support is ever added.
- **Navigation properties use `= null!`** on EF entities. EF guarantees population via change tracking; database foreign keys enforce integrity. This is idiomatic EF and avoids constructor friction in unit tests.

---

## Availability Rule

A cocktail is **available** when, for every `CocktailIngredient`:

- The linked `IngredientType.IsTracked = false` → assumed available, OR
- At least one `Bottle` of that type exists (any `FillLevel`)

### Availability Engine

Lives in `PourDecisions.Application/AvailabilityEngine/`.

**`AvailabilityResult`**
- `Status` — `AvailabilityStatus` enum (`Available` / `Unavailable`)
- `MissingIngredients` — `List<CocktailIngredient>` of ingredients not available. Untracked types never appear in the missing list.

**`AvailabilityCalculator`** (pure static class, no DI)
- Accepts a `Cocktail` and a `HashSet<int>` of available `IngredientType` IDs
- Returns `AvailabilityResult`
- No EF or Avalonia dependencies — fully unit testable with plain object initialisation

**`IAvailabilityService` / `AvailabilityService`** (registered as `Scoped`)
- Loads available type IDs via a pure `IQueryable` projection (no `Include` needed)
- Loads all cocktails with `CocktailIngredients` eagerly loaded via `Include` (required — entities are materialised before passing to the calculator)
- Delegates to `AvailabilityCalculator` per cocktail
- Returns `Dictionary<int, AvailabilityResult>` keyed by cocktail ID
- Called fresh on every Cocktails page activation — no cross-navigation caching

---

## Application Services

All services are registered as `Scoped` to match the `DbContext` lifetime. Always use async EF methods (`SingleAsync`, `ToListAsync`, `SaveChangesAsync`) — synchronous variants block the thread.

### `ICocktailService` / `CocktailService`

- `GetWithIngredientsAsync(int cocktailId)` — loads a single cocktail with ingredients ordered by `SortOrder`. Used when opening the edit form.
- `GetAllWithIngredientsAsync()` — loads all cocktails with full ingredient graph. Required before passing to the availability calculator.
- `GetAllSummariesAsync()` — thin `CocktailEditSummary` projection (`Id`, `Name`, `IsFavorite`). No includes. Used by the Edit Cocktails list.
- `AddCocktailAsync(...)` — owns entity construction via `BuildCocktailIngredients`. Returns new cocktail ID.
- `EditCocktailAsync(...)` — **delete-all-and-reinsert** for `CocktailIngredient` rows. Does not diff the old list. The in-memory list order in the form VM is the source of truth for `SortOrder`.
- `SetFavoriteAsync(int cocktailId, bool isFavorite)` — single entity fetch + save.
- `DeleteCocktailAsync(int cocktailId)` — entity fetch + remove + save.

**`BuildCocktailIngredients` (private)** — batches ingredient type lookup into a single query, handles inline type creation for new names, deduplicates types via a local `typeCache` dictionary. New types created here have `IsTracked = false`. Assigns `SortOrder` as the 0-based index of each ingredient in the list.

### `IBottleService` / `BottleService`

- `AddBottleAsync(string typeName, string bottleName, int volume, FillLevel fillLevel)` — looks up `IngredientType` by name (case-insensitive); creates it with `IsTracked = true` if not found; sets `IsTracked = true` on existing types. Normalizes type name to title case. Returns the created `Bottle` with `Type` populated.
- `GetBottlesWithTypeAsync()` — loads all bottles with `.Include(b => b.Type)`.
- `GetBottlesOfTypeAsync(int typeId)` — loads bottles filtered by type; used for surgical list refresh after add/delete.
- `UpdateBottleFillLevelAsync(int bottleId, FillLevel newFill)` — single entity fetch + save.
- `DeleteBottleAsync(int bottleId)` — entity fetch + remove + save.

### `IIngredientService` / `IngredientService`

- `GetIngredientTypeNamesAsync()` — all type names ordered alphabetically. Used for autocomplete in the cocktail ingredient row form.
- `GetTrackedIngredientTypeNamesAsync()` — tracked type names only, ordered alphabetically. Used for autocomplete in the add bottle form.

### Shared Utilities

**`ListExtensions`** in `PourDecisions.Shared/Extensions/`:
- `InsertIntoSorted<T>(this IList<T> list, T item, Comparer<T>? comparer = null)` — binary search insert maintaining sorted order. Used in `InventoryViewModel` and `EditCocktailsViewModel` to maintain sorted lists without full reloads.

---

## Presentation Layer

Built with Avalonia UI using the MVVM pattern (CommunityToolkit.Mvvm). All ViewModels are registered as `Transient` — new instance per navigation.

### Navigation

The app uses a **persistent side navigation panel** (always visible). The shell is a two-row, two-column `Grid` — a header row (app title) spanning both columns, and a content row split into nav panel (left) and main content area (right).

`ContentControl` in the right column binds to `CurrentPage : object` on `MainWindowViewModel`. Avalonia resolves the correct `UserControl` via `Application.DataTemplates` in `App.axaml` — one `DataTemplate` per ViewModel type. **Do not use `Window.Resources` or `ResourceDictionary` for this** — Avalonia requires `Application.DataTemplates` for implicit type-based dispatch.

Nav items are data-driven (a collection on `MainWindowViewModel`) so new sections can be added without XAML changes.

| Section | Description |
|---|---|
| Cocktails | Cocktail browser — default view |
| My Bar | Bottle inventory management |
| Edit Cocktails | Cocktail creation and editing |
| Settings | App info, ingredient type management |

### Async ViewModel Initialisation

ViewModels that require async loading on navigation implement `IAsyncLoadable`:

```csharp
public interface IAsyncLoadable
{
    Task LoadAsync();
}
```

`MainWindowViewModel` checks for this interface after setting `CurrentPage` and fires `LoadAsync` as fire-and-forget (`_ = loadable.LoadAsync()`). Error handling must live inside `LoadAsync()` itself, surfaced via a ViewModel property. **Do not put DB or service calls in ViewModel constructors** — constructors cannot be async and blocking them would freeze the UI thread.

### Design-Time ViewModels

`MainWindowViewModel` requires `IServiceProvider` and cannot be instantiated by the XAML previewer. A `DesignMainWindowViewModel` subclass passes `null!` for the service provider and guards against it. All `Design.DataContext` declarations in Views must use the design-time variant.

### Key ViewModels

**`CocktailsViewModel`** — Implements `IAsyncLoadable`. Fires `GetAllWithIngredientsAsync` and `GetCocktailAvailabilityAsync` **concurrently** via `Task.WhenAll` on load. Maintains `_allCocktails` (full list) and `FilteredCocktails` (observable, bound to UI). Filtering (search, available-only, favorites-only) is applied via AND logic and re-run on any filter property change or favorite toggle. Services are injected here; child `CocktailSummaryViewModel`s have no infrastructure dependencies.

**`CocktailSummaryViewModel`** — Pure display model. No service dependencies. Wraps a `Cocktail` entity and `AvailabilityResult`. Raises `FavoriteToggled` event (not subscribed within itself — parent owns the subscription and calls `SetFavoriteAsync` fire-and-forget).

**`InventoryViewModel`** — Implements `IAsyncLoadable`. Manages an accordion list of `IngredientTypeViewModel`s sorted alphabetically. Uses binary search insertion to add new types without a full reload (preserves accordion expanded/collapsed state). Uses `GetBottlesOfTypeAsync(typeId)` for surgical refresh after add/delete — not `GetBottlesWithTypeAsync()`.

**`EditCocktailsViewModel`** — Implements `IAsyncLoadable`. Uses a **sentinel item pattern**: `<New Cocktail>` (with `Id = null`) is always prepended to the list. Selecting it opens a blank form — the add and edit flows are identical. Guards unsaved changes with a confirmation dialog on list item change; uses `_isBusy` flag (always reset in `finally`) + `_lastSelectedCocktail` snapshot to prevent lock-up on exceptions.

**`CocktailEditItemViewModel`** — Form orchestrator. `IsDirty` is set by name/instruction changes and child VM events; reset on successful save. `_isDirty` is set via backing field in the constructor to prevent dirty-marking during initial population. Fires typed events upward (`SaveCocktailClicked`, `OnDeleteCocktailClicked`); parent owns all DB calls.

**`CocktailIngredientViewModel`** — Ingredient row, no infrastructure dependencies. Fires events upward: `OnChanged`, `OnNavigationIconClicked(vm, moveDown)`, `OnDeleteClicked`, `OnValidationChanged`, `OnDuplicateCheckNeeded`. `IsFirst` is maintained by the parent on every `CollectionChanged` (parent iterates and sets `Ingredients[i].IsFirst = i == 0`).

### Child VM Validation Patterns

**Error map pattern.** `CocktailEditItemViewModel` maintains a `Dictionary<object, (string? AmountError, string? NameError)> _errorMap` keyed by child VM identity. Each `CocktailIngredientViewModel` fires `OnValidationChanged` when its errors change; the parent updates the map and surfaces the first error of each type as `AmountError` / `NameError` properties — a single consolidated error surface regardless of row count.

**Duplicate name validation.** `CocktailIngredientViewModel` fires `OnDuplicateCheckNeeded` on name change and on delete. The parent re-evaluates all rows and sets `HasDuplicateName` on each, triggering the `ValidateUniqueName` custom validator.

**Do not use `WeakReferenceMessenger` for sibling or parent-child communication within a single form.** The direct event pattern is scoped to specific instances and has no cross-instance leakage risk. If two form instances exist briefly during a swap, both would receive and process broadcast messages, causing stale error state.

### Form ViewModel Pattern

Form ViewModels (e.g. `AddBottleViewModel`) follow a self-contained pattern:

- Inherit from `ViewModelBase` which inherits `ObservableValidator` (required for `[NotifyDataErrorInfo]` and `ValidateAllProperties()`)
- Validation attributes on `[ObservableProperty]` fields decorated with `[NotifyDataErrorInfo]`
- `ValidateAllProperties()` called before command execution
- `HasErrors` drives submit button disabled state
- String fields used for numeric inputs (e.g. `VolumeText`) to avoid binding cast errors; parsed in `[CustomValidation]` methods
- Submit command fires a typed event with already-validated, already-parsed values — the parent never needs to interrogate form field state

**Display normalisation ownership.** When the ViewModel constructs an in-place UI update after a save (without re-querying the DB), it owns normalisation — e.g. calling `name.ToTitleCase()` before passing to both the service and the summary constructor. The service saves the already-normalised value as-is.

### `IDialogService` / `DialogService`

Lives in `PourDecisions.Desktop/Services/`. Registered as `Singleton` — stateless UI service.

- `ShowConfirmationDialogAsync(...)` — two-button dialog; returns `Primary` or `Secondary`.
- `ShowChoiceDialogAsync(...)` — three-button dialog; returns `Primary`, `Secondary`, or `None` (cancel).
- `ShowInformationDialogAsync(...)` — single-button informational dialog. Used to surface async failures caught in `try/catch` blocks in ViewModels.

### Value Converters

Live in `PourDecisions.Desktop/Converters/`. Registered in `App.axaml` for app-wide availability.

**Use converters over ViewModel color/brush properties.** Returning Avalonia types (e.g. `IBrush`) from a ViewModel is a layer violation.

Current converters:
- `AvailabilityStatusToColorConverter` — maps `AvailabilityStatus` to `IBrush`. `ForestGreen` for `Available`, `OrangeRed` for `Unavailable`. Returns `null` for unrecognised values (Avalonia falls back gracefully).
- `BoolToFavoriteLabelConverter` — maps `bool` to string (`"⭐️ "` for `true`, empty for `false`).

### Avalonia Binding Notes

- **Binding negation** (`!HasResults`) is supported natively in Avalonia — no inverse property or converter needed.
- **`CheckBox.Content`** accepts inline label text — no nested `Grid` + `TextBlock` required. Same for `RadioButton`, `Button`, `ToggleButton`.
- **`[RelayCommand]`** is required for commands that need `CanExecute` support, async execution, or `IsRunning` state.

---

## EF Core Notes

- **Design-time factory** (`CocktailDbContextFactory`) lives in `PourDecisions.Core` so EF tooling can run migrations without booting the Avalonia app.
- **Migrations** are in `PourDecisions.Core/Migrations/`. Name migrations descriptively (e.g. `AddCocktailImagePath`, not `Update3`). Commit all migration files including `*.Designer.cs` — they are not reproducible and required for EF to calculate future deltas.
- **Auto-migration on startup** — `db.Database.Migrate()` is called in `App.axaml.cs` so the schema is always up to date on launch.
- **DB file location** — stored next to the executable (`AppContext.BaseDirectory`). Add `*.db` to `.gitignore`.
- **Always use async EF methods** (`SingleAsync`, `ToListAsync`, `SaveChangesAsync`). Synchronous variants block the thread.
- **SQLite structural migrations** may emit a warning about `PRAGMA foreign_keys = 0` not being transactional. This is a SQLite limitation for column renames/restructures — EF handles it correctly. If a migration of this kind fails mid-run, check `__EFMigrationsHistory` and the schema manually.

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

- `DbContext` — `AddDbContext<>` (scoped)
- `IAvailabilityService`, `ICocktailService`, `IBottleService`, `IIngredientService` — all `Scoped` to match `DbContext` lifetime; avoids captured-dependency bugs that would occur with `Singleton`
- `IDialogService` — `Singleton` (stateless UI service)
- ViewModels — `Transient` (new instance per navigation)
- `DisableAvaloniaDataAnnotationValidation()` is called to prevent duplicate validation errors — CommunityToolkit.Mvvm owns validation entirely via `[NotifyDataErrorInfo]` on `ObservableValidator`.

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

Point a SQLite client (DB Browser for SQLite, DBeaver, or VS Code SQLite Viewer) at the `.db` file next to the executable (`bin/Debug/net10.0/`).

### Other Tooling

- `dotnet-ef` installed as a local tool — restore with `dotnet tool restore`
- Commit `.config/dotnet-tools.json` to version control
- Do not commit `*.db` files

---

## Testing

- Test project uses **xUnit**
- `AvailabilityCalculator` is the primary unit-tested component — pure static class with no infrastructure dependencies
- Entity construction uses object initialiser syntax with `= null!` navigation properties; only properties relevant to the test case need to be set
- A `TestData` factory class in the test project provides minimal valid entity construction helpers to keep test setup concise

---

## Future Considerations

- **Network hosting** — adding a `PourDecisions.Api` project (ASP.NET Core) against the same `Core`/`Application` layers is viable without architectural changes
- **Cocktail images** — `ImagePath` is nullable and ready; UI placeholder is reserved in the detail panel layout
- **Perishable tracking** — optionally trackable if the use case shifts from "plan then buy" to "see what's in the fridge"
- **Soft deletes** — would be needed if multi-user/shared bar support is added
- **Shopping list** — auto-generate from cocktails that are one ingredient away from available
- **Recipe scaling** — `AmountValue`/`AmountUnit` already support this
- **Cocktail tagging** — spirit-forward, sour, tiki, low-ABV, etc.
- **Import / export** — JSON or CSV for recipe sharing and backup
- **Serving history / logbook**
- **Instructions enrichment** — per-step timers or ingredient references
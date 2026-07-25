## Download
Prebuilt binary available on the [Releases page](https://github.com/shubhamddhage/ifc-colour-plugin/releases) — no build required. Copy the `.dll` and rename `BeamTypeCreator.addin.sample` to `BeamTypeCreator.addin`, update the path inside it, and drop both into your Revit Addins folder.

# IFC Colour

A Revit add-in for managing and applying material colour rules — built to keep IFC export colour-coding and "as-modeled" appearance colours in sync without manually editing material overrides every time.

## Why

On projects that export to IFC, materials often need one colour scheme for the IFC export (to distinguish CIP walls, columns, footings, beams, slabs, etc. by discipline/type) and a different, more realistic colour scheme for in-model appearance. Switching between the two by hand in the Materials dialog is slow and error-prone. This tool stores both rule sets and applies whichever one you pick to every matching material in the model, in a single transaction.

## Features

- Maintains two independently-editable rule sets: **IFC Export Colours** and **Original Colours**
- Grid-based editor (add/edit/delete rows, pick colours with the standard Windows colour picker)
- Import/export rule sets as CSV
- Validation before applying or switching rule sets:
  - Material Name required
  - RGB values must be whole numbers 0–255
  - Duplicate material names within a rule set are rejected
- Matches rule-set entries to model materials by normalized name (case/whitespace/dash-variant insensitive) and reports:
  - How many materials were updated, and to what colour
  - Any rule-set entries with no matching material, plus fuzzy-name suggestions from materials actually in the model
- Rule sets persist between Revit sessions as JSON in `%APPDATA%\IfcColour\rules.json`, seeded with sensible CIP defaults on first run

## Usage

1. Run the **IFC Colour** command from a Revit project.
2. Pick a rule set from the **Rule Set** dropdown (IFC Export Colours / Original Colours).
3. Edit rows directly in the grid, or use **Pick Color** for the selected row.
4. **Import CSV** / **Export CSV** to move rule sets between projects or share with teammates.
5. Click **Apply** — the active rule set's colours are pushed onto every matching material in the model, and a summary dialog reports updates and any unmatched material names.

### CSV format
```
MaterialName,Red,Green,Blue
CIP - Columns,240,240,0
CIP - Beams,255,128,30
```

## Architecture

| File | Responsibility |
|---|---|
| `UpdateIfcColourCommand.cs` | `IExternalCommand` entry point — loads stored rules, shows the editor, applies the selected rule set to model materials in a transaction, reports results |
| `MaterialEditorForm.cs` | WinForms grid UI — editing, validation, CSV import/export, colour picker |
| `RuleSetStorage.cs` | Persists rule sets to `%APPDATA%\IfcColour\rules.json`, normalizes/clamps values on load and save |
| `IfcColourConfig.cs` | Default CIP rule sets used to seed storage on first run |
| `MaterialColorItem.cs` | Simple name + RGB color model used across the UI and storage layers |

## Requirements

- Revit 2023
- .NET Framework 4.8
- Visual Studio 2022 (or later) with the .NET desktop development workload

## Building

1. Clone the repo.
2. Open `IfcColour.csproj` in Visual Studio, or build via CLI:
   ```
   dotnet build -c Debug -p:Platform=x64
   ```
3. Copy `IfcColour.addin.sample` to your Revit add-ins folder (typically `%APPDATA%\Autodesk\Revit\Addins\2023\`), rename it to `IfcColour.addin`, and update the `<Assembly>` path to point at your built `.dll`.

## Recent Updates

**Validation pass** — RGB values are now restricted to whole numbers 0–255, and duplicate material names within the active rule set are rejected. Validation runs before Apply and before switching rule-set mode, so a bad row can't silently get lost when you toggle between rule sets.

## Roadmap / Known Gaps

- No unit test coverage yet
- Matching is name-based only — materials renamed on both "sides" (rule set and model) won't reconcile automatically
- No undo-safe preview before committing the transaction

## License

MIT — see [LICENSE](LICENSE).

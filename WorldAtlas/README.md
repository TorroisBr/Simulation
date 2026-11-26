# World Atlas · ATLAS-A

World Atlas is a standalone, offline Windows desktop shell for exploring the two reviewed `atlas.demo/v1` observations. It reads immutable JSON, presents the recorded identities and boundaries, and compares only compatible files from the same Demo run.

The shipped files are synthetic UI fixtures. Their Demo badge is always visible. Selecting, filtering, panning, zooming, or comparing never mutates Simulation Core. No Core runtime, Unity project, Lab process, network service, Dungeon Viewer, City placement rule, or historical reconstruction is included.

## Quick start

Open `WorldAtlas.Desktop.exe` from the published `win-x64` folder. The app loads the bundled Demo World automatically, starts on Day 1, and selects Day 2 as the comparison target. The prominent comparison panel reports **City city-1 — Wheat: Day 1 = 20, Day 2 = 25, observed delta = +5**. It describes an observed value difference; causal events are not covered.

- Use the left navigation to browse the World, Hexes, Locations, and Cities.
- Select a map Hex or a list entity to update the Inspector.
- Focus the map and use the arrow keys to move between visible Hexes. Scroll to zoom at the pointer, drag with the middle mouse button to pan, and use **Fit map** to reset.
- Use **Advanced: Open file…** only when you need to load another observation artifact. The same versioned reader and provenance checks apply; a rejected file leaves the last valid view visible with a status message.
- Choose **100%, 125%, 150%, or 175%** from the Scale control. Atlas saves this interface preference under the current user's Local AppData settings, separately from observation data.
- The unbound City remains in the City list and has no map placement.

Unplaced Hexes have null coordinates and terrain values in the reviewed contract. They stay in the entity list and Inspector and never receive a fabricated map polygon. The validation-only fixture `fixtures/demo/unplaced-hex-validation.json` exercises this behavior and is excluded from the app's fixture picker defaults.

## Build and validation

The solution pins .NET SDK 10.0.404, targets .NET 10, uses centrally pinned Avalonia packages, and commits NuGet lock files.

```powershell
dotnet restore .\WorldAtlas.sln --locked-mode
dotnet test .\WorldAtlas.sln --no-restore
dotnet run --project .\src\WorldAtlas.Desktop\WorldAtlas.Desktop.csproj
dotnet publish .\src\WorldAtlas.Desktop\WorldAtlas.Desktop.csproj -c Release -r win-x64 --self-contained true --no-restore -o .\artifacts\publish\win-x64
```

Avalonia Headless tests exercise controls, selection, the comparison panel, scale settings, and dialog bounds. A native Windows interaction smoke and user-run acceptance remain distinct checks. The app solution contains only the Observation library, the Avalonia desktop client, and focused tests.

## Observation boundary

ATLAS-A accepts only the frozen `atlas.demo/v1` profile with the documented geography, City roster, and one scoped City market-stock capability. The loader rejects unknown versions, extra properties, duplicate properties and identities, invalid coordinates, out-of-scope stock, dangling bindings, oversized input, and unsupported profiles. A future approved immutable Lab observation can be adapted at this file boundary after its own review; ATLAS-A does not implement the Core reader or Lab producer.

Comparison requires adjacent observation indices, increasing day and completed sequence, matching world/run/producer/build/fingerprint/schema/scope, and the later file's exact predecessor digest. Missing rows remain distinct from zero; unsupported information remains marked as not covered. The comparison panel has no generic event contract.

## Follow-on status

ATLAS-B real artifact integration remains `WAIT_DEPENDENCY`. It requires this ATLAS-A shell and valid reviewed LAB-OBS-A observation files; this Demo-only release does not implement that producer or integration.

The planned Inspector Entity Reference Navigation checkpoint is recorded in [the UX backlog](docs/UX_BACKLOG.md); it is not part of this release.

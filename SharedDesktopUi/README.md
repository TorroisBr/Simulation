# Simulation.Ui.Viewport

This standalone .NET 10 library contains only immutable double-precision viewport mathematics. It has no Avalonia, Unity, Simulation Core, Dungeon Core, or World Atlas dependencies. Application-specific camera policy, gestures, geometry, snapping, and rendering remain with each consumer.

The public API consists of `ViewportPoint`, `ViewportYDirection`, and `ViewportTransform`. The transform provides `TryProject`, `TryUnproject`, and `TryReanchor`. A failed operation returns `false` and leaves its output at the default value. Equal-scale reanchoring still evaluates inverse mapping and pan recalculation.

From this directory, restore and validate with the pinned SDK and committed lock files:

```powershell
dotnet restore .\tests\Simulation.Ui.Viewport.Tests\Simulation.Ui.Viewport.Tests.csproj --locked-mode
dotnet build .\SharedDesktopUi.sln --configuration Release --no-restore -p:UseSharedCompilation=false -m:1
dotnet build .\tests\Simulation.Ui.Viewport.Tests\Simulation.Ui.Viewport.Tests.csproj --configuration Release --runtime win-x64 --no-restore -p:UseSharedCompilation=false -m:1
dotnet test .\tests\Simulation.Ui.Viewport.Tests\Simulation.Ui.Viewport.Tests.csproj --configuration Release --no-build --no-restore
```

The locked graph covers portable `net10.0` and Windows `win-x64` builds. The library is source-reference ready and is not currently published as a NuGet package.

# Third-party software and package inventory

The project uses only the Avalonia desktop UI and its platform/runtime dependencies, plus NUnit-based test tooling. Versions are pinned in `Directory.Packages.props`; full resolved versions are pinned in project `packages.lock.json` files after restore.

Direct package families:

- Avalonia 12.2.0 (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Headless.NUnit`) — MIT.
- NUnit 4.5.1 and NUnit3TestAdapter 6.3.1 — MIT.
- Microsoft.NET.Test.Sdk 18.12.0 — MIT.

The Avalonia UI and NUnit dependencies are used under their upstream package licenses. The application does not bundle Dungeon Studio code or art, Simulation Core binaries, Unity runtime content, third-party map tiles, external icon sets, or network client libraries. Review every resolved transitive package's NuGet nuspec license metadata during dependency updates; see the generated dependency inventory recorded with the validation evidence.

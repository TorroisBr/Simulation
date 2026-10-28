# CORE-F P12-D selected owner staging through the Core application

## Scope

The production restore path now asks `DailyV1Application` to stage the
selected Core City/NPC owner vectors after P12-C roots have been staged. The
application applies the same captured-bootstrap or exact-session replacement
reservation checks used by P12-C before it inspects the D inputs. Core creates
the selected owners through the existing P12-D stager; Unity then constructs
its compatibility projections over those exact instances.

The change does not complete P12-D or CORE-F. It does not complete
`DailyV1Application.TryStart`, P12-E/F owner staging, full detached restore,
the fixed .NET host, or separate-process T/T+1/T+2 continuation. The legacy
spatial owner and Unity projection remain host inputs/outputs at this boundary.

## Candidate identity

- Repository: `TorroisBr/Simulation`
- Branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Core canonical baseline: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate parent: `5dab433665650842dd653c5e52370ed89fc6ac42`
- Validated and pushed code commit: `8d38d4f08621a92e069c726221ccb507926c822c`
- Candidate tree: `e2cd0bd3bb4acd9bb0f97479ae317c0581cc05d7`

The official runners validated the four changed files in the working tree
before commit. Their recorded file hashes were checked against the committed
files after the commit; the bytes are unchanged.

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `c2a315990284495a7091610ad738e0659ba5a53c2709698123421916ab391ac3` |
| `Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs` | `7010878fb5be29991693d70775645800395e5f7c88d311b05553392883db3f40` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `16dc285a19075144989aaafd76f4d3e6e2cad720a3ce2ea7a9dba39cc97d817d` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `4b65e36b73b348e4e7dc99edb01375a077548d2a564239367e8ba51271e846d2` |

## Validation

- Official CoreValidation run `dce34cbdb3ec45f1ba595acce1286e9b`: cold and warm **894/894 PASS**; input inventory stable. Input digest: `d361eb05436cb0d4f5af8d8219b7bd0ad2b261541e1e8af2938421b945a04076`. Private runner manifest SHA-256: `a65314d4fea4cb9ba021b197e1fcabdc678968062fdf0c1377dcaa129e80cd79`. Original cold/warm TRX SHA-256: `67cbe9ec0837265008dcb137733861a9acf319d3138cf6d1b9e6d84b5ca44992` and `e611c91e1fd75fa5f4bf4a887bd13f883d4ad5827fcdd7922ff33d942b0b90aa`. Originals and the run manifest remain in owner-only local evidence retention; no public TRX projection is claimed.
- Focused Unity `SimulationRuntimeAdmissionTests`, run `bcb747aae6a243dbb3e26465b4a0fe62`: **227/227 PASS**.
- Unity ALL EditMode, run `7cc65456e65d4eef8b64c013acc73f6f`: **3274/3274 PASS**, zero failed, skipped, or inconclusive tests.
- Official Unity Smoke filter, run `5a57bbd800cf4745bd1aed3f0603ec82`: **5/5 PASS**.
- All three Unity runs report stable before/after inputs with the same 1,956-file inventory SHA-256: `db2391a9b680186c249b7da43657891f87e9306fcc4e65bc7ad03f9a3885718d`. The 227 focused identities and five Smoke identities were independently compared with the full EditMode XML and are exact subsets.
- The sanitized full EditMode package is [`coref-p12d-owner-staging-editmode.zip`](Evidence/coref-p12d-owner-staging-editmode.zip), SHA-256 `0367c86ab6e428ee839a04591ad1ace09a8de61bebe55900c6723d8dc7384867` (172,972 bytes). Its manifest is [`coref-p12d-owner-staging-editmode.manifest.json`](Evidence/coref-p12d-owner-staging-editmode.manifest.json), SHA-256 `769fb47a837d7f9b24b8edb6d09117b45b7b7326835dcb085dba3ae8adba81f4`. The official package verifier passed against the original private run manifest. A path/credential-pattern scan of the public manifest found no matches.
- `git diff --check`: PASS for the implementation candidate.

The CoreValidation run observed a Unity `Library` in the worktree. It does
not claim Library-independent .NET execution. No LongRun gate was added because
this restore-staging boundary does not change daily-loop behavior.

## Review and promotion status

This is a validated candidate checkpoint, not canonical promotion or CORE-F
completion. The candidate code is published on its existing branch. The exact
current integration boundary still requires independent exact-tip review before
any promotion decision. Existing `ProjectSettings` edits, untracked files, and
other protected evidence were not staged or modified.

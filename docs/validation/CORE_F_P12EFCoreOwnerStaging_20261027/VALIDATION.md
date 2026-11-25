# CORE-F P12-E/F Core owner staging through the application

## Result and scope

The detached persisted-image restore path now routes the already Core-owned
P12-E and P12-F owner construction through `DailyV1Application`. The
application checks the same unused-bootstrap-thread or exact-session
replacement reservation used for P12-C/D before inspecting stage inputs.
Core creates the selected institution/property/political/justice owners for E
and political knowledge, scheduled directives, actor choices, travel parties,
and expeditions for F. Unity continues to stage and project the owners that
remain host-owned in the accepted profile.

The low-level coordinator test path that intentionally has no application
continues to call the same Core owner stagers. It creates no application
session and does not publish a candidate. The integrated restore test now
exercises the application-authorized P12-C through P12-F staging path.

This candidate does not complete `DailyV1Application.TryStart`, Core-owned
Unity runtime forwarding, complete detached restore/publication, the fixed
.NET host, or separate-process T/T+1/T+2 continuation. P12-E military,
conflict, war, battle, and remaining Unity compatibility projections remain
outside the moved Core staging boundary. CORE-F remains incomplete and is not
promoted to Core canonical.

## Candidate identity

- Repository: `TorroisBr/Simulation`
- Branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate parent: `398bbd50fd43921082ea5f3eaca8d64b29ca5814`
- Candidate implementation commit: `383af5ab8a7261ad6ba9178ef74928936909b8e1`
- Implementation tree at the code commit: `814bcf1b51637d0a2e9c6bb861ce1a276103bd19`
- Candidate is published on the branch above; exact-tip independent review is
  still required before any promotion consideration.

The official runners used the pre-commit worktree snapshot at `398bbd50`.
`inputStable` was true in every run, and the six changed files' current SHA-256
values match the exact files consumed by the applicable runner. The Core test
file is bound by the CoreValidation input manifest; the five Unity inputs are
bound by the Unity inventory.

| Changed file | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `2e71d6d82877f854f449c16c2734f6815fd5eddbcfbb5a0c81063a77577a45bc` |
| `Assets/_Project/Scripts/P12EDailyV1OwnerPackage.cs` | `ab6b6cc26f0b844841f349a465a0e72ec73d4bdcfac32e610d9856e1bf2bfb7e` |
| `Assets/_Project/Scripts/P12FDailyV1OwnerPackage.cs` | `26def753d60c7977912796a2be497cc3afc8468aa75683f4e1cbefa3a1844b6e` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `eb767733bef620ab86c277b239c10cad05311d290058d3ca6662ead0e81261fa` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `d751469a092df24f03fdd8fcee3cda3f5bfd29bf2c3d89eaa8e11d338a103f12` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | `ce154b32b5fcb93d492a00f3b6cd025ed1cdf28b0513f5748c664663d232bed9` |

## Validation

- Official CoreValidation run `b46749ada78d4f288a7988ade151fa9c`: cold and
  warm **896/896 PASS**. Input snapshot was stable across 504 inputs with
  digest `1187305edc7994436aaa2679d4135e6f7c219b4f0040c9e4f9e51b312cc7f3ce`.
  Private run-manifest SHA-256:
  `9d742feacefd67ffa7f79b7adb4b877ebd8357e3f6ac0c26b54e861a06b6658d`.
  Original cold/warm TRX files remain private and unmodified; no public .NET
  TRX package is claimed. The runner observed a Unity `Library`, so this is
  not a Library-independent .NET claim.
- Focused Unity `P12GPoliticalBindingRestoreTests`, run
  `93d61fcc02bc4bf3837392812f7bd06f`: **15/15 PASS**. Private manifest
  SHA-256: `3b7a6026b89e2f1a24c69b3b913fce960a3330ff1db2fbe294ee2f8f3cbadc53`.
- Unity ALL EditMode, run `2acb6e8542794a63882eae08f55b214d`:
  **3274/3274 PASS**, zero failed, skipped, or inconclusive cases. Private
  manifest SHA-256:
  `9766c45c2554a7f04af7fe295b5c181721814a807270786b8d0fa611b4984113`.
- Official Unity Smoke filter, run `adb05ae66445473eb821599b561e890e`:
  **5/5 PASS**. Private manifest SHA-256:
  `d8ab4f667d5daff8be422fe542a7cd1b9ae3c1e30a2fae87f1799dd121864b7c`.
- All three Unity runs reported stable input snapshots with the same 1,956-file
  inventory digest `34fe4d31af25890b7e582e67b8fbda297d378a5374851279965d799b9fc7d5db`.
  The 15 focused test identities and five Smoke identities were independently
  compared with the full EditMode XML and are exact subsets.
- The sanitized ALL EditMode package is
  [`coref-p12ef-core-owner-staging-editmode.zip`](Evidence/coref-p12ef-core-owner-staging-editmode.zip),
  SHA-256 `fcb14832d3af38e0a5a78e3fedafe26cff23516eb526b216612cc0013322e1ff`
  (172,812 bytes). Its package manifest is
  [`coref-p12ef-core-owner-staging-editmode.manifest.json`](Evidence/coref-p12ef-core-owner-staging-editmode.manifest.json),
  SHA-256 `a0863da5dadffe01caa6729bad9aff0a1c4d4ba5f0cdfe2c5794bb6d3e67f0a4`.
  The official package verifier passed against the private source run
  manifest. A scan of the public manifest for local Windows home paths and
  credential-like patterns found no matches. Original Unity XML/logs remain
  private and unmodified.
- `git diff --check`: PASS for the implementation candidate.

The first focused attempt before retaining the no-application Core-stager path
failed four cases because the existing low-level restore tests invoke the
coordinator without an application. The follow-up preserves that test seam,
adds an integrated application-authorized restore, and passes the focused
suite above. The failed original run remains private as diagnostic evidence.

No LongRun gate was added: this change stages detached owner data and does not
alter daily execution or causal behavior.

## Review and promotion status

This is a pushed, validated implementation checkpoint, not a canonical
promotion or CORE-F completion. Independent exact-tip review of the
implementation plus this evidence record remains outstanding. No unrelated
`ProjectSettings` changes, `.validation` data, or other untracked evidence was
included in the candidate commit.

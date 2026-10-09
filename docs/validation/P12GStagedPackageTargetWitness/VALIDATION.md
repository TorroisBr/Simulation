# P12-G staged-package target-owner witnesses

**Status:** validated, test-only evidence slice. P12-G and P12-A remain
`WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`.

**P12 canonical base:** `0a0213fcc1bed7e2ad233d6681aca9e2eb935bb7`

**Architecture baseline:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Code commit:** `a73fdff877af9f0cbdb1657dff36a45b87eb25e9`

**Reviewed Assets tree:** `f3af2c9ce70ccc562c5f56d337e07a420dbf0ed8`

The existing C/D/E/F private-staging composition test now checks three
target-side census facts before a P12-G assembler exists:

- For every staged D NPC, exactly one local-observation and one merchant
  trade-state receipt provider points to that exact staged NPC and its exact
  installed receipt owner; both witnesses report zero cardinality and revision.
- The staged E Justice receipt sentinel identifies the exact staged
  `JusticeSystem`, with its contractual singleton cardinality and zero local
  revision.
- The staged F ActorChoice temporal witness identifies the staged store's
  `CensusOwnerIdentity`, shares that identity and revision with the staged P11
  witness, and reports zero temporal inputs. The test does not require that
  shared revision to be zero.

The test-only change adds no production behavior, owner API, section, operation
ID, serialized payload, or gameplay semantics. It exercises only target owners
that the current private packages actually construct. It does not prove that a
future G coordinator constructs these owners, binds all target providers,
validates them in the required order, or publishes a graph. P8-C/D, global
receipt-cache and Crime-sentinel target checks still require the G composition
boundary. This evidence does not establish complete live owner/cardinality or
operation/epoch coverage, target-bound restored-boundary admission, global
quiescence, a completed-boundary token, publication, whole-graph validation,
continuation parity, P12-G/P12-A/P13 readiness, or Phase 12 closure.

## Validation

Unity Editor: `6000.3.9f1`. The final candidate results all report zero failed,
skipped, or inconclusive tests.

| Run | Result | XML SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|
| `P12CPrivateRootCompositionTests` | 53/53 PASS | `BC7C0139065ED0BC92E68C39A07BD29A7F3FBE9CEB0ADFC587474BB69A4FD0A6` | `9E23127CC73C3A828CF47A6456193828EC19E5A7C597CF3B3255F855E0412D8F` |
| ALL EditMode | 2733/2733 PASS | `0BD24983E177264E3E6C9FBC5C44F52DD2DE872B24C136BFD0A2286064F1DC80` | `B0F5A5FC260BC02DC2E192BB72D9F549ACC18CD2417D531C2BDA2FD892DE4805` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `79F8D780B9A38CB41AD9F2B473F75F51C4992EEEB98E6A369EBE568E346D3048` | `0D6B411AD1EEC3429F077944CBE2ED2F161D638F7445B7737A8DF06ACDCFA8FF` |

Artifacts are stored alongside this manifest under `Focused`, `AllEditMode`,
and `OfficialSmoke`. The successful runs used:

```powershell
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter P12CPrivateRootCompositionTests -ResultsDirectory docs/validation/P12GStagedPackageTargetWitness/Focused
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -All -ResultsDirectory docs/validation/P12GStagedPackageTargetWitness/AllEditMode
.\Tools\UnityValidation\Invoke-UnityValidation.ps1 -ProjectPath . -Mode EditMode -TestFilter Smoke -ResultsDirectory docs/validation/P12GStagedPackageTargetWitness/OfficialSmoke
git diff --check a73fdff^ a73fdff
```

The first scratch focused run exposed an incorrect test assumption about the
ActorChoice witness owner identity; the test was corrected to compare the
store's canonical census identity. Its retained pair
`Focused/EditMode-20261009-223954-8544179f2a12445d8026602e6ff24a89.xml` and
`.log.gz` is diagnostic only. Only the corrected focused result and the full
suites above are candidate validation evidence. Unrelated ProjectSettings
edits, existing untracked `.meta` files, and earlier diagnostics remain
unstaged and untouched.

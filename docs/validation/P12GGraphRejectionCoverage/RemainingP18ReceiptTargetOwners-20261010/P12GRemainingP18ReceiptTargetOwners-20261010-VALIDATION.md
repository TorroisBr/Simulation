# P12-G remaining P18 receipt target-owner rejection — exact-tree validation

## Candidate identity

- P12 canonical base: `301957c57c2a92838736dcae1df6850daae7b94c`.
- Test code commit: `64f6eee8d5e2be949a1b76e4a8709e130e8ccfd1`.
- Candidate Git tree: `f2d900e34cc55a3238b4014a819446da76e6633f`.
- Candidate `Assets` tree: `2611c3a53175aded6505cd6e2d6b591d9ec1e840`.
- Changed code path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only.
- Unity Editor: `6000.3.9f1`; runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Bounded evidence

Four cases extend `DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically`:

- `p18-crime-receipt-owner` substitutes the source CrimeSystem reference in a
  private candidate runtime. Source and staged target witnesses are distinct,
  each with the required singleton cardinality and zero local revision. The
  runtime's captured target vector remains bound to the staged CrimeSystem.
- `p18-justice-receipt-owner` applies the same identity substitution to the
  JusticeSystem receipt witness and verifies the staged target-vector identity.
- `p18-npc-local-observation-receipt-owner` substitutes the source Local
  Knowledge Observation provider for each matching staged row while preserving
  the other provider family.
- `p18-npc-merchant-trade-state-receipt-owner` does the corresponding
  substitution for Merchant Trade State receipt providers.

Each per-NPC family has exactly one row per NPC, uses stable matching section
IDs, and retains exact-zero cardinality/revision. Source and target owner
identities differ while the runtime-captured target rows remain target-bound.
The existing target sentinel rejects each mismatch as
`TargetOwnerVectorFailed` before publication.

The shared harness verifies that the active source session, completed token,
health, and complete owner graph survive; the subsequent ordinary advance
matches an uninterrupted control; and a valid restore retry succeeds. This is
test-only evidence and changes no production runtime behavior, P18 semantics,
selected-profile scope, capture policy, or P12 readiness.

## Validation

The focused result explicitly reports all four new cases as Passed. The full
EditMode and official Smoke XMLs both report zero failed or skipped tests.
Compressed logs were decompressed and their raw SHA-256 values rechecked.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|---|
| Focused `DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically` | 18/18 | `EditMode-20261010-181754-6d943ba6da074cf99db2a3750d40abdd.xml` | `7E97462A0E5A1AC427827B1F73EE3AB9AA0AE8F657032D919D8B2CB192B26FC6` | `E4978A7B4F5421227C3E69F164E90A049F71C152E0FBF5BE9AA91BD88683C72A` | `0BB4310CB14F199BDFD5DDB044CF2D2C317639EDF2CE501D861D2369E5519E20` |
| ALL EditMode | 2803/2803 | `EditMode-20261010-181817-bbf0e40d65864c73935d8cae33c722e1.xml` | `068F8F57D2D8CE3C3E4DE00919E49846A97C5DFE4E9FFEE8077AB1D15C4FC36D` | `00C693E37D33B66A2C8B9C9055C36EC183CDF1A5C5D52FB9F92CF6034D36588A` | `060136F12AABAFB146B868158CC4D5A70DFD1679047922566B84B9970FECFA37` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `EditMode-20261010-181901-fa57588c3e994cf09b96f28888476e54.xml` | `AA680D8E32D9BEF9A0CCF015F02CC05FA851F44DD5CCB4FA46377FA885B8165C` | `0BA625A66C4822FE65A532866787CD1DB23F124058F312D00E5758428965862F` | `793B1DF32BA8F544E8E5647993E17AB0490B5A601722C8168708DCB5246DEF46` |
| `git diff --check` | PASS | `301957c..64f6eee` | — | — | — |

The tested `Assets` tree equals the code commit's `Assets` tree above. No
protected ProjectSettings or unrelated `.meta` files were staged or modified by
the validation run.

## Status boundary

P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
`BLOCKED`; Phase 12 remains `OPEN`. This increment closes only the four known
P18 receipt target-owner substitution cases. It does not establish P12-G
completion, capture eligibility, export/hydration, downstream readiness, or
Phase closure. The broader §6 matrix, failure-boundary, no-replay, and
multi-boundary parity obligations remain governed by the current P12-G
technical contract.

# Independent exact-tip review — P12-G remaining P18 receipt target owners

- Verdict: **VALIDATED_CANDIDATE**
- Candidate branch: `codex/phase12/P12GRemainingReceiptTargetOwners`
- Candidate tip: `7cbf8a1b92f8a0a988c8974df990b5c0a0a4f425`
- Base / current P12 canonical at review: `301957c57c2a92838736dcae1df6850daae7b94c`
- Code-bearing commit: `64f6eee8d5e2be949a1b76e4a8709e130e8ccfd1`
- Candidate Git tree: `f2d900e34cc55a3238b4014a819446da76e6633f`
- Validated Assets tree: `2611c3a53175aded6505cd6e2d6b591d9ec1e840`

## Review findings

The candidate is a clean two-commit descendant of the stated base. The only code path changed is `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`; other additions are the exact-tree validation manifest and its three XML/compressed-log pairs. No production code, ProjectSettings, unrelated `.meta`, or unrelated raw XML changes are present.

The four added corruption cases are bounded to the P18 receipt-owner providers:

1. Crime receipt: substitutes the source CrimeSystem reference into the staged candidate runtime.
2. Justice receipt: substitutes the source JusticeSystem reference into the staged candidate runtime.
3. NPC local-observation receipts: substitutes same-section source providers into the candidate composition for each NPC.
4. NPC Merchant trade-state receipts: performs the corresponding per-NPC substitution for that provider family.

Crime and Justice source/target witnesses are distinct; each has cardinality 1 and local revision 0. The captured candidate owner-vector row remains tied to the staged target owner. For both NPC families, provider arrays contain two rows per NPC; selected and corresponding source rows are exact-zero cardinality/revision with distinct owner identities. The substitution is applied by matching stable section IDs, and the candidate target vector is checked to retain its staged target-owner identity and one selected row per NPC.

Each case uses the existing restored-graph rejection harness. It expects `P12GDailyV1RestoreFailure.TargetOwnerVectorFailed`, checks the source active session/token/health/complete owner projection remain unchanged, compares subsequent continuation against the uninterrupted control, and exercises valid restore retry. No production behavior or capture policy is changed.

## Validation evidence

The exact-tip manifest is `docs/validation/P12GGraphRejectionCoverage/RemainingP18ReceiptTargetOwners-20261010/P12GRemainingP18ReceiptTargetOwners-20261010-VALIDATION.md`. It binds the validation to code commit `64f6eee8d5e2be949a1b76e4a8709e130e8ccfd1` and Assets tree `2611c3a53175aded6505cd6e2d6b591d9ec1e840`.

Manifest-reported gates and hashes:

| Gate | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Focused corruption suite | 18/18, 0 failed/skipped | `7E97462A0E5A1AC427827B1F73EE3AB9AA0AE8F657032D919D8B2CB192B26FC6` | `E4978A7B4F5421227C3E69F164E90A049F71C152E0FBF5BE9AA91BD88683C72A` | `0BB4310CB14F199BDFD5DDB044CF2D2C317639EDF2CE501D861D2369E5519E20` |
| ALL EditMode | 2803/2803, 0 failed/skipped | `068F8F57D2D8CE3C3E4DE00919E49846A97C5DFE4E9FFEE8077AB1D15C4FC36D` | `00C693E37D33B66A2C8B9C9055C36EC183CDF1A5C5D52FB9F92CF6034D36588A` | `060136F12AABAFB146B868158CC4D5A70DFD1679047922566B84B9970FECFA37` |
| Official Smoke | 5/5, 0 failed/skipped | `AA680D8E32D9BEF9A0CCF015F02CC05FA851F44DD5CCB4FA46377FA885B8165C` | `0BA625A66C4822FE65A532866787CD1DB23F124058F312D00E5758428965862F` | `793B1DF32BA8F544E8E5647993E17AB0490B5A601722C8168708DCB5246DEF46` |
| `git diff --check` | PASS | `301957c..64f6eee` | — | — |

The focused XML header and Smoke XML header report the same counts and zero failures/skips as the manifest. The artifact paths and candidate GitHub blob identities were inspected at the exact candidate tip. No Unity tests were rerun for this review.

## Limits

This test-only increment covers the four named P18 receipt target-owner substitutions. It does not close P12-G, establish full owner/epoch coverage, or imply capture/export/hydration readiness. The candidate manifest records P12-G as `WAIT_DEPENDENCY`, P12-A as `WAIT_DEPENDENCY`, P13 as `BLOCKED`, and Phase 12 as `OPEN`.


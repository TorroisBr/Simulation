# Independent Exact-Tip Review — CORE-F P12-F Core-Bound Encoding

**Verdict: `VALIDATED_CANDIDATE`**
**Reviewed:** 2026-10-30

## Exact identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate tip: `c037cff3ad001a136127708703a9f15080c203ea` (docs-only validation record after code)
- Code commit: `62157a822bffd2f2b2c9717aecba4ec471f85bec`
- Code tree: `570615a49938a476052e086283a112d8d17b5b20`
- Candidate tip tree: `61c3124f8794be1c46ce3b5ff03ca638992ef4d9`
- Immediate base / code parent: `99aa3218980dae9d78be7bc52bdb8a695ad19efb`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate ref matched `c037cff` at review time. The six-path code delta is an ancestor of that tip; one validation record is the only additional path. No canonical promotion is claimed.

## Review result

No actionable code or test-integrity findings.

The operation-backed path now retains the P12-F source snapshots on the exact active continuation-save operation. Capture is taken from the selected Core graph's PoliticalKnowledge, ActorChoice, ScheduledDirective, TravelParty, and Expedition authorities against the operation's token and exact owner census vector. The existing snapshot capture routines enforce identity/cardinality/revision matching, and the operation revalidates before capture, after capture/binding, and after encoding. Reuse is tied to the same operation token and owner-vector references.

`DailyV1P12FCoreOwnerSectionEncoder` uses the existing P12-F codecs for those five snapshot types, rejects missing sources, non-required or duplicate sections, and returns deterministic ordinal section order. The Unity adapter supplies these Core-produced sections to the existing owner encoder; D/E encoding and the image composer remain intact. The encoder's mutually exclusive inputs preserve the prior Unity capture path when it supplies the existing F capture, while the app-backed path supplies Core sections. The restore coordinator's operation-backed branch uses the Core source and section encoder; the null-operation legacy branch and its former header/root/owner encoding order remain unchanged. Failed capture/encoding or stale revalidation returns failure without a sealed image.

The added regression compares each Core-produced P12-F section with the sealed image, including section version/role/revision, row count, owner keys, row revisions, and payload bytes. It supplements the existing full-image census and deterministic restore/continuation assertions. No assertions were weakened. The code delta changes only the six implementation/test paths in the validation record; the candidate adds the scoped validation record as its seventh path. Protected ProjectSettings, `.meta`, private evidence, and unrelated files are excluded.

## Exact-source validation reviewed

The six tested source/test hashes match the candidate checkout and the private Core/Unity input inventories:

| Input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs` | `0d56bd13d64ee2ba481198c2858b4184189bfa1f11e708340e45a2739ab51e78` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `2a6e51d7684c554ec2ffce84ab6bffc360f1aeca72dc1df673f2141c3af57762` |
| `Assets/_Project/Scripts/P12FDailyV1OwnerPackage.cs` | `bfa9c9e54d11e8aacc524b51c69cdfb272273b4ce102b6176e3bb2828d0d7447` |
| `Assets/_Project/Scripts/P12ADailyV1OwnerSectionEncoder.cs` | `5847937d0b85c07d57b8c2315ae2d39779be229634fe6bc49ef2f78c8ec1b9b1` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `9b539cb4b29f0a024f35c4df2b2875d634fd4b80fb23ab9d8138e2e8afd460a6` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `5c7683be1fc6a4b4cd9358edd5f7de2a86acb2ef844e3562af441b9f55b3f155` |

- Official CoreValidation run `8b6fac8029ea42169821665ae3b8975d`: cold 936/936 and warm 936/936, no skips; stable input digest `d471f745efd9b96bd36d045c0af87233d58001228665c8924561cc25220c34b0`. Protected manifest SHA-256 `85f13bd12a23aa1ccf0cf83455ab43cac3dc01323e60cd28a6d23015bdc1aea7`. Cold TRX SHA-256 `6be5f2f52a9007fae25f6825d74be3d86236a2afe0502ba130677874b0455d23`; warm TRX SHA-256 `1063329e26d11b64d6602202f7baa410ddf942299f16577f204629b6e4deb4d5`.
- Official Unity focused EditMode run `a157170e2dc9406da6f28a524d2f13ed`: `SimulationRuntimeAdmissionTests.DailyV1RestoreHydratesNonemptyPersonAndGenealogyGraphAndContinuesDeterministically`, 1/1 passed with stable before/after inventory. Manifest SHA-256 `7f4c6890718cf727165e402ab1ddfbf225dd83768e576201c05fee8eb3481684`; original XML SHA-256 `d505fdf55eb7a083ab56e7ce76240516c75a2282bbfc29c840bfb569d78ac28c`; original log SHA-256 `a259b2ce77beefe0d4700d148c75d2b67a175bbee70a1e8bb882974d57d0d24b`.
- Protected original manifest, report, XML, and log hashes matched the records. The earlier Unity attempt without XML is excluded from passing evidence.
- `git diff --check` passes for the exact base-to-candidate range.

Private originals remain in protected evidence storage; no absolute workstation path, raw log content, or machine/user identifier is included here. Full Unity EditMode, Smoke, LongRun, permanent privacy-safe evidence packaging, and CORE-F acceptance are not claimed. Those broader gates remain necessary before eventual promotion.

## Promotion status

This review covers only the bounded Core-bound capture/encoding integration for P12-F. It does not claim complete Core-owned P12 capture or restoration, detached .NET hosting, or CORE-F completion. No canonical promotion is approved or claimed.
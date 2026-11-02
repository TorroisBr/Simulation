# P12-F PoliticalKnowledge owner identity on restored Core runtime

This is a bounded follow-up to the independent `NEEDS_CHANGES` review of candidate `7894e637ceff192e76a425d6e7a5179b8fdc8433`. It fixes the restored owner-graph binding and Unity projection identity only. It does not complete or promote CORE-F.

## Change

- Core now binds the exact staged P12-F `PoliticalKnowledgeStore` to the restored selected-owner graph and requires that reference when accepting the staged Core session.
- When a Core-prebound graph is projected into `SimulationRuntime`, the Unity runtime now retains that exact owner instance and rejects a different supplied instance. Non-Core construction retains the existing clone behavior.
- The restored continuation regression asserts that the Core graph and Unity projection refer to the same `PoliticalKnowledgeStore` before continuing.

Candidate implementation commit: `78d3bffa527b7599167e5c84b6315ed95f708439`, based on `7894e637ceff192e76a425d6e7a5179b8fdc8433`. The candidate's source files are identified by these SHA-256 digests:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `978b2d100955c762d347d70ed7b0f84edb859cc692e9d2600ca9050c09e46af3` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `8581a66e8118b11dfd8e69437433fa812fd2d23bf5508d060341c63d1a0159a1` |
| `Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs` | `afc30f5592bd92dc257440f578ce93b680584c8dd43613c57e177aa4ce4ae637` |

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | 988/988 passed | Run `85fc2cbb69164d8eaffb0c1240dff8dc`; the Core application source in this candidate is unchanged from that passing run. |
| CoreValidation warm | 988/988 passed | Same run. |
| Restored military/site-knowledge continuation test | 1/1 passed | Official EditMode filter; run `9e9f8f71562a4765a88f08059828ad7d`; exercises image restore and deterministic T+1/T+2 parity with the explicit owner identity assertion. |
| Restored target-owner-vector atomicity test | 1/1 passed | Official EditMode filter; run `b0243ee85da841e4a7edaefb14fa0d27`. |
| Restored image atomicity parameter cases | 2/2 passed | Official EditMode filter; run `abcece06c9204644ba4a7b3aa41cd7ac`. |
| Full Unity EditMode | 3294/3294 passed; 0 failures, skips, or inconclusive | Official runner at candidate `78d3bffa`; run `0c67adcf0e944be7955e1b0a4e5da69d`; input digest `1c1efee9fb82d329fdf1b759f594db1f6c5a071f6d10cf34cbab7c152c4af9fe`; original XML SHA-256 `fb3173cccdf4b7bcc35baf21a26c18846b647fe08cbcdd76971c333d9b018b64`. |
| Official Smoke filter | 5/5 passed; 0 failures, skips, or inconclusive | Official EditMode filter at candidate `78d3bffa`; run `88aaa536ba10403dbbc41db2b18b12bb`. The five Smoke cases are also present in the full EditMode result package. |
| `git diff --check` | PASS | Candidate implementation commit. |

The full EditMode package is `UnityEvidence/p12f-political-knowledge-owner-78d3-full-editmode.zip` (SHA-256 `ec1e2ecdfab127660d4f4f72444e9c9c244b4da6b168f958fa32aa92618f6eab`). Its manifest is `UnityEvidence/p12f-political-knowledge-owner-78d3-full-editmode.manifest.json` (SHA-256 `f308df79cc9fe18e5b3abe019b901785fb08f174cd7d2e728cceb28db8a46ba4`). The approved verifier returned `VALID` for one complete run and one result entry. The original XML and runner logs remain preserved in private evidence storage; no raw evidence was modified or published.

The official filtered Smoke run is retained privately. The full EditMode package independently contains all five Smoke test identities and their passing outcomes.

## Review and scope status

This correction addresses the prior review finding that Core restoration did not retain the staged P12-F `PoliticalKnowledgeStore`. The graph now requires reference identity, and the Unity projection no longer clones that Core-owned instance. The candidate still requires a fresh independent exact-tip review. It is not promoted to Core canonical, and it does not claim full CORE-F acceptance, a detached .NET host, or any Phase 12/13 readiness change.

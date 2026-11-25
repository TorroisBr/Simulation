# Core selected Daily-v1 execution boundary

## Candidate

- Commit: `ba4ce8c9d0a86279e880261bf7854ccfc9cb1134`
- Parent: `13bdf7d1022297a28acdd09073646d7cf9fb9539`
- Commit tree: `d00ab5852971db310d9b0e17839644270291785e`
- Scope: Core constructs the selected execution boundary against the exact admitted execution state and owner graph, derives selected actor/demography/travel contexts from those Core roots, and receives narrow host effect/writer/projection/P12 ports. The Unity adapter rejects a substituted graph. Mixed-roster fallback remains selected from the Core roster.

## Validation

- Official CoreValidation, SDK 9.0.315: **987/987 cold and 987/987 warm PASS**. Run `e9f6ab2551d44914bc2e15b16fdcff79`; manifest source commit/tree match this candidate; source input digest `b44ef5eaa52b566e8214c9190a4fedd387981666e68a54641b6c9a1ffce9d447`. Private native manifest SHA-256 `d1cf1caa069cf448f1e49820f2bfbe28c95ae1cce7d5a978a4b095514fa04daa`; cold/warm TRX SHA-256 `e5210eabc586a65413e38b28d5a7420fa71f86dbb553aa24e7ebc5b0db9fda24` / `647185c1d3d3e6737bfcbf644c2cbe941b59ffcea8126c9312630317ee9b4ba1`. These originals remain in protected local evidence; no public .NET package is claimed.
- Full Unity EditMode: **3284/3284 PASS**. Run `1b8c541076364e30a02c8ba502cf0fa1`; candidate tree `d00ab5852971db310d9b0e17839644270291785e`; stable input digest `28f5531529245c09951e300e55ef58c5365e88fe5c457225b0827fb153674f58`; zero failed, skipped, or inconclusive. Original XML/log SHA-256 `6f265f69a24cae6bbf19d2f7921a2c0be0f67c286de12c5a4db4c33669471e9d` / `94e6935b1d19311fdafd0309279dea509c3f017b0762dba9025e55d4ba7b602f`.
- Official EditMode Smoke filter: **5/5 PASS**. Run `78703a880825447fb1f98101a9fa3947`; same stable input digest as full EditMode; zero failed, skipped, or inconclusive. Original XML/log SHA-256 `6b26dfed3c3161e553d1e4bbdeeffc4db677d78a4e3231a157d8a22d4ccdf5b3` / `81442275cf7f18bf2e986a5caee375108a0fbf3b5db9fa967fe0686492bd661f`.
- Privacy-safe full EditMode package: [manifest](unity-editmode.manifest.json), [archive](unity-editmode.zip). Archive SHA-256 `17b56445cc1f0998c60a64795076cff13fc6260d41d0f3db4d1933ef5ab7ea7c`; manifest SHA-256 `aa6f50b528bea7466feb4161c6ad5812ab5bcc28a953b89d9bd83fba49bf904a`. The approved verifier passed against the protected original run manifest (`runs=1`, `entries=1`). The archive contains the full EditMode report, including the five Smoke cases; the filtered Smoke original remains private.
- `git diff --check`: PASS.

The Unity input snapshot also contained pre-existing user changes in `TesteSimulacao.cs` and ProjectSettings. They were not changed or committed by this candidate. The CoreValidation manifest covers its declared Core project inputs; raw reports remain private under the evidence policy. The Unity package binds the committed tree and full test input digest.

## Independent review and limits

Independent exact-commit code review: **PASS**, no actionable findings; see the companion review record. This is a recoverable Core execution-boundary milestone only. It does not complete the shared `DailyV1Application.TryStart`, detached P12 capture/restoration, descriptor-v2 host integration, the executable .NET host, or T/T+1/T+2 continuation. No Core canonical promotion is claimed.

The CoreValidation runner reported that a Unity Library was present (LibraryAbsent=false); this checkpoint makes no Library-independence claim.

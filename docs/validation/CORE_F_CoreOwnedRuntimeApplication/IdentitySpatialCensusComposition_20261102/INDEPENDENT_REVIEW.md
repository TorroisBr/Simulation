# CORE-F identity and spatial census composition — independent review

**Verdict: `VALIDATED_CANDIDATE` (PASS).** This review applies to the exact candidate commit `2aa79527bbd93446dfb848086a9d3d8df764c917`, tree `f5b2fcd13762db1bbc2f2c93982aa791f7389047`, based on `bfaa45faee39e62f9e3f447f961ca81b77bf7157`, tree `525c489f39e85dc3e5ab4e86de5cabb96b4decf5`. Current Core canonical at review was `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. Candidate remote and local tip matched. Repository origin was `https://github.com/TorroisBr/Simulation`. This review does not authorize canonical promotion.

## Scope and prior finding

The cumulative base-to-candidate change covers 12 files; the latest corrective commit changes only `DailyV1RuntimeAdvanceStage.cs` and `DailyV1ApplicationSessionGateTests.cs`. The prior review finding was that `TryComposeCoreOwnedIdentitySpatialSections` passed through a complete vector when all active Core identity/spatial section IDs were present without checking the row facts against the Core owners.

The correction closes that finding. The all-active-rows-present branch now compares every active host row against the freshly captured Core witness using exact section ID, schema version, role, owner object identity, cardinality, and local revision before returning the vector. Regression assertions exercise each altered fact through both this composition method and the host-filter path. The composition branches retain their intended behavior: a partial active-source omission fails closed; active rows wholly omitted by the host are recomposed from Core; inactive sources must remain represented by host rows; and a complete compatibility-host vector is retained only after exact active-row equality succeeds.

The same composition helper is used by the daily capture boundary, restored-target owner-vector construction, and final execution-boundary capture. No new issue was found in those call sites. The P12 census extension keeps existing role/schema/identity/cardinality/revision conventions. The 89-family completeness and owner-alias validation were not changed; the cumulative contract delta only names the new fixed-empty section IDs and routes the exact selected spatial owner through the existing Core owner graph. The established `application == null` route is unchanged.

## Validation evidence

All validation records below identify the exact candidate commit/tree and stable inputs; no tests were rerun for this review.

- Official CoreValidation run `90916662881e46b6b5b557de98e6ccdd`: PASS, exact source commit/tree, stable input digest `d96437eba444a1694891551a9d77fec3db05a6bcbe408cbb786feead4e0e196d`; cold and warm each passed 988/988 with zero failures, errors, or unexecuted cases. Cold TRX SHA-256: `6b27abd086c1edad9fdcffb713961f84de2e4aa1e446a663659e6d38c8ef4299`; warm TRX SHA-256: `f9312d5ecb3c57f3aeb43ae31ddcbd520d1344e63efb2b7631077a3f2ecbf402`.
- Official Unity EditMode run `1e4fbdcfddea4a7180b6e0b2ad211ac0`: PASS, exact source commit/tree, `inputStable=true`, input fingerprint `0daf7f424003423dd9402a6c902b1f4a96ad604548fc5c17b9a0a657368b9ae2`; 3294/3294 passed, zero failed/skipped. XML SHA-256: `094d2a36236bb9a94969876c3faa7cd703eb82db1f6f332b75ba4eec4fb21875`; log SHA-256: `00072d01a8ce35d2dab948bdea438553a1799e4204773b7ec19ceefc867d8446`.
- Official Smoke run `d51092846e814c1a9f989a6b9c04e6d6`: PASS, exact source commit/tree, `inputStable=true`, the same input fingerprint; 5/5 passed, zero failed/skipped, using the `Smoke` EditMode filter. XML SHA-256: `9ba6783cc9d712021dc2ab566ef1d0e856f39997fa075864456b6f8186840c46c`; log SHA-256: `d0157addcfdd3989c1f33417b9b264ebf963d6905ead654456b6f8186840c46c`.
- `git diff --check` from the reviewed base through candidate: PASS.

Original XML and logs remain in their private evidence stores; this public record includes run identities and hashes only, not workstation paths or private raw payloads. No credentials or other secrets are recorded. The candidate worktree contained unrelated user-owned modifications and private/untracked artifacts; they were excluded from this record and left untouched.

## Review conclusion

The exact-fact gap identified on `bfaa45fa` is corrected and covered through the Core composition helper. The reviewed candidate is suitable as a validated implementation candidate, subject to the repository’s separate canonical-promotion preflight and policy. No canonical promotion was performed or recommended here.
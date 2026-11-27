# LAB-OBS-A Official Producer — Exact-Tip Independent Review

**Verdict:** `VALIDATED_CANDIDATE`. No actionable implementation, test-integrity, provenance, privacy, or scope findings. This does not promote the candidate or close all LAB-OBS-A obligations.

## Reviewed identities

- Candidate branch: `codex/core/LAB-OBS-A-official-producer-4dd2c2db`
- Candidate commit/tree: `3eaa62d98faf3fb68958588f04ef25a576ccd0a9` / `904010da94f5b58025c2d1e28caf08fabd0ac477`
- Base, parent, and cached remote-tracking Core canonical: `4dd2c2db755b5a86e003933e65e0a5bf9b02f3df`
- Worktree was clean; complete base diff is exactly 12 paths: producer implementation, CLI integration, assembly/project/lock files, producer tests, and the public validation README, summary, manifest, and normalized package. `git diff --check` passes.

## Implementation review

The existing no-argument `Simulation.Lab` finite-source path remains the `Main` fallback; the normal cold/warm Core+Lab demo gates pass. The new command accepts only `observe-daily --out <new-directory>` and rejects unknown/extra arguments. It starts the approved Core facade with the fixed selected profile in a fresh scratch root, captures Day 1 and Day 2 from the same Core run/session, and encodes only returned detached observations. No Unity runtime, second simulation engine, P12 writer, or new authoritative mutation path was introduced.

The encoder emits the fixed v1 schema with stable property/row ordering, invariant numeric formatting, UTF-8 without BOM, canonical string escaping, identity/link validation, and distinct absent-stock versus present-zero representation. The producer verifies consecutive day/sequence, same WorldId/run/build/input fingerprint, stable scoped City/Item identity, a real nonzero TRIGO stock delta, and a predecessor digest over the exact first artifact bytes. The committed output summary and protected CLI provenance pair agree: two files, hashes `6d0bbe954296c63aefc75e840ac2b9911b75f3230245065c87030bb30a42e977` and `2ce0a22066fa4df9c4430c6a30d23e3faa3f630af285a075d5dc50f33bf15402`; same world/run/build/input; indices 0/1; predecessor link valid; Day 1/2 and sequence 1/2; two City rows with null P8 bindings; one geography hex and Location; TRIGO stock delta +30.

Each artifact is published via a same-directory create-new temporary file, write-through/flush, read-back/hash check, then no-overwrite move. Tests cover create/write/flush/readback/rename failures, no overwrite, malformed/foreign encoder inputs, deterministic encoding, and CLI pair output. The boundary is per-artifact atomicity: a later failure preserves an already committed first observation, as explicitly tested; the command returns failure and never overwrites it. No all-or-none pair transaction is claimed.

## Evidence and applicability

- Official CoreValidation run `390de45267554676a8ec529dc569eae5`, input digest `288e0c18dc7bdd59e33f5adecfcf4febebfdd457fe33805f78c2a4aecb73ddb5`, `LibraryAbsent=True`; cold and warm each `1,241/1,241`, all eight Core/Lab gates passed. The native manifest inventory's 571 source inputs match candidate files byte-for-byte; four generated dependency identities are recorded. The public normalized package contains only cold/warm TRX. ZIP SHA-256 `e2f915d872d7a1db09e13f1990c1dc81700f68abe12fdea07495c80b441fb593`; manifest SHA-256 `6d7fd28e66f046838d970d39d909f2f3cc7f724eb50f1c053a663f8f7f452031`. The official verifier reports valid integrity and original comparison. Independently parsed archive reports have 1,241 cases each, all Passed; inner TRX hashes match the package manifest.
- The public manifest uses repository-relative input/dependency paths. The manifest, summary, and both normalized reports contain no absolute paths or credential markers. Raw output reports/logs and the private CLI provenance record remain outside the candidate/package; that private record binds the same run, source input digest, successful CLI exit, and output hashes. The protected output pair was independently located by its hashes and its parsed facts match the public summary.
- `Program.Main` retains no-argument behavior and the official cold/warm demo gates verify it. No tests were rerun for this review.

## Findings and remaining scope

- Code/test/evidence findings: none.
- The local `codex/core/canonical` ref was stale at `08f6700`; cached `origin/codex/core/canonical` and candidate parent were `4dd2c2db`. Refresh refs before promotion.
- The candidate delivers the bounded official producer/encoder only. Broader LAB-OBS-A closure, remaining same-cut/read-isolation obligations, ATLAS-B, and interactive-host gates remain separate. No canonical promotion is implied.
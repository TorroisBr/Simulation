# Independent review — P12-F CommercialKnowledge census rebind

**Verdict:** `VALIDATED_CANDIDATE` — PASS; no actionable findings.

**Review date:** 2026-10-30.

## Exact refs

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Candidate commit: `cc7c1d721edcef543d96afbc7319f1a1f9d46efa`.
- Candidate tree: `16b1a84eb14c2416262d9fb700160c755a1e09a8`.
- Exact parent/base: `49710bf3a1ab715944fded359f39ac1a129a59e7` (tree `534b7a0379d5763e987f5e740b7cb4ff7ba144b8`).
- Current Core canonical at review: `codex/core/canonical` at `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The candidate branch was verified remotely at the exact candidate commit. The review was performed in a separate clean E: worktree based on that exact tip.

## Reviewed scope and result

This is a bounded CORE-F owner-census rebind for the existing P12-F CommercialKnowledge owner. It does not change owner-section IDs, schemas, serialization, writer behavior, or the accepted Daily-v1 profile.

The review verified that:

- The three markets, liquidity, and share-receipts sections name the exact selected `DailyV1CommercialKnowledgeOwner`; selected owner uniqueness and admitted-definition identity are checked.
- The Unity `CommercialKnowledgeRuntime` is verified as a projection of that same Core owner. P12-D checks Core identity, counts, and revision while reading values through the projection.
- Runtime P12 mutation-boundary installation continues through the Unity adapter, which forwards the boundary to the same Core owner. No parallel CommercialKnowledge authority is introduced.
- Core census rebinding rejects duplicate/missing section keys and uneven family counts, checks selected owner witnesses, preserves ordering and the whole host vector for mixed rosters, and leaves stale captured rows unchanged for existing P12 classification.
- New tests cover selected owner identity, the shared three-section owner/revision, populated and empty section cardinalities, mixed-roster preservation, stale-row preservation, missing selected rows, and uneven family rows. Existing assertions were not weakened; bootstrap expectations now assert the Core owner and its Unity projection alias.
- The commit changes only the eight intended production/test files and the validation record plus its two public Unity evidence artifacts. It contains no ProjectSettings, unrelated, or private evidence files.

The candidate adds no family IDs and preserves the existing complete census vector. It does not claim P12-G completion, P12-A readiness, complete phase coverage, or CORE-F completion.

## Validation and evidence verification

The associated record is [`VALIDATION.md`](VALIDATION.md). The eight recorded source SHA-256 values match the candidate worktree.

- Official CoreValidation run `a5c3ee9476574882a1bbd9286a0e9c95`: cold 957/957 and warm 957/957. The protected native manifest hash is `44ff242b1a2c570e90b91138abbdcc8450d94c7ae3a2934ecc405231f2419234`; its stable input digest covers 536 files. I independently rehashed all 536 inputs against the candidate: zero missing or changed. The original cold and warm TRX hashes also match the record.
- Official full Unity EditMode run `d5777e4f4bb648958cbaf460fdeb8248`: 3283/3283, zero failed, skipped, or inconclusive. The private run-manifest hash is `6a00a8cbf8f095739290df4c570e5a498a7dfcf94b459fec083d1b7bbfdcf003`; original XML hash is `60604d7ee170d546f00f19287680033ceb1f59e9ed4054e56d5636fa88547390`.
- Official standalone Smoke run `029c29c2340a4286992b3f3135e5ac9d`: 5/5, zero failed or skipped. Its private run-manifest hash is `1a51a977eb6cded90eacad47fd84443f99cc66ae09f358938d5d917b478c3098`; original XML hash is `0a34e2eeeb6335edf51b2472b7709011cf6957d2b5b8c18a7544e0d1cc929507`.
- The full EditMode package manifest hash is `1c93d12b759a3bdcb5701124a06ca7b6c63f5dd2508fd33bc150302ff20e1115`; archive hash is `7a8010ca35789f0dde0772124dd63ebc4179a901a32154fb12ea13196d8cdd73`. These match the validation record. The packaged report contains no local user path. The full EditMode bundle includes the Smoke tests; the separate filtered Smoke run is retained privately and is not claimed as a public package.
- No public .NET TRX package is claimed: the native CoreValidation manifest does not satisfy the separate `dotnet-trx-source-context-v1` packager input contract, and the packager rejected it. The original reports and native manifest remain protected; no context was fabricated or evidence rewritten. This packaging limitation is accurately disclosed in the validation record.
- `git show --check` passes for the exact candidate commit.

## Findings and boundary

No actionable code or test findings remain for this bounded candidate. This review does not approve canonical promotion, formal CORE-F acceptance, or any P12 readiness change. The candidate must continue through the separate applicable integration/promotion workflow after the remaining CORE-F requirements are satisfied.

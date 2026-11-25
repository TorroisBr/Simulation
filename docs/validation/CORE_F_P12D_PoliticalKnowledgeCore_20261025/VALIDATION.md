# CORE-F P12-D Political Knowledge Core Validation

## Scope and source identity

- Candidate implementation commit: 09a94f5fbe5013bdf9198f7b7a9079a56f450dab
- Candidate source tree: 29bb5e659419533b25c59cf30652eb8518514511
- Implementation base: 0314023dd211eeca96e29c350e027b019652382a
- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate branch: codex/core/CORE-F-p12d-core-hydration

This bounded slice moves the existing selected Political Knowledge, Faction,
Property, factual-read admission and P12 image codec/snapshot sources into
Simulation.Core, and moves the Political Knowledge census provider with them.
The existing Political Decision census provider remains Unity-owned. The codec
and owner source moves preserve their prior implementation; focused Core tests
cover owner identity/revision roundtrip, invalid rows and empty-image staging.
This is an implementation checkpoint only. It does not complete CORE-F, P12-D,
P12-B or P12-A and is not a canonical promotion.

## Validation input applicability

The final validation manifests were captured while the candidate worktree was
at review-record commit 0314023dd211eeca96e29c350e027b019652382a, before the
implementation commit. Each manifest's complete recorded source/test input
inventory was rehashed against commit 09a94f5fbe5013bdf9198f7b7a9079a56f450dab
after commit creation. The crosswalk found zero missing and zero changed inputs
for CoreValidation (443 inputs), Unity EditMode (1,914 inputs), and Smoke
(1,914 inputs). This establishes that the validated source and test inputs are
identical to the committed candidate; it does not change the original run
metadata recorded in the manifests.

## Results

### CoreValidation

- Run: 65bf8f6d65d144ca8c9c895720d03e9d
- SDK: 9.0.315
- Cold: 794/794 passed; warm: 794/794 passed.
- Failed, skipped, and inconclusive: 0.
- Input digest: d15d60c269fcc0df8a8e7e09c132cb7186b399d4313cec0adf883917b9534b8
- Private manifest SHA-256: 6de5ddb73bf1e0c554702723be52bccea63541b0e7a0d08367ef23255092858b
- Cold TRX SHA-256: ba5dd98e90f34e3b9b815c3e84833495c638d73c7c9626fc3bd192d56adf96b8 (1,122,439 bytes).
- Warm TRX SHA-256: f5cd9d27982964650e62657d643539e2d2a088867a066a0b5c3dc44b4bb378d3 (1,122,439 bytes).
- Demo and Lab build, cold, and warm steps exited successfully.
- The original TRX files and private manifest remain in protected local evidence.
  No permanent public .NET result package is claimed: the approved .NET TRX
  adapter in the evidence policy is not yet delivered.

### Unity EditMode

- Run: 5416dd79ddde4cf3b9795e0e7114b4f1
- Unity: 6000.3.9f1
- Result: 3,264/3,264 passed; failed, skipped, and inconclusive: 0.
- Input digest: e07874aa35803d2130208e003720e401f5d8c8684c9e58fbf0da33c87c23487e
- Original XML SHA-256: 76558f5f5ff9cfc0b799c0a1c0ca5e3ff0179657356aec53d23e36fcd2a03e57 (3,264,925 bytes).
- Raw log SHA-256: 08fdd353af7307bad773f8cfc189d0a07a21b85d7ba4d453691f71f7b4592e79 (237,528,384 bytes; protected, not published).
- Result-only package: coref-p12d-political-knowledge-editmode.zip, 172,562 bytes,
  SHA-256 9dcbf002648e1e5675a664643135c464c8568de82997b3920934a61eb52615ed.
- Committed package manifest content SHA-256 (GitHub/Git blob LF bytes):
  8424372d0f51cf3de4a4cdafa96434e14aa8f23a02ddae53ffd3faccfb437eb3.
- Windows checkout CRLF representation SHA-256 (not the committed blob):
  17d6913c5c76179fb9df69f4753837d42dc2b3bea9d6a71de86247c47a5dc25a.
- The official package verifier accepted the bundle (runs=1, entries=1);
  a separate privacy scan found no workstation path patterns.
- LibraryAbsent=False; this run does not prove Library-independent validation.

### Official Smoke

- Run: d4ace20abcba47728b342bf94872cd2a
- Result: 5/5 passed; failed, skipped, and inconclusive: 0.
- Input digest: e07874aa35803d2130208e003720e401f5d8c8684c9e58fbf0da33c87c23487e
- Original XML SHA-256: cff4cf3bbf6ed32120a0bb14a4dd203ae7e156651f914bf7e1240a8f0ab24205 (6,353 bytes).
- Raw log SHA-256: 2db00f3530a117b450b91107e42538bda1b189a0e941d3972c6f4eca6426d3630 (51,575 bytes; protected, not published).
- The official result-only packager rejected this filtered report because an
  ancestor suite declares a testcase count that does not match the selected
  descendants (Suite 'Simulation' testcasecount does not match its descendants.).
  The original report was left unchanged and was not hand-edited or repackaged.
- All five exact Smoke test identities were independently cross-checked in the
  accepted full EditMode result bundle, where each passed. The separately run
  Smoke XML and log remain protected locally; this record does not claim that
  the filtered Smoke report has a valid permanent public package.

### Other gates

- git diff --check: passed for the implementation candidate. Re-run after this
  documentation record before publishing it.
- LongRun: not run or claimed; this slice does not change daily-loop behavior.
- Independent implementation review of this exact candidate is pending.

## Evidence boundary

This record does not replace the complete case-level Unity report bundle or
private original .NET/Unity diagnostics. Raw logs and .NET TRX are not included
in the public candidate. The candidate must not be treated as promotion-ready
until the independent exact-tip review and any applicable permanent evidence
requirements are resolved under the current validation policy.

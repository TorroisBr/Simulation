# CORE-F P12-D Restored City/NPC Membership Validation

Date: 2026-10-24

## Candidate identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Code commit: `e57f6aa0fad70952bf6acc83d9ce0b52d8371744`.
- Code tree: `01057713b5ed89fafc821574e2d6e49c32bf2040`.
- Parent candidate: `67691bb8b2314f674da1ffb2bb64aca576451e17`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Production file SHA-256: `697f97e400e6cfedbf1ae4d437b356705ffa9f3c12b93567b65109ed40e1a49d`.
- Test file SHA-256: `e631cc955c4e8493c4bb640518eccd3af999667e304de45c01dab167f2717eba`.

## Delivered scope

The Core P12-D restored-owner graph stager now validates the detached City membership list against each selected NPC's restored current City and Location before returning the graph. It rejects missing or dangling members, duplicate membership, and nonreciprocal links. Membership order is retained exactly and is not reconstructed from the NPC roster. The validation remains P12-D-specific; the generic owner-graph factory and runtime-operation planning behavior are unchanged.

Five focused tests cover order preservation, missing members, dangling members, an NPC listed by multiple Cities, and a nonreciprocal current-City link. Existing exact-owner, deterministic ordering, NPC-only, and empty-vector cases remain in the same suite. No assertions were weakened.

This is partial CORE-F restore integration only. It does not close P12-D, complete P12, establish capture eligibility or whole-runtime quiescence, or complete the detached .NET host and continuation contract. No Phase State or canonical branch was changed.

## Validation

### Official CoreValidation

- Run: `098cc44aff34485291de4a6dd5b5f0b3`.
- Runner: official `Tools/CoreValidation/Invoke-CoreValidation.ps1`.
- SDK: `9.0.315`.
- Cold: `723/723` passed; warm: `723/723` passed.
- Stable compile/test input digest: `2f7a34f0837df8741398922b8b2ec9ce93467a19501220e015dda691b304307e`.
- The run's source/test hashes match the committed files listed above.
- Private manifest SHA-256: `cd0ac83a075074c56f1fe7ee880122c4fae43901534b4655d2c7bef3d5113e5e`.
- Original CoreValidation reports and manifest remain in protected E: evidence retention; they are not published as TRX packages.

### Official Unity validation

- Unity: `6000.3.9f1`; official runner and evidence module.
- All three runs reported stable before/after input snapshots with SHA-256 `aea1aa446fab1add98f90a5781b98ac799474b7672ea804e1ce55b1052f2d578`.
- Focused `P12GPoliticalBindingRestoreTests`: `15/15` passed; run `25bd2c6a786d4644829985de69cec68f`.
- Full EditMode: `3263/3263` passed; run `f9665ee152d647e2b72f747047bde82e`.
- Official Smoke name filter: `5/5` passed; run `a913000fa1ba4b208f302c88e609d972`.
- All 15 focused cases and all 5 Smoke cases also occur in the complete full-suite report.
- The full EditMode result package was produced by the approved privacy-safe packager and verified against the exact private source-run manifest. Filtered originals remain private; the full report is the durable complete result authority.
- Package: `coref-p12d-restored-city-npc-membership-20261024.zip`.
- Package SHA-256: `f0172dbd897e080cf6ea6c6ec9b55d93cccb38b13b72bfdbdd4eeec9a278d929`.
- Package manifest SHA-256: `2a97206801274c74253a4dc7306cc15599d944f25298281e478d4f8cfe21c156`.
- Private original XML, focused and Smoke reports, logs, and run manifests remain in owner-only E: evidence storage; they were not modified.

`git diff --check`: PASS. Two pre-existing user ProjectSettings modifications remained unstaged and outside the candidate.

## Evidence limits

The Unity package contains the complete full-suite report with all decision-bearing test results. The standalone filtered reports remain protected originals and are not represented as separately packaged public result bundles. CoreValidation uses `simulation-core-validation/v1`; no public TRX normalization or Library-independence claim is made.

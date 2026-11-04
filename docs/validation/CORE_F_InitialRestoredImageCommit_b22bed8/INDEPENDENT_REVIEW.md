# Independent exact-tip review — initial restored-image commit

**Verdict: VALIDATED_CANDIDATE**

## Reviewed identities

- Candidate branch: `codex/core/CORE-F-try-start-transaction`
- Exact candidate: `f1db51c27c932f5cc56ff03fd924411be33b7451`
- Candidate tree: `912207e0003644958ea859981ff0c094d4efd278`
- Code/test commit: `b22bed8521bf97b0b21f7354c91dd90ec5624e5c`
- Code/test tree: `5e09ff2a0e003581c1ce5d639ba94b066db691a5`
- Base: `4622e71841e74f6e2f995927cf4cc80bdc9d3f2e`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Reviewer: independent read-only agent; no candidate files edited and no tests rerun.

## Review result

No actionable correctness or contract findings. The initial restore proof is bound to the exact admitted image, staged Core session, world, day, sequence, target owner vector, and mutation epoch. The application commit gate validates the exact admission, proof, projection, and host ports; recaptures and compares the target owner vector and epoch; finalizes the restored boundary; and publishes the Core session and host projection under the same gate. Replacement restore finalization retains its existing path.

The updated regression fixture verifies that no completed capture token exists before commit, rejects missing or foreign admissions and missing projections, rejects a late target-roster mutation at commit without publication, and verifies valid initial restore, image parity, and continuation after commit. No `ProjectSettings` files are part of the candidate diff. Three pre-existing local `ProjectSettings` edits remain uncommitted and untouched.

## Evidence verified

- CoreValidation run `87900d26e6cc470d816e17c3bc906c43`: cold and warm **1,010/1,010 PASS**. The protected manifest and original TRX hashes match `docs/validation/CORE_F_InitialRestoredImageCommit_b22bed8/VALIDATION.md`; the reviewed source/test hashes match the exact code tree above.
- Unity run `dd11aa19399c4606b9c97b1d309ada93`, `P12GPoliticalBindingRestoreTests`: **16/16 PASS**; protected manifest and XML hashes match the validation record and exact test/source identities.
- `git diff --check`: PASS as recorded.

This review does not claim full Unity EditMode, Smoke, LongRun, complete CORE-F acceptance, or Core-canonical promotion. The candidate is not promoted; remaining application/host and detached continuation requirements remain open.

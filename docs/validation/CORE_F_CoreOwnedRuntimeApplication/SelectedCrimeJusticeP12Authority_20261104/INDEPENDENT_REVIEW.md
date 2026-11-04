# Independent review — selected Crime/Justice P12 Core boundary

## Verdict

PASS — exact-tip implementation review; no actionable code findings.

- Candidate branch: codex/core/CORE-F-try-start-transaction
- Candidate: 3314fe41e68f196e4259778eed4f4d4f1418d40c
- Tree: e22b251b3b1d6713f7a7835dda92f2c73cf05726
- Parent: 6cf0af2937d16eaaf36b0a7134feb8e37e895cc8
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Reviewer: independent Codex subagent /root/crimejustice_p12_boundary_review; did not edit the candidate.

## Findings

The admitted selected day-start path now enters the Core actor-turn boundary. Core performs the four existing Crime/Justice P12 reservations in their original order. The Core boundary validates the selected session, owner thread, exact graph, and host-provided census identities before each operation. Unity retains the census/projection/receipt ports and the non-admitted compatibility path.

Mixed-roster execution still uses the whole-stage legacy fallback. The test proves the admitted path avoids calls to the legacy Unity mutation boundary and observes all four Core reservations. Existing phase-order, coordinator, and mixed-roster assertions remain intact; no assertions were weakened.

The exact five-file code delta is listed in VALIDATION.md. Core/test input hashes match the official CoreValidation manifest; Unity-visible source hashes match the full EditMode input snapshot and the packaged report.

## Validation applicability and limitations

The full Unity report contains 3,299 passed cases, including seven LongRun tests and all five Smoke identities. The standalone filtered Smoke attempt produced no result XML and is not counted as a separate Smoke pass. The result-only Unity archive was verified against its protected original and is retained beside the validation record.

CoreValidation cold and warm each report 1,007/1,007 passed and the exact source/test hashes match the candidate. The private runner manifest and original TRX files are retained but no normalized public .NET TRX package is claimed because the required matching private source-context record was unavailable. This gap must be resolved before any canonical promotion that requires permanent per-case .NET evidence.

This review validates the implementation behavior at this exact tip; it does not promote code, close P12/CORE-F, or claim complete shared-runtime/restoration capability.
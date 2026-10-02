# P12-B money-transfer operation design review

**Verdict:** PASS — independent exact-tip technical review  
**Reviewed design commit:** `30c00d78fcb68c2969ac2eac3ed694423c55783e`  
**Base P12 canonical:** `2f7c7422812de40aa1223e8310dcbd9f5d8ca474`  
**Review date:** 2026-10-01 (America/Sao_Paulo)

The design is one account-transfer operation slice within accepted P12-B scope and adds no checkpoint ID or product semantics. Review cross-checked the documented boundary against the current transfer helper and `CrimeSystem.TryExecuteSteal`.

The revised contract closes the raw-account overload bypass by rejecting it before mutation whenever the shared service is P12-bound. The NPC overload uses the actual `float` signature and retains the existing successful `0f` no-op semantics without owner revision, notification, or mutation-epoch advance. The operation scope spans debit, credit, compensation, and result selection; owner hooks remain the sole postcommit notification source; compensation is recorded as another commit; and postcommit bookkeeping faults fail closed without implying rollback. The later theft outcome and warrant remain excluded.

The direct NPC owner-commit invalidation capability is a hard implementation dependency. Until it is promoted, this design is reviewed but remains `WAIT_DEPENDENCY`. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.

The design is docs-only; `git diff --check` passed and Unity tests do not apply.
# P12-E War Owner Snapshot — Exclusive Hotspot Handoff

**Handoff:** `P12E-WAR-OWNER-SNAPSHOT-2026-10-08`

**Owner:** `codex/phase12/P12EWarOwnerSnapshotImplementation`

**Base:** current-base design review record `475b86c30c13aca117cfd3e127745fac509390a6`; revalidation candidate `4c11486c9b918716c4fe1443cfffab681c6088eb`; P12 canonical parent `77135b3e0ca8df83c6852f2c234ff9098a833468`.

This worktree has the exclusive edit handoff for the War region of
`Assets/_Project/Scripts/PersistentConflictWarBattleStores.cs` while this
implementation is active. The owner may add only the private War snapshot
staging seam there. Conflict and Battle behavior and tests are outside this
handoff. Other tracks must not edit this shared file until this implementation
candidate is pushed. The dependency order is ArmedForce → Conflict → War →
Battle; War staging must use the exact staged ArmedForce and Conflict objects.

The owner may also add War-only detached snapshot code and focused War tests
and metadata, plus candidate-specific design and validation evidence. Runtime,
bootstrap, P12-B, other P12-E owners, P17-A, P16, global quiescence,
profile-wide persistence, and canonical promotion are outside this handoff.

**Release:** after the implementation candidate is committed and pushed, this
shared-file edit handoff is released to the integration owner. It confers no
canonical promotion authority.

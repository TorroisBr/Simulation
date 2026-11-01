# P12 Core owner-stage composition validation

## Candidate identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `bc8d024ce35d1cf8deb4ea455fc40b74692fd354`
- Candidate tree: `e97e87376afece57af5496eab9b6da56633d7098`
- Candidate base: `9bb0fd65d89f49995b08dddc5c28d96bf19607ec`
- Core canonical at validation/review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Bounded change

P12-D, P12-E, and P12-F image staging retain the exact Core owner-stage
objects used to hydrate the image. The detached-image restore path now asks
`DailyV1Application` to compose its active Core session from those C/D/E/F
stages, preserving their owner instances instead of selecting them again from
Unity compatibility packages.

The separate live capture-bound path retains its existing composition route
while it carries an in-flight staging attempt. The change preserves that
distinction. It does not deliver `DailyV1Application.TryStart`, complete P12
restore ownership, the detached .NET host, or full CORE-F acceptance. No Core
canonical promotion is claimed.

The follow-up preserves the existing exact-reference contract between the
admitted definitions and effective configuration. It forwards the host's
effective configuration to composition and adds a Core regression proving a
different configuration reference is rejected before stage access.

## Validation

| Gate | Result | Durable identity |
|---|---:|---|
| CoreValidation cold and warm | **988/988 PASS** each | Run `3eda2eb5427e455096f65bf4cdb5fd7c`; input digest `0e977f27709a12eabbe1e48b76faa00d99a5d6cb0f21bac9e62c545dcc420db6`; private manifest SHA-256 `565986eec2482f1fbee3b1607519089d6c1d39df8326a9d59cc90b1cdb920eca`; cold TRX SHA-256 `cf9f2c421dda4b15e4fbebdc3f9dbca9c9e14686f3b8c0161a3a00f4f9fe9361`; warm TRX SHA-256 `65f457cb73c8d5a33886c0bead48fb0bc350d88d53fbee8dc36c2030d1ed961a`. |
| Unity restore/admission class | **241/241 PASS** | Run `6a3b6704af124438aa1b11c5a03fc557`; input digest `3b49bdc0256f645d1f1f8fec60f5be3f36e406dad6516bac62d030d0be79fff2`; private manifest SHA-256 `abb78775275f6353472ab399611977d4d931dbf16feb0652a938e014195bea01`. |
| Full Unity EditMode | **3294/3294 PASS** | Run `5492d272c4da412989922741a22d40fc`; same input digest; private manifest SHA-256 `23ff08b47ed6a4ead434a5775b91111aaf4ddf40c82ed45348a892b2a853fb72`; original XML SHA-256 `3db51438d21ab8099558564443215603496bd57d8b65efc7f0ac384a3fff7023`. |
| Filtered Unity Smoke | **5/5 PASS** | EditMode filter `Smoke`; run `93cba10556f149a983e55fab50a600e5`; same input digest; private manifest SHA-256 `f7867330bf5296ebd5851642c8cd874ae4580428f500a32ddae6fcfc12bdd6c6`; original XML SHA-256 `7717efdaace9d94ce7174e7654cd777747896b3eba9e859bbb791ee2b0c4ca00`. |
| `git diff --check` | **PASS** | Checked before checkpoint commit. |

The source snapshot remained stable for every Unity run. Current committed
source hashes match the official input manifests. CoreValidation recorded
commit `8c990377f3122d2bd73c7b56fae5c58bfcce9e8e` plus a working-tree input
digest; the validated production and test inputs match candidate `bc8d024`.
The .NET worktree contained a Unity `Library`, so no Library-independent
claim is made. Original XML, TRX, raw logs, and private manifests remain in
protected local evidence storage; this record does not copy or rewrite them.

An attempted PlayMode filter selected zero cases and is excluded. The valid
Smoke gate is the five-case EditMode `Smoke` filter above.

## Preserved boundaries

- Existing owner identities, exact configuration identity, ordering,
  revisions, epochs, leases, and P12 admission semantics remain in force.
- No tests or assertions were weakened.
- Existing ProjectSettings changes, `TesteSimulacao.cs`, untracked files, and
  other protected evidence were not staged or modified by this checkpoint.
- The candidate branch is recoverable and synchronized remotely. It remains
  separate from Core canonical.

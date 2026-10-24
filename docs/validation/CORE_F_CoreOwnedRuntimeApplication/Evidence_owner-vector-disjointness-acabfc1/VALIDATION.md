# Restored owner-vector cross-graph identity validation

## Candidate identity

- Candidate commit: `acabfc1efaa8693355502392bfa6c82926be6e55`
- Candidate tree: `f1fbe020b6e341a125df1f594788cbb41be0b3e6`
- Parent candidate: `df56c18f112608348d8a11b930295ca9d58530bb`
- Core canonical used for applicability: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Change and scope

The Core restored-owner-vector contract now rejects any owner object that appears
in both the source graph and the restored target graph, even when it appears
under a different section identity. The existing exact section, role, schema,
cardinality, revision, empty/excluded, alias-partition, recapture-reference and
mutation-epoch checks remain in place.

The added regression uses distinct source owners A/B and target owners C/A.
It verifies that the cross-section reuse of A is rejected as
`ReusedSourceOwnerIdentity`. No persisted representation, public API, owner
semantics or gameplay behavior changed.

## Validation

All official Unity runs used Unity 6000.3.9f1 in the isolated E: validation
worktree. The P12-G, ALL EditMode and Smoke manifests report stable inputs and
the exact candidate commit and tree above.

| Gate | Result | Run |
|---|---:|---|
| Focused `P12GDailyV1OwnerVectorTests` | 16/16 PASS | `5039aaa2cb8344f99f9cd8bb396a8c53` |
| P12-G regression filter | 32/32 PASS | `414c7a1b37574eeebd876181c4e77991` |
| ALL EditMode | 3260/3260 PASS | `6b051a6994d24349a6760e5c4e607cd5` |
| Official Smoke filter | 5/5 PASS | `3c50d505c09c413899f1042d3c28ce1d` |
| CoreValidation cold | 704/704 PASS | `6cbd3d6bb7224e249a04b06a062a2b75` |
| CoreValidation warm | 704/704 PASS | `6cbd3d6bb7224e249a04b06a062a2b75` |
| `git diff --check` | PASS | candidate commit |

The focused owner-vector run preceded the candidate commit. It used base
`df56c18f112608348d8a11b930295ca9d58530bb`; the hashes of both changed
source files matched the subsequent `acabfc1e` commit. Its input digest
was `d151a4aa97492d79f1a029bcb6942283eb54dd7d66d6d5576576a3212ef2907e`.
The focused run recorded source SHA-256
`fe6731ae409d62a57b19ee8a99912d9162aa15b3b7a7880184d9cfee1b5156e3`
and test SHA-256
`8ad4afadd83e1ec25a3e57d203ad8c92ff79450db51a0bef960f150321e3738f`.
The exact-commit P12-G and full EditMode runs then repeated the changed
regression and share input digest
`bfb5472a7bf1da04f57fadbb98a4cc997c7cda0fbceff3859a44a28bcaabcc3e`.

Private Unity run-manifest SHA-256 values, in table order, are:
`fc2a026b276baec27b1e069272916252073c1a0930eab6f886c23f6f3ed25142`,
`5307954a952938c936506d3de975cc83506aa9b10b84043a712339b7543be659`,
`24a7b4f3cccf77561a8dff332bef13559d203b2fa25432d4df99bfe8c9c9a1fe`,
and
`560789a77edd88ac113f12a8afff7ad7e52fe898169df0d853983f7858f581a6`.

The CoreValidation run records the changed Core source in its stable input
manifest (SHA-256
`fe6731ae409d62a57b19ee8a99912d9162aa15b3b7a7880184d9cfee1b5156e3`).
Its protected manifest SHA-256 is
`5b97460323bf334f2f1feadfee546cef3897948e0e31cdcc5870d0599141447e`.
Its input digest is
`a36eb3f4793efbaef7d2adaf0e6e611641f7a1ad86b59a7384e1bef813a3ae46`.
CoreValidation ran in a worktree containing a Unity Library; no Library-free
.NET environment claim is made.
The original CoreValidation reports and manifest remain protected and
unmodified. No public DotNetTrx package is claimed: the retained runner
manifest is not the strict TRX source-context format accepted by the current
packager, and the reports were not altered to fit it.

The full EditMode report was packaged and verified with the official evidence
packager. The verified archive SHA-256 is
`010720e5bf33c571e8d8bcd4fc51088e9659c2e3cb7c7717be37f0dfff31a871`.
The package manifest SHA-256 is
`e2aeb32c2448b4aa6ab92287dc09068859ac824e8b77fce334124a41bda4de65`.
The full EditMode report contains the Smoke cases. The filtered reports and
original CoreValidation reports remain in protected E: evidence storage; their
original bytes were not changed.

## Limits

This candidate repairs only detached owner-instance separation. It does not
complete P12-G, the shared runtime/restore package, or CORE-F, and does not
claim canonical promotion.

# CORE-F WP1-Values validation

## Intermediate integration evidence, not a promotion verdict

Source `dad86c6cdcaac43321fd0b27d6763bc2b103bb0b` was independently reviewed
against promoted Core `8fc975631db0d4e53fa58c490c752fb79d209b0e` and approved
architecture `4da844dee0c69fdf66562590fa96cc728fc86bf7`. Review found a remaining
Unity typed-adapter field-by-field parity test gap. This package is historical
intermediate evidence; it is not full WP1 closure or full CORE-F readiness.

Core cold/warm run `a6239afd2d4f42148aa497fdd775eceb` passed169/169 in each
attempt using the pinned SDK and selected CLR, with129 explicit input files,
locked dependencies and no Library. The complete normalized TRX package is
`core-f-values-cold-warm.manifest.json` and matching ZIP; archive SHA-256
`e8047074cbdf5e6790d7d0a2c2dbcf8878c6ceec561c1277c56232e1704c7f62`.
Protected original comparison passed. This is distinct from archive-only
verification. Originals and matched validation binaries remain protected and
unchanged; private paths/binaries are not published.

Unity focused characterization/budget run `49505501fe5442e59de883248a5c407e`
passed3/3 from the same source. Broader configuration/bootstrap/genesis/P10/P12/
restore-focused run `b3e322a786de4fdc9825d79775bed5e1` passed. These runs retain
original case/source identities in protected storage; final full gates and
complete Unity report package must follow the additional typed adapter proof.

Preceding diagnostic runs caught private nested-member/cross-assembly constant
access and a test-helper mistake decoding plain hash values as base64. Those
were corrected before these passing runs; no production parity mismatch was
masked. Earlier diagnostic runs are not final completion evidence.

The fixed C# constructor fixture regenerated identically from the independent
718 member captures and164-row source map. Normalized generated-source SHA-256:
`0f7711d29f323c06ee09b759a9d003b783dfd581ab6c08dece7cf89132a7d4b0`.
Expected P12 bytes are output vectors only; generator input is not the encoded
catalog. Baseline Unity fixture bytes remain unchanged at SHA-256
`422805cbe373cf1410ee6a928d363fa1ef413dbcf50dc062dce7867ed4fdd1b3`.

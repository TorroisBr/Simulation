# P12-D Genealogy owner snapshot validation

## Candidate and boundary

- P12 canonical base: `0e786db8e6ed5ed937ff62e3f63258d8b73fd93c`.
- Current architecture baseline: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Reviewed D/E current-base design: `0735103009b0161b3175349e9c78db241913de74`.
- Code candidate: `2b3de7cd13e6f35d35d5dece56eefb64c1fd972a`.
- Code candidate `Assets` tree: `127bc9dc10c0e69312ae98748f04becbe2d8ceda`.
- Code candidate full tree: `a2fe8033298b96a68b8e209cce6d605dbbe6ca44`.
- Unity: `6000.3.9f1`.

The slice is limited to the immutable schema-v1 Genealogy owner snapshot and
private exact-value factory. It preserves the local revision, including gaps
and saturation, rebuilds only Genealogy's local indexes, and validates local
edge uniqueness and acyclicity. It does not add profile registration, capture
token issuance, Person membership validation, City/NPC/runtime/bootstrap
integration, P12-A readiness, or whole P12-D completion.

## Results

| Gate | Result | Evidence |
|---|---:|---|
| `GenealogyFoundationTests` focused | 24/24 PASS | `Focused/EditMode-20261008-123729-32459300227142239c608459602f9ddf.xml` |
| All tests matching `Genealogy` | 52/52 PASS | `Focused/EditMode-20261008-123801-f2a76ce084d645619475524c985aa2cd.xml` |
| `PersonNamedBirthLifecycleTests` regression | 15/15 PASS | `Focused/EditMode-20261008-123817-d0500c24c3bd48c9a2d6a97752607752.xml` |
| ALL EditMode | 2540/2540 PASS | `Full/EditMode-20261008-123838-833c1829be11455da492877ae6f29d63.xml` |
| Official Smoke | 5/5 PASS | `Smoke/EditMode-20261008-123915-eee7f4edf7f4408790c8adaa3ca09fd3.xml` |
| `git diff --check` | PASS | Exact code candidate `2b3de7c` |

The first focused attempt ran 23/24 because the existing pure-core source test
rejects a forbidden owner name even in a comment. That comment was corrected;
the exact committed candidate then passed all focused and promotion-level
validation gates above. Its original XML is retained alongside the successful
run artifacts for diagnostic traceability.

## Artifact hashes

SHA-256 hashes are recorded for each raw test XML/log. The ALL EditMode log is
stored losslessly as gzip to keep the repository artifact below GitHub's file
size limit; decompressing it reproduces raw log SHA-256
`8D085F028E63E828E88A16A2759D478CC213C6EA95879A4354D60287FF2993D1`.

| Artifact | SHA-256 |
|---|---|
| `Focused/EditMode-20261008-123517-3351539422134f08a2fad63f6c99f692.xml` | `027DCE436545BA7DD63D566F18ED83E7020FF3AB0B7CC7393E7578E9475A21C7` |
| `Focused/EditMode-20261008-123517-3351539422134f08a2fad63f6c99f692.log` | `E4AB6C9677053E38B4FF66D6E3DE94B1A84616E7A09977C8211443D5930DA6FE` |
| `Focused/EditMode-20261008-123729-32459300227142239c608459602f9ddf.xml` | `2289E6842127149DB96E8F1F374F101F25F2EA2A00B5D31BEA468C57FD6B30BC` |
| `Focused/EditMode-20261008-123729-32459300227142239c608459602f9ddf.log` | `0D3976D95486CFA2448CC12F090A68FC5AB2AFF9BA87BE917736EDF4F005DA93` |
| `Focused/EditMode-20261008-123801-f2a76ce084d645619475524c985aa2cd.xml` | `02C99CA58B9D3054FA73722ED3506EF1B22D382796EA091453BC6BBD742BDA1B` |
| `Focused/EditMode-20261008-123801-f2a76ce084d645619475524c985aa2cd.log` | `6CB4DE31E7F682DF048801ACC9A42D7F77F0D196EF492CE775CFDE7D2AEF66A1` |
| `Focused/EditMode-20261008-123817-d0500c24c3bd48c9a2d6a97752607752.xml` | `68592ADB7BCD2953466F7B46FC4853C79F15E591B7FF56C433FE0907C942767B` |
| `Focused/EditMode-20261008-123817-d0500c24c3bd48c9a2d6a97752607752.log` | `C761F0426DB6796CDAEC9FD11AE7F3AB7C0DFEC526FC11095AAD81CFD4323A8D` |
| `Full/EditMode-20261008-123838-833c1829be11455da492877ae6f29d63.xml` | `906881CB7690D2B8E0292118E5D0F03E72B19BC1A9E6F6AA77BBDE772AC19254` |
| `Full/EditMode-20261008-123838-833c1829be11455da492877ae6f29d63.log.gz` | `0DBCE544EA2A89B954E0CB1573B00F3F6259CB046F1B8EBC2C02F7902FE52161` |
| `Smoke/EditMode-20261008-123915-eee7f4edf7f4408790c8adaa3ca09fd3.xml` | `848A9553DF3469B7FA4E06F733EB63B4C2C2B316275AB5852EB8BDAD80EE7D6C` |
| `Smoke/EditMode-20261008-123915-eee7f4edf7f4408790c8adaa3ca09fd3.log` | `C7DA5F02B630597D18DA79C4F486442F69CE1E5A99FBB703B1FC79393FE7C0EE` |

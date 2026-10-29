# CORE-F P12-E ArmedForce Spatial Core — Independent Exact-Tip Review

**Verdict: PASS — VALIDATED_CANDIDATE.** This review validates the bounded source-migration candidate and its exact-tip evidence. It does not promote Core canonical or claim CORE-F/P12 completion.

## Exact identity

- Repository: TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Review tip before this record: 7e7e8018f6bb2448411d83344af9398c267a3bb0
- Code commit: fc9eb2419185a90b14eb46eb8deb58124ad6291a
- Code tree: 4e882533a0190f719348f256027bd8beff3e0d3a
- Code parent / diff base: fb2a127b237a5b4b3f675f02a97999423a9b6c69
- Current Core canonical during review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Remote preflight: candidate branch points to the review tip; Core canonical remains at the stated SHA. The code commit has the stated single parent and tree; the evidence commit is its direct child.
- Review scope: the complete fb2a127..fc9eb241 source/test delta and the 7e7e8018 validation/evidence addition.

## Implementation review

The source delta is limited to the ArmedForce spatial owner and P12-E military census/codec/snapshot moving into Simulation.Core, explicit Core compile links, a host projection seam, Unity runtime/diagnostic forwarding adjustments, and affected tests.

- **Spatial authority:** Core resolves P8 Hex, Location, and Crossing references through SpatialAuthorityStore.TryResolveReference. Only SubLocation references use IArmedForceSubLocationResolver; the Unity adapter resolves the P10 topology owner/place and validates its canonical P8 Location binding through the existing spatial authority. No P8 identity, binding, revision, or mutation ownership moved into the adapter.
- **P12 LocalTopology boundary:** capture rejects any installed SubLocation resolver, matching the prior rejection of an installed LocalTopology store. Staging rejects a composed topology flag, and detached P10/SubLocation references fail P8-only staging. The selected Daily-v1 caller supplies the existing not-composed state; the P12-E contract excludes LocalTopology. No P10 state is admitted by this change.
- **Owner and capture semantics:** owner identities, section IDs/cardinality, census providers, codecs, local revisions, mutation guards, exact token/vector checks, and staged publication ordering are unchanged apart from replacing the concrete Unity topology dependency with the resolver boundary and not-composed fact. The P12-E snapshot diff does not duplicate an owner or alter the five existing military sections.
- **Unity identity:** all four moved source metadata files retain byte-identical contents and GUIDs at their new Core paths.
- **Build boundary:** Simulation.Core.csproj adds explicit links for the moved production sources. The linked Core implementation contains no Unity authoring dependency. Unity keeps the P10 adapter and passes it through the new seam.
- **Tests:** edits adapt the constructors and not-composed assertion without weakening assertions. Existing tests still exercise SubLocation resolution and reject capture/staging with LocalTopology; the full exact-source run passes.

No actionable implementation finding remains.

## Validation and artifact review

The exact-tip VALIDATION.md and manifests bind the .NET run to code commit/tree fc9eb241/4e882533 and the Unity run to its complete 1,965-file input snapshot. The record says all 1,965 inputs were rechecked against the candidate with zero mismatch, including the three intentionally uncommitted ProjectSettings inputs by hash. I independently checked the snapshot entries for the four changed spatial/snapshot/test files listed in this review; their recorded hashes match the candidate file bytes after the recorded Windows CRLF representation.

- Official CoreValidation: 922/922 cold and 922/922 warm, zero failed/error/timeout/inconclusive; cold and warm CalendarDemo passed. Lab gates were omitted via the documented -SkipLab option. The test source identity is the exact code commit/tree.
- Unity 6000.3.9f1, Test Framework 1.6.0: 3,279/3,279 EditMode, zero failures/skips/inconclusive; Smoke 5/5 is included in the full run. The exact run and result totals are present in the Unity manifest. I did not rerun unchanged tests.
- Archive verification: independently computed SHA-256 matches the declared .NET ZIP (438,405 bytes, 1f3ff9a4…fa13fa) and Unity ZIP (173,746 bytes, d8bdd62c…cfe287). Their ZIP central directories contain only the declared relative result XML entries. Decompressed reports contain no absolute workstation-path patterns; the .NET TRX retains test stdout values but projects its worktree reference to <WORKTREE>.
- The manifests contain no absolute workstation paths. The listed manifest SHA values correspond to CRLF working-copy bytes; CRLF restoration reproduces each declared value, while GitHub stores the same manifest content with LF-normalized blob bytes.
- The validation record reports git diff --check PASS. The remote code delta has no ProjectSettings changes. No candidate or user-owned worktree files were modified by this review.

The local E: checkout was not readable to this review process, so the exact immutable remote commit contents and refs were used. The review is limited to committed code/evidence; uncommitted ProjectSettings changes are outside its scope.

## Scope limits

This is a validated CORE-F candidate checkpoint only. It does not promote Core canonical; complete P12/CORE-F; change P12-A scope; admit LocalTopology to Daily-v1; establish new P12-B coverage, capture eligibility, or P13 readiness; or authorize phase closure.

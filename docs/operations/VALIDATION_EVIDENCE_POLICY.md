# Validation evidence and Git retention policy

**Approved responsibility:** operational process; subordinate to semantic architecture.
**Prepared:** 2026-10-11. **Scope:** prospective validation/review/publication work.
Architecture investigation base: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
Representative P12 base: `ab7c4b4e8347546c6bd8f4353d98353176b99807`.
This policy changes evidence packaging and reuse, not Phase objectives, test
requirements, determinism, continuation, profile membership or delivery status.
The General Orchestrator remains free to continue its authorized work.

## 1. Assessment and evidence inspected

Current architecture, Roadmap, Execution Model, root/parent AGENTS, all eight
project Skills, current P12 Brief/State/designs, validation harness/module,
Git refs/worktrees, and current candidate evidence were inspected. P12 B–F
are promoted within scope; G/A remain WAIT_DEPENDENCY and P13 BLOCKED at the
representative source. This policy supplies none of their missing capabilities.

At that exact P12 tree, `docs/validation` contains:

| Class | Files | Logical bytes in the checkout |
|---|---:|---:|
| XML reports | 634 | 338,003,087 |
| ZIP/GZIP archives | 456 | 515,846,909 |
| Other records | 124 | 510,806 |
| Total | 1,214 | 854,360,802 |

P12-G paths account for 478 files / 461,630,124 bytes. These are current-tree
uncompressed blob sizes, not Git pack size, network transfer or disk use across
all worktrees. The repository had 218 worktrees, 365 local branches and 684
origin tracking refs. Counts do not imply that any particular ref/worktree is
safe to delete. No identical-content duplicate blobs were found among the
current validation paths; redundancy includes repeated near-identical output,
overlapping runs and diagnostics, rather than demonstrated byte-identical copies.

The reported 21-file case is local branch
`codex/phase12/P12GSuccessfulDirectiveTravelPartyParity`,
`15bba2ee4133d58f01c30299c9092c91f924afed`, based on the P12 base above.
Its delta contains one test source, two reviews, two validation records, eight
XMLs and eight compressed logs: 20,631,960 logical bytes. It preserves runs for
two successive code trees. The final cumulative gate reports 1 focused case,
194 admission cases, 2,868 ALL EditMode cases and 5 Smoke cases, all passed.
Earlier-tree validation cannot substitute for that final-tree validation.

The candidate review records an automatic publication rejection for exporting
project-derived reports/logs without explicit authorization for that payload.
The current `Orchestrate Main` conversation confirms the rejection and a later
user authorization for that exact 21-file payload. This investigation neither
publishes nor repackages that active candidate and grants no extra authorization
for it. External approval depends on actual payload/destination and tool policy,
not on repository prose.

XML already embeds runtime stdout: one final ALL report contains about
1.32 MB of output text inside its 2.91 MB file. Compressing logs, renaming reports,
or putting everything in an archive does not remove that publication content.

The current harness correctly treats parseable result XML as run evidence,
not process exit or discovered assembly counts; it preserves operational
failure/timeout paths. Existing contracts require exact-tree evidence and
independent review, but do not define a uniform permanent evidence package.
Practice varies between raw Git copies, archives and transient local paths.
The architecture checkout also retained older routine-approval wording;
the already accepted P12 autonomous bounded-promotion rules are reconciled
into the common workflow without dropping genuine human gates.

## 2. Required permanent evidence

Keep the following retrievable through canonical Git history for every promotion:

1. **Source and validation-input identity:** full base, tested code commit and
   root tree, final reviewed/promotion content refs, plus an explicit complete
   input inventory/digest. Include source/tests/assembly definitions and
   metadata, Packages manifest/lock, effective ProjectSettings/config/fixtures,
   validation tools, scripting defines/target and any external/generated input
   actually used. Assets tree alone is insufficient when other inputs differ.
   Record dirty/untracked influencing inputs explicitly; otherwise validation
   cannot establish that a later commit represents the tested tree.
2. **Execution record:** run ID/time, exact Unity/framework and harness version
   or blob, host/numeric/build profile, mode, exact filter/category/arguments
   using portable path placeholders, selected required gates and results.
   Record operational retries, failures, skips/inconclusive cases and dispositions.
3. **Actual result evidence:** complete per-case result records for each final
   required gate, with root/suite hierarchy, selection, identities/full names,
   outcomes/labels/reasons/assertion and failure data, counts and available
   durations/framework metadata. Retain run seeds and relevant properties.
   Assembly/discovery totals are not executed-case evidence. Never substitute
   a green aggregate, hash, log claim or subset for the complete selected run.
4. **Manifest:** schema/packer identity, archive and entry paths, byte lengths,
   SHA-256 of retained bytes and original XML bytes, extraction/omission
   declaration and any protected diagnostic locator/retention commitment.
5. **Independent review:** reviewer identity, independence, full reviewed
   base/tip and diff scope, validation-input identity, inspected result hashes,
   raw-to-retained comparison method/verdict, findings and resolved limitations.
6. **Promotion provenance:** owning State links to the reviewed content,
   validation package and previous/final canonical refs, scope and remaining
   gates. Use normal State update; do not create a parallel delivery authority.

A permanent manifest pointing only to deleted Temp files is insufficient.
A provenance hash of an expired original is not proof of the original bytes.
The retained complete result reports are the accessible historical result
evidence; the source hash binds their origin, and independent comparison
establishes their decision-bearing equivalence. These prove recorded execution
results, not cryptographic attestation that a trustworthy machine ran the tests.
Reproduction needs the identified source/inputs/toolchain, not the old log.

## 3. Smallest permanent package

Default for NEW final validation packages after the packer gate in §9:

- one concise validation manifest/record;
- one result-only ZIP containing final required gate reports, in ordinal order;
- one cumulative independent review record, with per-slice closure findings;
- normal source/tests and owning State evidence.

A small self-contained slice may put its validation manifest in the review
record and link the same package. Reuse an existing package by hash/ref rather
than copying it into a new directory. Git supplies versioned accessibility;
ZIP is ordinary packaging, not a new storage service. Never commit raw and
compressed copies of the same report or the same diagnostic in several bundles.

Result-only reports preserve the original result XML hierarchy and all
decision-bearing fields listed in §2. They may remove arbitrary stdout/stderr,
environment dumps and local command/machine paths only when those fields are
not needed by the claim. Manifest records removed field categories. Case
identity, outcome, skip/failure reason, selection, count, seed or relevant
diagnostic evidence must not be lost. Path-like stack traces or failure reasons
may need reviewed redaction; preserve the failure meaning, identity and needed
claim evidence, with protected originals where required and permitted. Unknown result-bearing nodes/schema
changes fail packaging closed until supported; no silent successful extraction.

Do not rewrite Passed/Failed/Skipped/Ignored/Inconclusive states, omit failed
attempts to manufacture a passing run, or infer outcomes from prose. Original
XML remains the authority for determining whether the run passed. Validate
the actual run using the existing harness and Phase gate first; independent
review then verifies every retained case/result and suite/root count against
the original. Declared dispositions for permitted skips remain required;
Phase-specific zero-skip gates are unchanged. A valid failed run stays failed.

Retain original XML and raw logs outside Git in an owned, access-controlled
`.validation-evidence/<run-id>/original` directory (or an equivalent explicitly
recorded protected diagnostic location) before Temp cleanup/worktree removal.
Default diagnostic retention covers review/promotion and up to 30 days after
promotion, subject to applicable privacy/security requirements. This is neither
a minimum security-retention mandate nor authority to keep sensitive data beyond
a binding limit. Unresolved failures and necessary diagnostic claims may justify
longer protected retention where permitted; record the reason and disposition.
If a binding rule forbids retaining originals, independently compare them before
permitted discard and retain the minimum safe complete decision evidence. Never
publish originals to satisfy this rule. If required claim evidence cannot safely
be preserved, do not claim that evidence gate passed. No automated deletion is
introduced by this policy.

Keep full originals/selected diagnostics permanently when a contract or
specific claim needs them (for example a measured long-run metric present only
in output). Retain reviewed safe excerpts or an appropriately protected durable
artifact with integrity, access and recovery guarantees. Such external storage
is optional; without a qualifying store, retain safe necessary evidence in Git,
or leave the affected publication/claim blocked. A local-only vault is not the
sole permanent promotion evidence. Never publish sensitive data merely to make
it durable, and do not erase material claim evidence during normalization.

**Before a validated packer exists:** continue independent inspection of original
XML. Safe final original XML may be retained as one XML-only bundle, with a
manifest naming every entry/hash, under normal publication checks. Keep a
specifically required existing raw package when that is its reviewed contract.
No manifest-only migration or hand-edited summary may replace missing results.
Removing logs from Git is already permissible for new packages whose claims
do not depend on those logs and whose original results remain accessible.
This transitional form still contains XML output; assess it accordingly.

## 4. Artifact and publication boundaries

| Content | Normal destination and treatment |
|---|---|
| Source code, tests, fixtures | Git; ordinary exact-diff review and permitted push. Fixtures remain subject to data review. |
| Human-authored review/State records | Git; concise factual attestation, no copied runtime dumps or secrets. |
| Validation manifest | Git; portable invocation/identities/hashes/results; assess actual content, not filename. |
| Machine result-only reports | One reviewed permanent Git bundle, or a qualifying durable store; generated evidence remains generated data. |
| Raw XML/logs/exploratory output | Local diagnostic retention by default; selective permanent retention when needed. |
| Sensitive runtime output | No routine public push; minimize/redact nonessential data, preserve required facts in protected evidence, respect external restrictions. |

Before any push, examine the actual outgoing object closure, not just HEAD's
diff. Adding a deletion commit above a raw-log commit still publishes that
ancestor. Inspect/decompress generated archives and review output categories,
paths, parameters and diagnostic messages for secrets/unrelated private data.
Regex scanning alone does not establish safety. Manifest and review include
the retained artifact boundary; compression is not redaction or authorization.

Use the ordinary approved destination/route. Do not use a different remote,
API, branch label or transport to evade a security rejection. If an external
review blocks the exact safe payload, report the action/reason and obtain the
specific authorization required by that review. This policy cannot eliminate
all external approval requests and introduces no routine conversational gate.
Payload minimization and explicit evidence classes reduce avoidable requests.

## 5. Storage alternatives

| Option | Durability / verification | Decision |
|---|---|---|
| Compact Git result package | Retrieved at fixed commit; ordinary clones/history; independently inspectable; small metadata/output boundary. | Default; uses existing infrastructure. |
| Every raw XML plus compressed logs in Git | Durable but large, overlapping diagnostics and higher disclosure surface. | Selective exception for necessary claims. |
| Manifest/hashes alone | No independent historical result inspection after underlying evidence disappears. | Insufficient. |
| Local off-Git vault | Useful diagnostics and original comparison; machine/worktree loss and inaccessible-to-reviewer risk. | Temporary retention, not sole permanent evidence. |
| GitHub Actions artifacts | Useful handoff/cache; expiration/deletion/access limits. | Not sole permanent promotion evidence. |
| Release-associated bundle | Can preserve milestone evidence, with stable asset/digest and access/backup policy; a release is an additional lifecycle to manage. | Optional later consolidation, no per-slice release requirement. |
| Dedicated external artifact store / LFS | Requires credentials, accessibility, retention/recovery and management; LFS does not remove content/security review. | No new service required now. |

GitHub documents default 90-day workflow artifact retention; artifacts can
expire or be removed. Thus an Actions URL plus hash is not permanent retention.
See [artifact retention](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/remove-workflow-artifacts).
Where enabled, immutable releases protect attached assets against replacement
during that release's lifetime; whole-release/repository lifecycle and access
still matter. See [immutable releases](https://docs.github.com/en/code-security/concepts/supply-chain-security/immutable-releases).
No assumption is made about this repository's release/Actions settings.

## 6. Reuse without reducing validation

Validate early with focused tests; batch related already authorized bounded
changes into one final integration window when ownership allows. Run ALL,
official Smoke and required LongRun on that FINAL cumulative input tree before
promotion. A separate promoted code change still needs its own required gates.
Do not split two ready fixtures into independent full runs solely because they
have different working labels. Do not combine unresolved scopes or delay
unrelated work just to batch. Full suites are never replaced by focused tests.

Reuse existing gate results only with recorded equality of the complete
validation-input identity, harness/Unity/profile, selected test scope, retained
evidence and relevant dependency/architecture assumptions. Record the source
run/package and applicability; no new archive, invented execution date or
claim of a rerun. Changes outside that inventory must be inspected, not assumed
irrelevant. A test-source change is an input change even if runtime is unchanged.

Any executable/source/config/test/toolchain difference requires the affected
focused tests, mandatory promotion suites and fresh independent review on the
integrated target; scope-specific LongRun/demonstration gates remain intact.
Do not reuse an old ALL run after adding another test to the assembly.
For pure docs/review/State/packaging successors with proven input equality,
reuse run results and perform independent exact-tip delta applicability review.
Missing/unverifiable evidence or changed contract requires resolving the gap.

Independent review still covers the full candidate diff against its real base,
with cumulative per-slice findings and exact proposed final tip. The same
independent reviewer may verify a docs-only successor with a concise delta
check referencing the prior full review and unchanged input identity. A second
full implementation review/Unity rerun is not required solely for its own
review record or a routine promotion State entry. The final proposed tip must
still receive independent applicability verification; no author self-approval.

Avoid self-referential SHA/commit loops: review records name the fixed reviewed
content/code ref and evidence digests; an attestation of final metadata-tip
applicability can be retained in the normal durable orchestration/promotion
record, outside that already-reviewed tree. Do not create an extra commit,
reviewer, branch or worktree solely to restate the same unchanged proof.

## 7. Git lifecycle and historical migration

Historical promoted records, XMLs, archives and refs retain their meaning and
accessibility. No rewriting history, deleting promoted artifacts, force-push,
purging remote objects, phase reopening or retroactive missing-evidence PASS.
This policy slows new growth; it does not shrink existing clone history.
Do not run repository GC/prune or recursive checkout deletion during this review.

Reuse a clean suitable idle worktree. Create another only for actual concurrent
writers/incompatible checkout needs. Review can inspect Git objects read-only.
When a workstream ends, ordinary authorized cleanup may remove its disposable
worktree ONLY after proving ownership, no active work, all commits and required
untracked evidence safely retained, and needed ignored diagnostics safely retained
where permitted under section 3.
Use the managed archive workflow for managed worktrees. Never remove another
thread's checkout or discard ProjectSettings/.meta/user changes.

Branch names are navigation, not sole evidence. State/canonical ancestry and
review refs keep content recoverable. Remote deletion is separately evaluated;
no bulk deletion or new authorization for important unintegrated refs. Local
cleanup must not remove the last ref to needed candidate/review content.
Normal Git compression/GC is maintenance, not a substitute for retention.

In-flight candidates keep their existing accepted package/push authorization.
Master may adopt the prospective package before any relevant raw-object push
if it is safe, preserves complete evidence and independent applicability.
Do not edit active candidates in this architecture stream. If an unpublished
ancestry already contains raw artifacts, a separate additive presentation
branch from current canonical may carry the same reviewed code plus approved
minimal evidence; preserve the original branch and review provenance. Verify
complete input equality, exact new tip/diff, gate applicability and security.
This is ordinary reconstruction of a candidate, not silent shared-history
rewriting. Do not rerun unchanged input trees merely to change packaging.
Existing canonical raw artifacts remain in outgoing ancestry when actually
needed by a new clone; evaluate that fact, not just the reduced new delta.

## 8. Existing versus proposed workflow

| Step | Representative current practice | Prospective policy |
|---|---|---|
| Work | successive bounded commits and suites | early focused runs, one planned final cumulative gate set |
| Evidence | 2 packages, 8 XMLs + 8 logs for one final promotion | 1 manifest + 1 final result bundle; prior meaningful evidence referenced |
| Review | 2 full records plus metadata iterations | one cumulative final review; short exact-tip applicability check for metadata only |
| Publication | all project output carried by candidate ancestry | required result evidence only; actual outgoing closure reviewed |
| State | repeated copies of test counts/hashes | concise scope/promotion record linking the same package and review |
| Worktrees | candidates/reviews/validation often get separate checkouts | reuse clean idle checkout; independent reads need no new checkout |

The 21-file example could prospectively publish 1 test source, 1 manifest,
1 cumulative review and 1 result bundle before its normal State update.
This is an illustration, not a four-file limit or rewrite of that live candidate.
Both test cases remain in the final assembly and receive independent review.

An in-memory sizing exercise, not a validated production extractor, removed
only output/environment/command-line nodes from the four final XMLs and
preserved all test-case attributes/counts. GZIP totaled 164,410 bytes versus
20,631,960 bytes for the existing two-package payload. A ZIP/manifest adds
overhead; this estimates the opportunity, not a measured published bundle or
guaranteed reduction. Even all eight ORIGINAL XMLs alone GZIP to 382,174 bytes.
Compression and removal of duplicate diagnostics matter more than dropping
required tests. Original XML privacy still requires the publication checks.

## 9. Bounded Master handoff

**Implementation readiness:** policy adoption/documentation can proceed after
independent PASS; a small standalone result packer requires the following
bounded tool implementation/review. This is not a numbered Phase or a
dependency on P12-G correctness. P12-G and other authorized work continue.

Owner: General Orchestrator, isolated Tools/UnityValidation/process window.
Base: current canonical at execution; reconcile this policy with current
P12 AGENTS/Skills/Execution Model by content, preserving their existing
autonomous bounded promotion and genuine gates. Do not merge the old executable
tree from the architecture branch into a numbered-phase branch.
Affected files: a small export/verify helper under Tools/UnityValidation,
its PowerShell self-tests/README, and only necessary invocation metadata hooks.
No SimulationRuntime/domain changes, new storage service, CI platform,
generic registry, security bypass or automatic deletion.

Closure contract:

1. Capture the complete validation-input manifest and invocation facts; copy
   originals to protected diagnostic retention before Temp cleanup where
   permitted under section 3. If raw retention is prohibited, independently
   compare before permitted discard and preserve minimum safe complete evidence;
   required evidence that cannot be safely retained cannot earn a passed gate.
2. Produce one manifest and deterministic result-only ZIP (stable entry names,
   ordinal order, fixed archive metadata/encoding, pinned packer runtime/toolchain).
   The deterministic-byte promise applies within that identified toolchain; a
   different compression implementation requires a newly identified bundle. Retain original XML hashes
   and lengths plus normalized entry and archive hashes. No arbitrary logs.
3. Independently verify original XML against retained reports: exact case
   multiset/hierarchy/outcomes/labels/reasons/counts/required fields, selection
   and original gate result. Reject truncated XML, DTD/entity inputs, count/
   case mismatches, missing required fields, unknown result semantics,
   duplicate run entries, hidden skips and path traversal/zip-slip.
4. Provide ordinary local verification/read commands. Publication review sees
   archive contents and all outgoing objects; output stripping is explicit,
   and diagnostic-dependent claims retain their necessary safe evidence.
5. Self-tests cover passed/failed/skipped/inconclusive, parameterized duplicate
   display names, partial/malformed XML, locale durations, missing reports,
   output containing secrets/paths, claim-required output, bundle tampering,
   deterministic bytes and archive safety. Use synthetic fixtures; do not
   commit real raw project dumps to prove this tool.
6. Independently compare a real existing P12 final run in read-only mode;
   confirm preserved counts/cases/hashes with no publication of its raw output.
   Do not rerun Unity solely to verify packaging. Existing harness result
   authority, retry/timeout/process-ownership behavior remains unchanged.
7. Review exact tool tip/diff, run Tools self-tests and diff-check, then adopt.
   If executable harness behavior is changed, run its affected regression
   gate and actual Unity validation needed for that change; do not claim a
   docs-only exemption. Validate later phase candidates under unchanged gates.

Until this closes, use §3's original-XML transition path; do not advertise
normalized packaging as implemented. The new rule does not exempt a currently
binding Phase contract that explicitly requires particular raw diagnostics.

After promotion, record tool capability and package locator, then use it for
new authorized candidates. Lifecycle cleanup is separately bounded; this
handoff authorizes no bulk worktree/ref/history deletion. No product or
simulation architecture choice remains. Only a genuine external security
restriction or incompatible accepted contract can require user input.

## 10. Current packager and Core migration host evidence — 2026-10-13

The §9 Tools handoff is delivered on inspected P12 canonical
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d`; its production README and independent
review define supported behavior. Earlier future wording is historical. Existing
NUnit normalization/path redaction and Assets/Packages/ProjectSettings/UnityValidation
input roots remain bounded; this refresh does not extend the implemented parser.

For CORE-A, use the exact [host evidence contract](../architecture/SIMULATION_CORE_A_MASTER_HANDOFF.md):
complete .NET TRX result evidence in one reviewed durable bundle (originals, or
the implemented and independently validated projection in section 11), full SDK/CLR/
project/lock/source/runner input manifest, before/after equality and nonempty
complete per-case execution. Preserve failures/skips/counts/identity/reasons and
protected diagnostics. Review actual report content and outgoing objects before
publication. Do not feed unsupported TRX into the Unity packer, edit it to fit,
or replace inaccessible results with hashes. Unity uses its delivered packer plus
a sidecar identity manifest for new inputs beyond its roots. The normalized
.NET adapter in section 11 still needs implementation and independent schema/
result-equivalence review before use. Existing mandatory
focused/full/Smoke/LongRun gates and historical packages remain unchanged.

## 11. Privacy-safe .NET TRX evidence — approved contract, tool not yet delivered

Use [the exact TRX contract and Master handoff](DOTNET_TRX_EVIDENCE_CONTRACT.md).
After its independent design PASS and architecture promotion, a complete normalized
TRX package may satisfy CORE-A's durable result-evidence gate only once the shared
Evidence Packager's bounded TRX adapter is implemented, validated and independently
reviewed. The original-only public TRX clause is replaced for this supported path;
no existing sanitized artifact is retrospectively accepted and no execution gate
is weakened. Until that capability passes, the current evidence block remains.

Originals remain immutable and access-controlled under diagnostic retention;
complete normalized reports plus source/input/assembly/projection identities and
independent comparison are permanent accessible evidence. Local-only originals,
hashes or JSON outcome summaries are not sole permanent promotion evidence.
Archive-only verification checks retained integrity/consistency, not raw-source
equivalence or execution authenticity. Independent original comparison must be
completed and attested before promotion; no stronger claim without raw access.

Only the exact field projection is permitted; preserve all outcomes/case identities,
native counters, derived complete outcome accounting, errors, times and cold/warm
provenance. Unsupported structures/private immutable payload fail closed. Existing
NUnit behavior and mandatory validation remain. Actual CORE-A local ACLs were not
isolated as previously claimed; secure-copy/hash/access remediation is required,
without editing originals or active implementation worktrees. This finding is not
a finding of disclosure. Retained original SDK outputs permit reviewed retrospective
assembly binding; label recovery honestly, never invent capture-time hashes.

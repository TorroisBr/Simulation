# CORE-F Runtime-Boundary Publication Gate — Independent Review

## Verdict

**PASS — VALIDATED_CANDIDATE.** The exact implementation candidate is
`6c71b46b841b1c38e8d3953ca15603b840d7184b` (tree
`4b11d5ea0b53a6544615cd994ebae9615ebc1910`), based on
`0834116ffa3aa41530bd42b3feb8f6d2b4b1dd4d`. Core canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. The candidate was published on
`codex/core/CORE-F-runtime-app-session-boundary-20261025`; it has not been
promoted to Core canonical.

The change is limited to the Core session publication gate and its tests. It
rejects initial and replacement publication unless the exact candidate has a
bound runtime-advance boundary, and removes the helper that could prepare and
publish genesis without binding that boundary first.

## Independent review

The separate exact-tip reviewer inspected the full two-file diff, current
CORE-F application/session contracts, Unity call sites, restoration
publication, and validation manifests. The review confirmed:

- Initial and replacement publication reject an unbound candidate.
- The supported Unity genesis path binds the exact session boundary before
  publication; restore publication retains its separate restored-admission
  checks before the atomic exchange.
- No C# call site remains for the removed prepare-and-publish helper.
- No actionable semantic, ownership, or test-integrity finding remains.

The review initially returned `NEEDS_CHANGES` because earlier results belonged
to source inputs predating this diff. That finding was resolved by rerunning
validation against the exact candidate. The independent reviewer rechecked both
new manifests and returned `PASS` without modifying repository files.

## Exact-tip validation

| Gate | Run | Result | Evidence identity |
| --- | --- | ---: | --- |
| Official CoreValidation cold | `9eed7b7fc6494683a28fc7f0cd0af354` | 818/818 | source commit/tree match candidate; stable input digest `7bf815bb2660d445373ac1de7971175aa8cdecfbb9718cd057afd34d3c99d62f` |
| Official CoreValidation warm | `9eed7b7fc6494683a28fc7f0cd0af354` | 818/818 | same exact source and stable input digest |
| Unity EditMode `SimulationRuntimeOrchestrationTests` | `7255e561ccf5452db03d9e52005dcffe` | 13/13 | stable before/after input `c178fd1762a1e1ee84e18084093b8025a15f1f43ff2ce6d5ff4580233ece333e` |
| `git diff --check` | candidate vs. parent | PASS | no whitespace errors |

The CoreValidation manifest records candidate commit
`6c71b46b841b1c38e8d3953ca15603b840d7184b`, tree
`4b11d5ea0b53a6544615cd994ebae9615ebc1910`, and stable file inputs. Its
SHA-256 is `b51bb0de3dd3ad276b32eb6fd7cbb39ef8db30348fd6cfdc3ac77f1c41747a2b`.
The Unity manifest is stable and its application source SHA-256 matches the
candidate (`8ce928ce95b537b3213f9499c600bfc785064b7c06ba4aeb18366735f06ea805`);
its SHA-256 is `f2006587b7feb2936dd5e8c0ca840c9f403288e7bb510169a79a06ab27e07321`.
The changed Core test source SHA-256 is
`e799c9221ce3b242a2729d799e2f02fcfaa6690f06279115bb47a88f749d4bec`.

CoreValidation reported that this checkout contains a Unity `Library` cache.
These results make no Library-independence claim. The Unity run was the focused
affected suite; full-package Unity, Smoke, detached-host, and T/T+1/T+2 gates
remain part of the larger incomplete CORE-F integration and are not claimed by
this checkpoint.

## Scope boundary

This is a recoverable implementation checkpoint only. It does not deliver the
Core-owned active runtime, `DailyV1Application.TryStart`, detached P12
restoration, the fixed .NET host, or deterministic cross-process continuation.
CORE-F remains incomplete and Core canonical remains unchanged. Private raw
manifests, reports, and logs remain in protected local evidence storage; no
workstation paths or raw evidence are included here.

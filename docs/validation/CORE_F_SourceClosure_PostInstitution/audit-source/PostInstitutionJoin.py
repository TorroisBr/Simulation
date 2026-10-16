#!/usr/bin/env python3
"""Join CORE-F owner/operation source references to the promoted code tree.

This is an evidence-preparation utility. It does not infer owner completeness,
implementation readiness, semantic reachability, or access authorization.
"""

from __future__ import annotations

from collections import Counter
import csv
from functools import lru_cache
import pathlib
import re
import subprocess
import sys

SOURCE_COMMIT = "cb31d4d6f777a78af25670e3e6d43f01578b8809"
PATH_RE = re.compile(r"(?:Assets|Packages|src|tests)/[A-Za-z0-9_./-]+\.cs")
APPROVED_TEST_ONLY_DELTAS = {
    "Assets/_Project/Tests/EditMode/Editor/ExpeditionTests.cs",
    "Assets/_Project/Tests/EditMode/Editor/P12DNpcRootOwnerSnapshotTests.cs",
    "Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs",
    "Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs.meta",
    "Assets/_Project/Tests/EditMode/Editor/P12GPoliticalBindingRestoreTests.cs",
}


def read_csv(path: pathlib.Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


@lru_cache(maxsize=None)
def git_blob(repo: pathlib.Path, path: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(repo), "rev-parse", f"{SOURCE_COMMIT}:{path}"],
        capture_output=True,
        text=True,
        check=False,
    )
    return result.stdout.strip() if result.returncode == 0 else ""


@lru_cache(maxsize=None)
def git_blob_at(repo: pathlib.Path, revision: str, path: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(repo), "rev-parse", f"{revision}:{path}"],
        capture_output=True,
        text=True,
        check=False,
    )
    return result.stdout.strip() if result.returncode == 0 else ""


def git_text(repo: pathlib.Path, path: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(repo), "show", f"{SOURCE_COMMIT}:{path}"],
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(f"Cannot read pinned source {SOURCE_COMMIT}:{path}")
    return result.stdout


def method_body(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        raise RuntimeError(f"Missing source method: {signature}")
    open_brace = source.find("{", start)
    if open_brace < 0:
        raise RuntimeError(f"Missing method body: {signature}")
    depth = 0
    for index in range(open_brace, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[open_brace + 1 : index]
    raise RuntimeError(f"Unclosed method body: {signature}")


def owner_anchor_line(
    source_lines: list[str], authority: str, section_id: str
) -> tuple[int, str]:
    if authority in {"FixedRequired", "FixedEmpty", "NpcPrefixes", "CityPrefixes"}:
        declaration = f"string[] {authority} ="
        begin = next(
            (index for index, line in enumerate(source_lines) if declaration in line),
            -1,
        )
        if begin < 0:
            raise RuntimeError(f"Missing {authority} inventory declaration")
        end = next(
            (index for index in range(begin + 1, len(source_lines)) if "};" in source_lines[index]),
            len(source_lines),
        )
        for index in range(begin, end):
            if f'"{section_id}"' in source_lines[index]:
                return index + 1, source_lines[index].strip()
    elif authority == "TryCreate dynamic key validation":
        for index, line in enumerate(source_lines):
            if f'"{section_id}"' in line:
                return index + 1, line.strip()
    raise RuntimeError(f"No current section anchor for {section_id} ({authority})")


def verify_live_owner_identity_assertion(repo: pathlib.Path) -> None:
    test_path = "Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs"
    source = git_text(repo, test_path)
    selected_profile = method_body(
        source,
        "public void SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne()",
    )
    owner_map_assertion = method_body(
        source,
        "private static void AssertSelectedDailyV1RegisteredOwnerIdentityMap(",
    )
    registered_owner_assertion = method_body(
        source,
        "private static void AssertRegisteredOwnerIdentityMap(",
    )
    required_fragments = (
        "AssertSelectedDailyV1RegisteredOwnerIdentityMap(",
        "expectedOwnerIds, Is.EqualTo(expectedSectionIds)",
        "AssertRegisteredOwnerIdentityMap(registeredSections, expectedOwners)",
        "witness.OwnerInstanceIdentity, Is.SameAs(expected.Value)",
    )
    actual = selected_profile + owner_map_assertion + registered_owner_assertion
    missing = [fragment for fragment in required_fragments if fragment not in actual]
    if missing:
        raise RuntimeError("Live-owner identity assertion is incomplete: " + ", ".join(missing))


def verify_selected_profile_knowledge_zero_assertion(repo: pathlib.Path) -> None:
    test_path = "Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs"
    source = git_text(repo, test_path)
    selected_profile = method_body(
        source,
        "public void SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne()",
    )
    required_fragments = (
        '"p12f.explorable-site-knowledge/"',
        '"p12f.local-topology-knowledge.places/"',
        '"p12f.local-topology-knowledge.connections/"',
        '"p12f.adventure-intel.opposition/"',
        '"p12f.adventure-intel.notable-items/"',
        '"p12f.adventure-intel.common-resources/"',
        '"p12f.adventure-intel.access/"',
        "if (sectionIndex < 7)",
        "expectedCardinalities[sectionIndex], Is.Zero",
        "expectedRevisions[sectionIndex], Is.Zero",
        "witness.Cardinality, Is.Zero",
        "witness.Revision, Is.Zero",
        "later runtime states may legitimately carry populated owner payloads",
    )
    missing = [fragment for fragment in required_fragments if fragment not in selected_profile]
    if missing:
        raise RuntimeError(
            "Selected-profile Required Knowledge exact-zero assertion is incomplete: "
            + ", ".join(missing)
        )


def write_csv(path: pathlib.Path, columns: list[str], rows: list[dict[str, str]]) -> None:
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=columns, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def require_exact_unity_projection(
    selected_rows: list[dict[str, str]], retained_rows: list[dict[str, str]]
) -> None:
    selected = Counter((row["source_path"], row["unity_symbol"]) for row in selected_rows)
    retained = Counter((row["source_path"], row["unity_symbol"]) for row in retained_rows)
    if selected != retained:
        raise RuntimeError("Selected Unity symbol projection disagrees with the retained edge inventory")


def main() -> int:
    repo = pathlib.Path(__file__).resolve().parents[4]
    output = pathlib.Path(__file__).resolve().parents[1]
    owner_dir = output.parent / "CORE_F_OwnerClosure"

    inventory = read_csv(output / "source-inventory.csv")
    core_inputs = read_csv(output / "core-build-inputs.csv")
    migration_rows = read_csv(output / "source-migration-crosswalk.csv")
    core_symbol_edges = read_csv(output / "core-symbol-edges.csv")
    unity_symbol_edges = read_csv(output / "unity-symbol-edges.csv")
    writer_rows = read_csv(owner_dir / "OWNER_WRITER_MATRIX.csv")
    capture_rows = read_csv(owner_dir / "OWNER_CAPTURE_TARGET_CROSSWALK.csv")
    operation_rows = read_csv(owner_dir / "OPERATION_CASE_MAP.csv")
    family_rows = read_csv(repo / "docs/architecture/SIMULATION_CORE_F_OWNER_SECTIONS.csv")

    source_changes = subprocess.run(
        ["git", "-C", str(repo), "diff", "--name-only", SOURCE_COMMIT, "HEAD", "--", "Assets", "Packages", "src", "tests"],
        capture_output=True,
        text=True,
        check=True,
    ).stdout.splitlines()
    unexpected_source_changes = sorted(set(source_changes) - APPROVED_TEST_ONLY_DELTAS)
    if unexpected_source_changes:
        raise RuntimeError(
            "Pinned source graph changed outside the exact approved test-only delta: "
            + ", ".join(unexpected_source_changes)
        )

    family_ids = [row["section_or_prefix"] for row in family_rows]
    writer_ids = [row["section_or_prefix"] for row in writer_rows]
    capture_ids = [row["section_or_prefix"] for row in capture_rows]
    operation_ids = [row["operation_id"] for row in operation_rows]
    if len(family_ids) != 89 or len(set(family_ids)) != 89:
        raise RuntimeError("The authoritative Daily-v1 owner-family inventory is not 89 unique rows")
    if set(writer_ids) != set(family_ids) or len(writer_ids) != 89:
        raise RuntimeError("Owner-writer matrix does not exactly cover the authoritative 89 families")
    if set(capture_ids) != set(family_ids) or len(capture_ids) != 89:
        raise RuntimeError("Capture/target crosswalk does not exactly cover the authoritative 89 families")
    if len(operation_ids) != 24 or len(set(operation_ids)) != 24:
        raise RuntimeError("The sealed operation case map is not 24 unique operations")

    owner_image_path = "Assets/_Project/Scripts/P12AContinuationImage.cs"
    owner_image_source = git_text(repo, owner_image_path)
    owner_image_lines = owner_image_source.splitlines()
    owner_anchors: list[dict[str, str]] = []
    for family in family_rows:
        line, text = owner_anchor_line(
            owner_image_lines,
            family["canonical_authority"],
            family["section_or_prefix"],
        )
        owner_anchors.append(
            {
                "section_or_prefix": family["section_or_prefix"],
                "role": family["role"],
                "shape": family["shape"],
                "canonical_authority": family["canonical_authority"],
                "source_path": owner_image_path,
                "current_source_line": str(line),
                "current_source_anchor": text,
                "source_commit": SOURCE_COMMIT,
                "source_blob": git_blob(repo, owner_image_path),
            }
        )
    write_csv(
        output / "owner-current-source-anchors.csv",
        [
            "section_or_prefix",
            "role",
            "shape",
            "canonical_authority",
            "source_path",
            "current_source_line",
            "current_source_anchor",
            "source_commit",
            "source_blob",
        ],
        owner_anchors,
    )
    verify_live_owner_identity_assertion(repo)
    verify_selected_profile_knowledge_zero_assertion(repo)

    inventory_by_path = {row["source_path"]: row for row in inventory}
    core_by_path = {row["source_path"]: row for row in core_inputs}
    migrations = {
        row["previous_unity_path"]: row["current_core_path"]
        for row in migration_rows
    }

    refs: list[dict[str, str]] = []
    source_cache: dict[str, tuple[str, str]] = {}

    def resolve(path: str) -> tuple[str, str]:
        if path not in source_cache:
            current_path = migrations.get(path, path)
            current_blob = (
                git_blob_at(repo, "HEAD", current_path)
                if current_path in APPROVED_TEST_ONLY_DELTAS
                else git_blob(repo, current_path)
            )
            source_cache[path] = (current_path, current_blob)
        return source_cache[path]

    def add_reference(row_kind: str, row_id: str, fields: dict[str, str]) -> None:
        for field, value in fields.items():
            for path in sorted(set(PATH_RE.findall(value or ""))):
                current_path, blob = resolve(path)
                if current_path in core_by_path:
                    compiler = "Simulation.Core"
                elif current_path in inventory_by_path:
                    compiler = "Unity Assembly-CSharp"
                elif "/Tests/" in current_path or current_path.startswith("tests/"):
                    compiler = "test source"
                else:
                    compiler = "unclassified source"
                refs.append(
                    {
                        "row_kind": row_kind,
                        "row_id": row_id,
                        "source_field": field,
                        "referenced_path": path,
                        "current_path": current_path,
                        "current_source_blob": blob,
                        "current_compilation_owner": compiler,
                        "resolution": "RESOLVED" if blob else "UNRESOLVED",
                    }
                )

    for row in writer_rows:
        add_reference("owner-writer", row["section_or_prefix"], row)
    for row in capture_rows:
        add_reference("capture-target", row["section_or_prefix"], row)
    for row in operation_rows:
        add_reference("operation", row["operation_id"], row)

    write_csv(
        output / "owner-operation-current-source-refs.csv",
        [
            "row_kind",
            "row_id",
            "source_field",
            "referenced_path",
            "current_path",
            "current_source_blob",
            "current_compilation_owner",
            "resolution",
        ],
        refs,
    )

    selected_access: list[dict[str, str]] = []
    for edge in core_symbol_edges:
        source = inventory_by_path.get(edge["source_path"])
        if source is None:
            raise RuntimeError(f"Core-symbol consumer missing from source inventory: {edge['source_path']}")
        runtime = source["runtime_closure"].lower() == "true"
        restore = source["restore_closure"].lower() == "true"
        if not runtime and not restore:
            continue
        selected_access.append(
            {
                "source_path": edge["source_path"],
                "source_blob": source["git_blob"],
                "source_scc": source["scc_id"],
                "runtime_closure": str(runtime).lower(),
                "restore_closure": str(restore).lower(),
                "target_assembly": "Simulation.Core",
                "resolved_symbol": edge["simulation_core_symbol"],
            }
        )

    write_csv(
        output / "selected-core-member-access.csv",
        [
            "source_path",
            "source_blob",
            "source_scc",
            "runtime_closure",
            "restore_closure",
            "target_assembly",
            "resolved_symbol",
        ],
        selected_access,
    )

    selected_engine_access: list[dict[str, str]] = []
    for source in inventory:
        runtime = source["runtime_closure"].lower() == "true"
        restore = source["restore_closure"].lower() == "true"
        if not runtime and not restore:
            continue
        for symbol in filter(None, source["unity_symbol_names"].split("|")):
            selected_engine_access.append(
                {
                    "source_path": source["source_path"],
                    "source_blob": source["git_blob"],
                    "source_scc": source["scc_id"],
                    "runtime_closure": str(runtime).lower(),
                    "restore_closure": str(restore).lower(),
                    "unity_symbol": symbol,
                }
            )

    # The graph has a separate edge file. Compare the full source/symbol
    # multiset so same-count substitutions cannot pass this integrity check.
    selected_engine_edges = [
        edge
        for edge in unity_symbol_edges
        if edge["source_path"] in inventory_by_path
        and (
            inventory_by_path[edge["source_path"]]["runtime_closure"].lower() == "true"
            or inventory_by_path[edge["source_path"]]["restore_closure"].lower() == "true"
        )
    ]
    require_exact_unity_projection(selected_engine_access, selected_engine_edges)

    write_csv(
        output / "selected-unity-engine-access.csv",
        [
            "source_path",
            "source_blob",
            "source_scc",
            "runtime_closure",
            "restore_closure",
            "unity_symbol",
        ],
        selected_engine_access,
    )

    unresolved_current_blobs = [
        row
        for row in refs
        if not row["current_source_blob"]
        or row["current_source_blob"] != git_blob_at(repo, "HEAD", row["current_path"])
    ]
    if unresolved_current_blobs:
        raise RuntimeError(
            f"Current source/blob join failed for {len(unresolved_current_blobs)} owner/operation references"
        )
    for source_rows, source_label in ((inventory, "Unity source inventory"), (core_inputs, "Core compile inputs")):
        mismatched = [
            row["source_path"]
            for row in source_rows
            if row["git_blob"] != git_blob_at(repo, "HEAD", row["source_path"])
        ]
        if mismatched:
            raise RuntimeError(f"{source_label} contains {len(mismatched)} stale source blob anchors")

    verify_live_owner_identity_assertion(repo)

    unresolved = sum(row["resolution"] != "RESOLVED" for row in refs)
    runtime_sources = sum(row["runtime_closure"].lower() == "true" for row in inventory)
    restore_sources = sum(row["restore_closure"].lower() == "true" for row in inventory)
    print(
        f"source={SOURCE_COMMIT} owners={len(writer_rows)} captures={len(capture_rows)} "
        f"operations={len(operation_rows)} source_refs={len(refs)} "
        f"unique_reference_paths={len({row['referenced_path'] for row in refs})} "
        f"unresolved_refs={unresolved} runtime_sources={runtime_sources} "
        f"restore_sources={restore_sources} selected_core_access={len(selected_access)} "
        f"selected_unity_engine_access={len(selected_engine_access)} "
        f"current_owner_anchors={len(owner_anchors)} "
        f"live_owner_identity=PASS selected_required_knowledge_zero=PASS"
    )
    return 1 if unresolved else 0


if __name__ == "__main__":
    sys.exit(main())

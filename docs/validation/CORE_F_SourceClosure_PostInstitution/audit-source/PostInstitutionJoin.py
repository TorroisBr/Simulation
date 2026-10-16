#!/usr/bin/env python3
"""Join CORE-F owner/operation source references to the promoted code tree.

This is an evidence-preparation utility. It does not infer owner completeness,
implementation readiness, semantic reachability, or access authorization.
"""

from __future__ import annotations

from collections import Counter
import csv
import pathlib
import re
import subprocess
import sys

SOURCE_COMMIT = "ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d"
PATH_RE = re.compile(r"(?:Assets|Packages|src|tests)/[A-Za-z0-9_./-]+\.cs")


def read_csv(path: pathlib.Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def git_blob(repo: pathlib.Path, path: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(repo), "rev-parse", f"{SOURCE_COMMIT}:{path}"],
        capture_output=True,
        text=True,
        check=False,
    )
    return result.stdout.strip() if result.returncode == 0 else ""


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
            current_blob = git_blob(repo, current_path)
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

    unresolved = sum(row["resolution"] != "RESOLVED" for row in refs)
    runtime_sources = sum(row["runtime_closure"].lower() == "true" for row in inventory)
    restore_sources = sum(row["restore_closure"].lower() == "true" for row in inventory)
    print(
        f"source={SOURCE_COMMIT} owners={len(writer_rows)} captures={len(capture_rows)} "
        f"operations={len(operation_rows)} source_refs={len(refs)} "
        f"unique_reference_paths={len({row['referenced_path'] for row in refs})} "
        f"unresolved_refs={unresolved} runtime_sources={runtime_sources} "
        f"restore_sources={restore_sources} selected_core_access={len(selected_access)} "
        f"selected_unity_engine_access={len(selected_engine_access)}"
    )
    return 1 if unresolved else 0


if __name__ == "__main__":
    sys.exit(main())

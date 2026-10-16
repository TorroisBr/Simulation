#!/usr/bin/env python3
"""Build the finite CORE-F selected-owner and split-host access ledgers.

This tool joins the retained P12 owner/capture matrices to current source,
the selected-profile live-owner identity map, and the source-bound Roslyn
member-access inventory. It does not change simulation production code or
declare the downstream shared host ready.
"""

from __future__ import annotations

import csv
import functools
import gzip
import hashlib
import json
import pathlib
import re
import subprocess
import sys
from collections import Counter, defaultdict

SOURCE_REFERENCE = "ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d"
CORE_CANONICAL_BASE = "1998974515b6357c308d861fb608f2db5071a63a"
ARCHITECTURE_CONTRACT = "512cf5721927a5b3d6766bc4d19cfd62a6729a41"
ARCHITECTURE_CONTRACT_PATH = "docs/architecture/CORE_F_OWNER_ENTRY_CONTRACT.md"
OWNER_DISPOSITIONS_PATH = "docs/architecture/CORE_F_OWNER_DISPOSITIONS.csv"
APPROVED_TEST_DELTAS = {
    "Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs",
    "Assets/_Project/Tests/EditMode/Editor/P12GPoliticalBindingRestoreTests.cs",
    "Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs",
    "Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs.meta",
    "Assets/_Project/Tests/EditMode/Editor/P12DNpcRootOwnerSnapshotTests.cs",
    "Assets/_Project/Tests/EditMode/Editor/ExpeditionTests.cs",
}

UNITY_DESTINATIONS: dict[str, tuple[str, str]] = {}


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def run_git(repo: pathlib.Path, *args: str) -> str:
    result = subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True)
    if result.returncode:
        raise RuntimeError("Git identity query failed: " + " ".join(args))
    return result.stdout.strip()


def read_csv(path: pathlib.Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def write_csv(path: pathlib.Path, columns: list[str], rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=columns, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def matching_source_deltas(repo: pathlib.Path) -> set[str]:
    committed = set(filter(None, run_git(
        repo, "diff", "--name-only", SOURCE_REFERENCE, "HEAD", "--", "Assets", "Packages", "src", "tests"
    ).splitlines()))
    working = set(filter(None, run_git(
        repo, "diff", "--name-only", "--", "Assets", "Packages", "src", "tests"
    ).splitlines()))
    untracked = set()
    for line in run_git(repo, "status", "--porcelain", "--untracked-files=all").splitlines():
        if len(line) >= 4 and line[3:].startswith(("Assets/", "Packages/", "src/", "tests/")) and line[1:2] == "?":
            untracked.add(line[3:])
    return committed | working | untracked


def find_body(source: str, signature: str) -> tuple[str, int]:
    start = source.find(signature)
    if start < 0:
        raise RuntimeError("Missing selected-profile identity method: " + signature)
    open_brace = source.find("{", start)
    if open_brace < 0:
        raise RuntimeError("Missing method body: " + signature)
    end = balanced_close(source, open_brace, "{", "}")
    return source[open_brace + 1 : end], open_brace + 1


def balanced_close(text: str, start: int, opening: str, closing: str) -> int:
    depth = 0
    quote = ""
    escaped = False
    line_comment = False
    block_comment = False
    index = start
    while index < len(text):
        char = text[index]
        nxt = text[index + 1] if index + 1 < len(text) else ""
        if line_comment:
            if char == "\n":
                line_comment = False
        elif block_comment:
            if char == "*" and nxt == "/":
                block_comment = False
                index += 1
        elif quote:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == quote:
                quote = ""
        elif char == "/" and nxt == "/":
            line_comment = True
            index += 1
        elif char == "/" and nxt == "*":
            block_comment = True
            index += 1
        elif char in {'"', "'"}:
            quote = char
        elif char == opening:
            depth += 1
        elif char == closing:
            depth -= 1
            if depth == 0:
                return index
        index += 1
    raise RuntimeError("Unbalanced C# delimiter while reading selected identity map")


def split_arguments(value: str) -> list[str]:
    args: list[str] = []
    begin = 0
    depths = {"(": 0, "[": 0, "{": 0, "<": 0}
    closers = {")": "(", "]": "[", "}": "{", ">": "<"}
    quote = ""
    escaped = False
    index = 0
    while index < len(value):
        char = value[index]
        if quote:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == quote:
                quote = ""
        elif char in {'"', "'"}:
            quote = char
        elif char in depths:
            depths[char] += 1
        elif char in closers:
            opener = closers[char]
            if depths[opener] > 0:
                depths[opener] -= 1
        elif char == "," and not any(depths.values()):
            args.append(value[begin:index].strip())
            begin = index + 1
        index += 1
    args.append(value[begin:].strip())
    return args


def invocations(source: str, body: str, body_start: int, name: str) -> list[dict[str, str | int]]:
    results: list[dict[str, str | int]] = []
    search = 0
    while True:
        local = body.find(name + "(", search)
        if local < 0:
            break
        open_paren = body.find("(", local)
        close_paren = balanced_close(body, open_paren, "(", ")")
        absolute = body_start + local
        args = split_arguments(body[open_paren + 1 : close_paren])
        if len(args) != 3:
            raise RuntimeError("Unexpected selected identity call arity at line " + str(source.count("\n", 0, absolute) + 1))
        results.append({
            "line": source.count("\n", 0, absolute) + 1,
            "section_expression": args[1],
            "owner_expression": args[2],
            "raw": body[local : close_paren + 1],
        })
        search = close_paren + 1
    return results


def method_identity_calls(source: str) -> list[dict[str, str | int]]:
    calls: list[dict[str, str | int]] = []
    for signature, method_name in (
        ("private static Dictionary<string, object> BuildSelectedDailyV1DynamicOwnerIdentityMap(", "BuildSelectedDailyV1DynamicOwnerIdentityMap"),
        ("internal static Dictionary<string, object> BuildSelectedDailyV1RegisteredOwnerIdentityMap(", "BuildSelectedDailyV1RegisteredOwnerIdentityMap"),
    ):
        body, body_start = find_body(source, signature)
        method_calls = invocations(source, body, body_start, "AddExpectedDailyV1OwnerIdentity")
        for call in method_calls:
            call["identity_method"] = method_name
        calls.extend(method_calls)
    return calls


def class_body(source: str, class_name: str) -> str | None:
    match = re.search(r"\b(?:class|struct)\s+" + re.escape(class_name) + r"\b", source)
    if not match:
        return None
    open_brace = source.find("{", match.end())
    if open_brace < 0:
        return None
    return source[open_brace + 1 : balanced_close(source, open_brace, "{", "}")]


def provider_sources(repo: pathlib.Path) -> dict[str, tuple[str, pathlib.Path]]:
    result: dict[str, tuple[str, pathlib.Path]] = {}
    for root in (repo / "Assets/_Project/Scripts", repo / "Packages/com.simulation.core/Runtime"):
        if not root.exists():
            continue
        for path in root.rglob("*.cs"):
            text = path.read_text(encoding="utf-8-sig")
            for match in re.finditer(r"\b(?:class|struct)\s+(\w+)\b", text):
                name = match.group(1)
                if name not in result:
                    result[name] = (text, path)
    return result


def provider_binding(row: dict[str, str], provider_file_text: str) -> tuple[str, str]:
    # The P12-F recorder was added by the promoted architecture closure and
    # its owner matrix intentionally has a disposition instead of a concrete
    # class declaration. Resolve it before parsing older generic anchors.
    if row["section_or_prefix"] == "p12f.npc-decision-occurrence-receipts":
        return "NpcDecisionRecorder", "additive current-source correction"
    for field, label in (
        ("provider_binding", "matrix provider declaration"),
        ("actual_provider_type_and_baseline_source", "capture crosswalk provider declaration"),
    ):
        value = row.get(field, "")
        class_names = re.findall(r"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)\b", value)
        if class_names:
            return class_names[-1], label
        match = re.search(r"\b([A-Za-z_][A-Za-z0-9_]*)\s+Provider implementation source\b", value)
        if match:
            return match.group(1), label
    if row["section_or_prefix"] == "p12e.economy-keyed-sale-receipts":
        return "EconomyTransactionService", "capture crosswalk current source"
    raise RuntimeError("Cannot identify provider type for " + row["section_or_prefix"])


def knowledge_array_bindings(body: str) -> dict[str, tuple[str, str]]:
    prefixes_match = re.search(r"string\[\]\s+knowledgePrefixes\s*=\s*\{([^}]+)\}", body, re.S)
    owners_match = re.search(r"object\[\]\s+knowledgeOwners\s*=\s*\{([^}]+)\}", body, re.S)
    if not prefixes_match or not owners_match:
        raise RuntimeError("Knowledge identity arrays are missing from the current source map")
    prefixes = re.findall(r'"([^"\\]*(?:\\.[^"\\]*)*)"', prefixes_match.group(1))
    owners = split_arguments(owners_match.group(1))
    if len(prefixes) != len(owners):
        raise RuntimeError("Knowledge owner/prefix arrays have different lengths")
    result: dict[str, tuple[str, str]] = {}
    for index, (prefix, expression) in enumerate(zip(prefixes, owners)):
        result[prefix] = (f"knowledgeOwners[{index}] => {expression.strip()}", str(index))
    return result


def expression_matches_family(
    repo: pathlib.Path,
    family: str,
    calls: list[dict[str, str | int]],
    providers: dict[str, tuple[str, pathlib.Path]],
    knowledge: dict[str, tuple[str, str]],
) -> dict[str, str | int]:
    if family in knowledge:
        binding, index = knowledge[family]
        call = next((row for row in calls if "knowledgePrefixes[i]" in str(row["section_expression"])), None)
        if call is None:
            raise RuntimeError("Missing per-NPC Knowledge identity-map loop for " + family)
        call = dict(call)
        call["owner_expression"] = binding
        call["knowledge_index"] = index
        return call

    matches: list[dict[str, str | int]] = []
    family_slug = family.rstrip("/")
    for call in calls:
        section_expr = str(call["section_expression"])
        class_refs = re.findall(r"\b([A-Za-z_][A-Za-z0-9_]*)\.[A-Za-z_][A-Za-z0-9_]*", section_expr)
        # Exact string literals or source-bound constants are preferred.
        literal_values = re.findall(r'"([^"\\]*(?:\\.[^"\\]*)*)"', section_expr)
        if family in literal_values:
            matches.append(call)
            continue
        for class_name in set(class_refs):
            provider = providers.get(class_name)
            if provider is None:
                continue
            provider_text, _ = provider
            body = class_body(provider_text, class_name) or ""
            if family not in body:
                continue
            const_pairs = re.findall(r'\bconst\s+string\s+(\w+)\s*=\s*"([^"\\]*(?:\\.[^"\\]*)*)"', body)
            if any(value == family and re.search(r"\b" + re.escape(class_name) + r"\." + re.escape(name) + r"\b", section_expr)
                   for name, value in const_pairs):
                matches.append(call)
                continue
            # Some providers compute a stable prefix in SectionIdFor. These
            # bounded cases have distinguishable existing enum/boolean inputs.
            if "SectionIdFor" in section_expr:
                exact_prefix_methods = [
                    name.removesuffix("SectionPrefix") + "SectionIdFor"
                    for name, value in const_pairs
                    if value == family and name.endswith("SectionPrefix")
                ]
                if any(re.search(r"\b" + re.escape(class_name) + r"\." + re.escape(method) + r"\b", section_expr)
                       for method in exact_prefix_methods):
                    matches.append(call)
                    continue
                if "merchant-trade-plan/" in family and "MerchantTradePlanKind" in section_expr:
                    matches.append(call)
                elif "npc-travel-plan/" in family and "TravelPlanKind" in section_expr:
                    matches.append(call)
                elif "npc-residence/" in family and "residence: true" in section_expr:
                    matches.append(call)
                elif "npc-life-state/" in family and "residence: false" in section_expr:
                    matches.append(call)
                elif sum(1 for candidate in calls if class_name + ".SectionIdFor" in str(candidate["section_expression"])) == 1:
                    matches.append(call)

    dedup = {int(row["line"]): row for row in matches}
    if len(dedup) != 1:
        raise RuntimeError(f"Current live-owner map is not a unique exact binding for {family}: {len(dedup)} candidates")
    return next(iter(dedup.values()))


def declaration_anchor(repo: pathlib.Path, owner_expression: str) -> str:
    if "knowledgeOwners[" in owner_expression:
        return "Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs:knowledge owner array element; NpcRuntime ExistingKnowledge owner"
    if "GetRuntimeOwner(runtime," in owner_expression:
        field = re.search(r'GetRuntimeOwner\(runtime,\s*"([^"]+)"\)', owner_expression)
        name = field.group(1) if field else "unknown"
        return f"Assets/_Project/Scripts/SimulationRuntime.cs:{name} Runtime private-field injection; see owner expression for exact selected source/test identity"
    if "ReadNonPublicProperty(" in owner_expression:
        prop = re.search(r'ReadNonPublicProperty\([^,]+,\s*"([^"]+)"\)', owner_expression)
        name = prop.group(1) if prop else "unknown"
        return f"Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs:{name} exact private-property identity read"
    if "bootstrap." in owner_expression:
        return "Assets/_Project/Scripts/SimulationRuntime.cs:SimulationBootstrapComposition owner projection; selected live map line is row-specific"
    if "runtime." in owner_expression:
        return "Assets/_Project/Scripts/SimulationRuntime.cs:SimulationRuntime member expression; selected live map line is row-specific"
    if "city." in owner_expression:
        return "Assets/_Project/Scripts/CityRuntime.cs:CityRuntime member expression; selected live map line is row-specific"
    if "npc." in owner_expression:
        return "Assets/_Project/Scripts/NpcRuntime.cs:NpcRuntime member expression; selected live map line is row-specific"
    if "person" in owner_expression.lower():
        return "Assets/_Project/Scripts/Person/PersonRuntime.cs:PersonRuntime instance from selected PersonStore enumeration"
    return "Selected-profile owner expression is directly anchored by its source line in the test ledger"


def method_bindings(source: str, method_name: str) -> dict[str, dict[str, str | int]]:
    signature = next((value for value, name in (
        ("private static Dictionary<string, object> BuildSelectedDailyV1DynamicOwnerIdentityMap(", "BuildSelectedDailyV1DynamicOwnerIdentityMap"),
        ("internal static Dictionary<string, object> BuildSelectedDailyV1RegisteredOwnerIdentityMap(", "BuildSelectedDailyV1RegisteredOwnerIdentityMap"),
    ) if name == method_name), None)
    if signature is None:
        raise RuntimeError("Unknown selected-owner identity method: " + method_name)
    body, body_start = find_body(source, signature)
    result: dict[str, dict[str, str | int]] = {}
    signature_start = source.find(signature)
    parameter_open = source.find("(", signature_start)
    parameter_close = balanced_close(source, parameter_open, "(", ")")
    for parameter in split_arguments(source[parameter_open + 1:parameter_close]):
        parameter_match = re.fullmatch(r"\s*([A-Za-z_][A-Za-z0-9_.<>?,\[\]]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*", parameter)
        if parameter_match:
            result[parameter_match.group(2)] = {
                "type": parameter_match.group(1),
                "expression": "selected profile identity-map method input",
                "line": source.count("\n", 0, signature_start) + 1,
                "kind": "method parameter",
            }
    declaration_pattern = re.compile(
        r"(?m)^[ \t]*(?P<type>var|[A-Za-z_][A-Za-z0-9_.<>?,\[\]]*)[ \t]+(?P<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*=[ \t]*(?P<expression>[\s\S]*?);"
    )
    for match in declaration_pattern.finditer(body):
        absolute = body_start + match.start()
        result[match.group("name")] = {
            "type": match.group("type"),
            "expression": match.group("expression").strip(),
            "line": source.count("\n", 0, absolute) + 1,
            "kind": "local initializer",
        }
    foreach_search = 0
    while True:
        start = body.find("foreach", foreach_search)
        if start < 0:
            break
        open_paren = body.find("(", start)
        if open_paren < 0:
            break
        close_paren = balanced_close(body, open_paren, "(", ")")
        contents = body[open_paren + 1 : close_paren]
        match = re.match(r"\s*(?P<type>[A-Za-z_][A-Za-z0-9_.<>?,\[\]]*)\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)\s+in\s+(?P<expression>[\s\S]+)$", contents)
        if match:
            absolute = body_start + start
            result[match.group("name")] = {
                "type": match.group("type"),
                "expression": match.group("expression").strip(),
                "line": source.count("\n", 0, absolute) + 1,
                "kind": "foreach source",
            }
        foreach_search = close_paren + 1
    return result


def resolve_live_owner_expression(
    expression: str,
    bindings: dict[str, dict[str, str | int]],
) -> tuple[str, str]:
    current = expression.split("=>", 1)[-1].strip()
    evidence: list[str] = []
    seen: set[str] = set()
    evidence_names: set[str] = set()
    for name, binding in bindings.items():
        if re.search(rf"\b{re.escape(name)}\b", current):
            evidence_names.add(name)
            evidence.append(
                f"{name} ({binding['kind']} line {binding['line']}, type {binding['type']}) = {binding['expression']}"
            )
    while re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", current) and current in bindings and current not in seen:
        seen.add(current)
        binding = bindings[current]
        if current not in evidence_names:
            evidence.append(f"{current} ({binding['kind']} line {binding['line']}, type {binding['type']}) = {binding['expression']}")
        source_expression = str(binding["expression"])
        if binding["kind"] == "foreach source":
            current = source_expression
            break
        current = source_expression
    return current, " | ".join(evidence)


@functools.lru_cache(maxsize=1)
def current_production_source_index(repo_text: str) -> tuple[tuple[str, str, str], ...]:
    repo = pathlib.Path(repo_text)
    items: list[tuple[str, str, str]] = []
    for root in (repo / "Assets/_Project/Scripts", repo / "Packages/com.simulation.core/Runtime"):
        if not root.exists():
            continue
        for path in root.rglob("*.cs"):
            relative = path.relative_to(repo).as_posix()
            items.append((relative, path.read_text(encoding="utf-8-sig"), sha256(path.read_bytes())))
    return tuple(items)


def owner_source_anchor(repo: pathlib.Path, owner_expression: str, bindings: dict[str, dict[str, str | int]]) -> tuple[str, str]:
    resolved, context = resolve_live_owner_expression(owner_expression, bindings)
    type_hint = ""
    member_name = ""
    owner_identifier = owner_expression.split("=>", 1)[-1].strip()
    if re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", owner_identifier) and owner_identifier in bindings:
        binding = bindings[owner_identifier]
        if binding["kind"] == "foreach source":
            type_hint = str(binding["type"])
    direct_member = re.fullmatch(r"([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)", owner_expression.strip())
    if direct_member and direct_member.group(1) in bindings:
        type_hint = str(bindings[direct_member.group(1)]["type"])
        member_name = direct_member.group(2)
    if "GetOccurrenceReceiptCensus" in resolved:
        type_hint, member_name = "NpcDecisionRecorder", "GetOccurrenceReceiptCensus"
    elif "GetEconomyKeyedSaleReceiptCensus" in resolved:
        type_hint, member_name = "SimulationBootstrapComposition", "GetEconomyKeyedSaleReceiptCensus"
    elif not type_hint:
        runtime_owner = re.search(r'GetRuntimeOwner\(runtime,\s*"([^"]+)"\)', resolved)
        if runtime_owner:
            type_hint, member_name = "SimulationRuntime", runtime_owner.group(1)
        else:
            private_property = re.search(r'ReadNonPublicProperty\(([^,]+),\s*"([^"]+)"\)', resolved)
            if private_property:
                receiver = private_property.group(1).strip()
                member_name = private_property.group(2)
                if receiver == "npc" or member_name.startswith("Existing"):
                    type_hint = "NpcRuntime"
                elif "ActorChoiceStore" in receiver:
                    type_hint = "ActorChoiceStore"
                elif "recordSequence" in receiver:
                    type_hint = "SimulationRecordSequence"
                elif "allocator" in receiver:
                    type_hint = "RuntimeIdAllocator"
            if not type_hint:
                if re.search(r"\bruntime\.[A-Za-z_]", resolved):
                    type_hint = "SimulationRuntime"
                    member_name = re.search(r"\bruntime\.([A-Za-z_][A-Za-z0-9_]*)", resolved).group(1)
                elif re.search(r"\bbootstrap\.[A-Za-z_]", resolved):
                    type_hint = "SimulationBootstrapComposition"
                    member_name = re.search(r"\bbootstrap\.([A-Za-z_][A-Za-z0-9_]*)", resolved).group(1)
                elif re.search(r"\bnpc\.[A-Za-z_]", resolved):
                    type_hint = "NpcRuntime"
                    member_name = re.search(r"\bnpc\.([A-Za-z_][A-Za-z0-9_]*)", resolved).group(1)
                elif re.search(r"\bcity\.[A-Za-z_]", resolved):
                    type_hint = "CityRuntime"
                    member_name = re.search(r"\bcity\.([A-Za-z_][A-Za-z0-9_]*)", resolved).group(1)
                elif re.search(r"\bperson\.[A-Za-z_]", resolved):
                    type_hint = "PersonRuntime"
                    member_name = re.search(r"\bperson\.([A-Za-z_][A-Za-z0-9_]*)", resolved).group(1)
                elif re.search(r"\bcrimeSocial\.[A-Za-z_]", resolved):
                    type_hint = "CrimeSocialAppraisalWorldState"
                    member_name = re.search(r"\bcrimeSocial\.([A-Za-z_][A-Za-z0-9_]*)", resolved).group(1)
                elif re.search(r"\bspatialAuthority\.[A-Za-z_]", resolved):
                    type_hint = "SpatialAuthorityStore"
                    member_name = re.search(r"\bspatialAuthority\.([A-Za-z_][A-Za-z0-9_]*)", resolved).group(1)
                elif re.search(r"\bspatialNetwork\.[A-Za-z_]", resolved):
                    type_hint = "SpatialNetworkRuntime"
                    member_name = re.search(r"\bspatialNetwork\.([A-Za-z_][A-Za-z0-9_]*)", resolved).group(1)
                else:
                    for variable, binding in bindings.items():
                        if re.search(rf"\b{re.escape(variable)}\b", owner_expression.split("=>", 1)[-1].strip()):
                            type_hint = str(binding["type"])
                            break
    if not type_hint:
        # Whole-owner aliases such as the exact NPC/city/person objects resolve
        # through their loop declaration; use that production type declaration.
        for variable, binding in bindings.items():
            if re.search(rf"\b{re.escape(variable)}\b", owner_expression.split("=>", 1)[-1].strip()):
                type_hint = str(binding["type"])
                break
    if type_hint == "object":
        type_hint = "SimulationRuntime" if "runtime." in resolved else type_hint
    if not type_hint:
        raise RuntimeError("Cannot bind live owner expression to a production type: " + owner_expression)

    source_items = current_production_source_index(str(repo.resolve()))
    class_pattern = re.compile(r"\b(?:class|struct)\s+" + re.escape(type_hint) + r"\b")
    class_items = [(relative, text, blob) for relative, text, blob in source_items if class_pattern.search(text)]
    if member_name:
        declaration = None
        member_pattern = re.compile(
            r"(?m)^[ \t]*(?:public|protected|internal|private)\b"
            r"(?:[ \t]+(?:static|readonly|volatile|new|virtual|override|sealed|partial|unsafe|extern))*"
            r"[ \t]+[A-Za-z_][A-Za-z0-9_.<>,? \[\]]*?[ \t]+"
            + re.escape(member_name)
            + r"\b[ \t]*(?=;|=|\{|=>|\(|$)[^\r\n]*$"
        )
        for relative, text, blob in class_items:
            lines = text.splitlines()
            declaration_line = next((i for i, line in enumerate(lines, start=1) if member_pattern.search(line)), None)
            if declaration_line is not None:
                declaration = (relative, text, blob, lines, declaration_line)
                break
        if declaration is not None:
            relative, text, blob, lines, declaration_line = declaration
            anchor = f"{relative}:{declaration_line} {type_hint}.{member_name} declaration (working-tree file SHA-256 {blob})"
            assignment_names = {member_name}
            declaration_text = lines[declaration_line - 1]
            arrow = re.search(r"=>\s*([A-Za-z_][A-Za-z0-9_]*)\s*;", declaration_text)
            if arrow:
                assignment_names.add(arrow.group(1))
            if "=>" in declaration_text:
                expression = declaration_text.split("=>", 1)[1].split(";", 1)[0]
                first_value = re.match(r"\s*([A-Za-z_][A-Za-z0-9_]*)", expression)
                if first_value:
                    assignment_names.add(first_value.group(1))
                assignment_names.update(re.findall(r"\b([A-Za-z_][A-Za-z0-9_]*)\s*=", expression))
            if "{" in declaration_text or (declaration_line < len(lines) and lines[declaration_line].strip().startswith("{")):
                line_offset = sum(len(line) + 1 for line in lines[:declaration_line - 1])
                open_brace = text.find("{", line_offset)
                if open_brace >= 0:
                    try:
                        close_brace = balanced_close(text, open_brace, "{", "}")
                        property_body = text[open_brace + 1:close_brace]
                        returned = re.findall(r"\breturn\s+([A-Za-z_][A-Za-z0-9_]*)\s*;", property_body)
                        assigned = re.findall(r"\b([A-Za-z_][A-Za-z0-9_]*)\s*=", property_body)
                        assignment_names.update(returned)
                        assignment_names.update(name for name in assigned if name[:1].islower())
                    except RuntimeError:
                        pass
            if "(" in declaration_text:
                line_offset = sum(len(line) + 1 for line in lines[:declaration_line - 1])
                body_open = text.find("{", line_offset)
                if body_open >= 0:
                    try:
                        body_close = balanced_close(text, body_open, "{", "}")
                        method_body = text[body_open + 1:body_close]
                        assignment_names.update(re.findall(r"\b([A-Za-z_][A-Za-z0-9_]*OwnerIdentity)\b", method_body))
                    except RuntimeError:
                        pass
            construction_hits: list[str] = []
            for partial_relative, partial_text, _ in class_items:
                partial_lines = partial_text.splitlines()
                for construction_name in sorted(assignment_names):
                    pattern = re.compile(r"(?m)^[ \t]*(?:this\.)?" + re.escape(construction_name) + r"\s*=")
                    construction_hits.extend(
                        f"{partial_relative}:{i} {construction_name} assignment"
                        for i, line in enumerate(partial_lines, start=1) if pattern.search(line)
                    )
                    field_pattern = re.compile(
                        r"(?m)^[ \t]*(?:\[[^\]]+\][ \t]*)*(?:public|protected|internal|private)\b[^\r\n]*\b"
                        + re.escape(construction_name)
                        + r"\s*="
                    )
                    construction_hits.extend(
                        f"{partial_relative}:{i} {construction_name} field initializer"
                        for i, line in enumerate(partial_lines, start=1) if field_pattern.search(line)
                    )
            if member_name == "GetEconomyKeyedSaleReceiptCensus" and not construction_hits:
                for partial_relative, partial_text, _ in class_items:
                    construction_hits.extend(
                        f"{partial_relative}:{i} provider delegation to keyed sale receipt census"
                        for i, line in enumerate(partial_text.splitlines(), start=1)
                        if ".GetKeyedSaleReceiptCensus(" in line
                    )
            composition = "; ".join(sorted(set(construction_hits))) if construction_hits else "live source member is executable-identity checked; no direct assignment is declared in the owner type"
            return anchor + "; composition: " + composition, context
    for relative, text, blob in class_items:
        class_match = re.search(r"(?m)^\s*(?:public|protected|internal|private)\b[^\r\n]*\b(?:class|struct)\s+" + re.escape(type_hint) + r"\b", text)
        if class_match:
            line = text.count("\n", 0, class_match.start()) + 1
            anchor = f"{relative}:{line} {type_hint} live owner type (working-tree file SHA-256 {blob})"
            return anchor + "; composition: selected runtime enumerates and identity-compares this exact type", context
    raise RuntimeError(f"Cannot bind live owner member to a source declaration: {type_hint}.{member_name} ({owner_expression})")


def make_owner_ledger(repo: pathlib.Path, output: pathlib.Path) -> tuple[list[dict[str, str]], str]:
    source_root = repo / "docs/validation/CORE_F_SourceClosure_PostInstitution"
    owners_dir = repo / "docs/validation/CORE_F_OwnerClosure"
    family_rows = read_csv(repo / "docs/architecture/SIMULATION_CORE_F_OWNER_SECTIONS.csv")
    writer_rows = read_csv(owners_dir / "OWNER_WRITER_MATRIX.csv")
    capture_rows = read_csv(owners_dir / "OWNER_CAPTURE_TARGET_CROSSWALK.csv")
    if len(family_rows) != 89 or len(writer_rows) != 89 or len(capture_rows) != 89:
        raise RuntimeError("The exact 89-row owner/writer/capture inputs are required")
    writer_by_id = {row["section_or_prefix"]: row for row in writer_rows}
    capture_by_id = {row["section_or_prefix"]: row for row in capture_rows}
    if set(writer_by_id) != set(capture_by_id) or set(writer_by_id) != {row["section_or_prefix"] for row in family_rows}:
        raise RuntimeError("Owner, writer, and capture keys do not match exactly")

    test_path = "Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs"
    test_source = (repo / test_path).read_text(encoding="utf-8-sig")
    calls = method_identity_calls(test_source)
    dynamic_body, _ = find_body(
        test_source,
        "private static Dictionary<string, object> BuildSelectedDailyV1DynamicOwnerIdentityMap(",
    )
    knowledge = knowledge_array_bindings(dynamic_body)
    providers = provider_sources(repo)
    test_blob = run_git(repo, "hash-object", "--", test_path)
    ledger: list[dict[str, str]] = []
    for family in family_rows:
        key = family["section_or_prefix"]
        writer = writer_by_id[key]
        capture = capture_by_id[key]
        family_source_blob = run_git(
            repo, "rev-parse", f"{family['production_base']}:{family['source_path']}"
        )
        live = expression_matches_family(repo, key, calls, providers, knowledge)
        owner_expression = str(live["owner_expression"])
        identity_method = str(live["identity_method"])
        owner_anchor, owner_binding_context = owner_source_anchor(
            repo, owner_expression, method_bindings(test_source, identity_method)
        )
        binding_type, binding_source = provider_binding(writer, capture.get("actual_provider_type_and_baseline_source", ""))
        provider_source = providers.get(binding_type)
        if provider_source is None:
            raise RuntimeError(f"Provider declaration is not source-resolved for {key}: {binding_type}")
        provider_text, provider_path = provider_source
        provider_class = re.search(r"\b(?:class|struct)\s+" + re.escape(binding_type) + r"\b", provider_text)
        if provider_class is None:
            raise RuntimeError(f"Provider declaration source does not contain the exact type for {key}: {binding_type}")
        provider_line = provider_text.count("\n", 0, provider_class.start()) + 1
        provider_blob = run_git(repo, "hash-object", "--", provider_path.relative_to(repo).as_posix())
        provider_anchor = f"{provider_path.relative_to(repo).as_posix()}:{provider_line} {binding_type} (Git blob {provider_blob})"
        identity_line = int(live["line"])
        identity_map_method = (
            "BuildSelectedDailyV1DynamicOwnerIdentityMap"
            if key.endswith("/") and family["shape"] in {"per-NPC", "per-City", "per-Person", "per-non-Person-NPC"}
            else "BuildSelectedDailyV1RegisteredOwnerIdentityMap"
        )
        if identity_map_method != identity_method:
            raise RuntimeError("Live owner identity call was bound to the wrong selected-profile map for " + key)
        live_source = "Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs"
        owner_case = capture.get("exact_target_negative_or_restore_case_and_baseline_blob", "")
        writer_case = writer.get("transition_case_evidence", "")
        ledger.append({
            "section_or_prefix": key,
            "role": family["role"],
            "shape": family["shape"],
            "canonical_authority": family["canonical_authority"],
            "family_schema_source_anchor": f"{family['source_path']}:{family['source_line']} ({family_source_blob} at {family['production_base']})",
            "identity_provider_type": binding_type,
            "identity_provider_source_anchor": writer.get("provider_binding", ""),
            "identity_provider_current_declaration": provider_anchor,
            "live_identity_map_method": identity_map_method,
            "live_identity_test_source": f"{live_source}:{identity_line} ({test_blob})",
            "live_owner_identity_expression": owner_expression,
            "live_owner_identity_binding_context": owner_binding_context,
            "owner_member_source_anchor": owner_anchor,
            "provider_registration_and_alias_evidence": writer.get("provider_binding", ""),
            "supported_writer_or_architecture_disposition": writer.get("admitted_writer_operation_disposition", ""),
            "revision_and_epoch_evidence": writer.get("epoch_semantics", "") + " " + writer.get("revision_and_alias_evidence", ""),
            "operation_case_evidence": writer_case,
            "capture_encoder_and_source_snapshot": capture.get("exact_value_codec_or_exact_empty_encoder_source", ""),
            "capture_case_identity": capture.get("codec_or_exact_empty_test_case_and_baseline_blob", ""),
            "source_capture_and_stage": capture.get("capture_and_package_stage_source", ""),
            "restore_target_factory": capture.get("private_target_staging_source", ""),
            "target_graph_validation_and_recapture": capture.get("target_owner_vector_validation_and_guard_recaputure_source", ""),
            "target_owner_validation": capture.get("target_sentinel_exact-owner-validation_source", ""),
            "restore_case_identity": owner_case,
            "accepted_role_and_state_disposition": capture.get("producer_zero_restore_scope_disposition", ""),
            "evidence_boundary": "Source-linked selected owner identity plus existing capture/restore crosswalk; only the grouped integrated cases identified in the Qualification validation record establish new P12-G behavior.",
        })
    if len(ledger) != 89 or len({row["section_or_prefix"] for row in ledger}) != 89:
        raise RuntimeError("The current owner-chain ledger is not 89 exact unique families")
    columns = list(ledger[0].keys())
    write_csv(output / "owner-entry-chain-ledger.csv", columns, ledger)
    return ledger, test_blob


def unity_destination(target: str) -> tuple[str, str]:
    if target == "UnityEngine.Mathf":
        return "Promoted host-neutral compatibility helper", "UnityMathfCompatibility in Packages/com.simulation.core/Runtime/Numerics/UnityMathfCompatibility.cs; exact operation signature is in target_symbol"
    if target.startswith("UnityEngine.Mathf."):
        name = target.removeprefix("UnityEngine.Mathf.").split("(", 1)[0]
        return "Promoted host-neutral compatibility helper", f"UnityMathfCompatibility.{name} with the same typed operands; no params overload is implied"
    if target == "UnityEngine.Application" or target.startswith(("UnityEngine.Application.", "UnityEngine.RuntimePlatform")):
        return "Unity host identity/path adapter", "Bind build identity, platform, version, or persistent-data roots at the approved host boundary; do not pass Unity Application into shared Core"
    if target.startswith("UnityEngine.Debug.") or target == "UnityEngine.Debug":
        return "Unity logging adapter", "Use the approved host-bound log callback; Core report/receipt state remains authoritative and callback results do not affect transitions"
    if target.startswith("UnityEngine.InputSystem."):
        return "Unity input adapter", "Translate Unity InputSystem state into immutable host input before calling DailyV1Application; input device types remain outside shared Core"
    if any(name in target for name in (
        "CreateAssetMenuAttribute", "HeaderAttribute", "RangeAttribute", "SerializeField",
        "SerializeReference", "ScriptableObject", "UnityEngine.MonoBehaviour",
    )):
        return "Unity authoring/lifecycle adapter", "Keep Unity asset serialization and MonoBehaviour lifecycle at the Unity edge; pass canonical immutable definitions/commands across the approved application API"
    if target in {
        "UnityEngine.Object",
        "UnityEngine.Object.operator !=(UnityEngine.Object, UnityEngine.Object)",
        "UnityEngine.Object.operator ==(UnityEngine.Object, UnityEngine.Object)",
    }:
        return "Unity object/fake-null adapter", "Evaluate UnityEngine.Object identity/null semantics at the authoring adapter before constructing shared Core inputs"
    raise RuntimeError("Unclassified Unity symbol: " + target)


def make_destination_ledgers(repo: pathlib.Path, output: pathlib.Path) -> tuple[int, int, str, str]:
    source_root = repo / "docs/validation/CORE_F_SourceClosure_PostInstitution"
    inventory = read_csv(source_root / "source-inventory.csv")
    split_sources = {
        row["source_path"]: row for row in inventory if row["disposition"] == "SplitHostDomain"
    }
    if len(split_sources) != 67:
        raise RuntimeError(f"Expected 67 SplitHostDomain sources, found {len(split_sources)}")
    access_archive = output / "member-access/selected-member-access-sites.csv.gz"
    if not access_archive.is_file():
        raise RuntimeError("The source-bound Roslyn member-access inventory is missing")
    with gzip.open(access_archive, "rt", encoding="utf-8", newline="") as stream:
        access_lines = list(csv.DictReader(stream))
    all_row_hashes = Counter()
    split_rows: list[dict[str, str]] = []
    grouped: dict[tuple[str, ...], list[dict[str, str]]] = defaultdict(list)
    for ordinal, row in enumerate(access_lines, start=1):
        raw_row = ",".join(row.get(column, "") for column in row.keys()).encode("utf-8")
        row_hash = sha256(raw_row)
        all_row_hashes[row_hash] += 1
        if row["source_path"] not in split_sources:
            continue
        if row["target_family"] == "Simulation.Core":
            destination = "Same Simulation.Core assembly"
            rationale = "Semantic Core member resolves to exact pinned Core source declaration; retain internal access within the shared assembly."
        elif row["target_family"] == "UnityEngine":
            destination, rationale = unity_destination(row["target_symbol"])
        else:
            raise RuntimeError("Unknown member-access target family: " + row["target_family"])
        declaration = row.get("declaration_sites", "")
        if row["target_family"] == "Simulation.Core" and not declaration:
            raise RuntimeError("A selected Core reference lacks its exact source declaration")
        key = (
            row["source_path"], row["caller_type"], row["caller_member"],
            row["target_family"], row["target_symbol_kind"], row["target_symbol"], destination,
        )
        group_id = sha256("\0".join(key).encode("utf-8"))[:20]
        item = {
            "input_row_ordinal": str(ordinal),
            "input_row_sha256": row_hash,
            "group_id": group_id,
            "source_path": row["source_path"],
            "source_blob": row["source_blob"],
            "source_line": row["source_line"],
            "caller_type": row["caller_type"],
            "caller_member": row["caller_member"],
            "caller_accessibility": row["caller_accessibility"],
            "target_family": row["target_family"],
            "target_symbol_kind": row["target_symbol_kind"],
            "target_symbol": row["target_symbol"],
            "target_accessibility": row["target_accessibility"],
            "target_containing_symbol": row.get("target_containing_symbol", ""),
            "target_declaration_sites": declaration,
            "destination": destination,
            "disposition_rationale": rationale,
            "reference_kind": row["reference_kind"],
        }
        split_rows.append(item)
        grouped[key].append(item)

    if len(access_lines) != 42625 or len(split_rows) != 18270:
        raise RuntimeError(f"Member-access row counts differ from the pinned closure: {len(access_lines)} total, {len(split_rows)} SplitHostDomain")
    group_rows: list[dict[str, str]] = []
    for key, rows in sorted(grouped.items()):
        group_rows.append({
            "group_id": rows[0]["group_id"],
            "source_path": key[0],
            "caller_type": key[1],
            "caller_member": key[2],
            "target_family": key[3],
            "target_symbol_kind": key[4],
            "target_symbol": key[5],
            "destination": key[6],
            "row_count": str(len(rows)),
            "input_row_ordinals": "|".join(row["input_row_ordinal"] for row in rows),
            "source_lines": "|".join(sorted({row["source_line"] for row in rows}, key=int)),
            "target_declaration_sites": "|".join(sorted({row["target_declaration_sites"] for row in rows if row["target_declaration_sites"]})),
            "disposition_rationale": rows[0]["disposition_rationale"],
        })
    if not split_rows or sum(int(row["row_count"]) for row in group_rows) != len(split_rows):
        raise RuntimeError("Split-host grouping lost or duplicated input rows")
    if len(group_rows) != 3365:
        raise RuntimeError(f"Source-linked split-host group count differs from the audited cut: {len(group_rows)}")
    write_csv(output / "split-host-destination-rows.csv", list(split_rows[0].keys()), split_rows)
    write_csv(output / "split-host-destination-groups.csv", list(group_rows[0].keys()), group_rows)
    raw_hash = sha256(gzip.decompress(access_archive.read_bytes()))
    compressed_hash = sha256(access_archive.read_bytes())
    return len(split_rows), len(group_rows), raw_hash, compressed_hash


def main() -> int:
    repo = pathlib.Path(__file__).resolve().parents[4]
    output = pathlib.Path(__file__).resolve().parent.parent
    remote_core = run_git(repo, "rev-parse", "refs/remotes/origin/codex/core/canonical")
    if remote_core != CORE_CANONICAL_BASE:
        raise RuntimeError("Current remote Core canonical differs from the approved owner-entry base")
    merge_base = run_git(repo, "merge-base", CORE_CANONICAL_BASE, "HEAD")
    if merge_base != CORE_CANONICAL_BASE:
        raise RuntimeError("The current evidence branch is not based on the approved Core canonical")
    allowed_delta = matching_source_deltas(repo)
    unexpected = sorted(allowed_delta - APPROVED_TEST_DELTAS)
    if unexpected:
        raise RuntimeError("Production or unrelated source changed outside the approved test-only set: " + ", ".join(unexpected))

    arch_file = run_git(repo, "show", f"{ARCHITECTURE_CONTRACT}:{OWNER_DISPOSITIONS_PATH}")
    dispositions = list(csv.DictReader(arch_file.splitlines()))
    required_rows = [row for row in dispositions if row.get("initial_or_write_reach", "").split(";", 1)[0].strip() == "Required"]
    if len(dispositions) != 18 or len(required_rows) != 18:
        raise RuntimeError("The promoted architecture disposition ledger must preserve all 18 Required rows")
    contract_blob = run_git(repo, "rev-parse", f"{ARCHITECTURE_CONTRACT}:{ARCHITECTURE_CONTRACT_PATH}")
    disposition_blob = run_git(repo, "rev-parse", f"{ARCHITECTURE_CONTRACT}:{OWNER_DISPOSITIONS_PATH}")
    definition_binding_path = "docs/architecture/CORE_F_DEFINITION_BINDING_TECHNICAL_DESIGN.md"
    optional_binding_path = "docs/architecture/CORE_F_OPTIONAL_DEFINITION_BINDING_DESIGN.md"
    definition_binding_blob = run_git(repo, "rev-parse", f"{ARCHITECTURE_CONTRACT}:{definition_binding_path}")
    optional_binding_blob = run_git(repo, "rev-parse", f"{ARCHITECTURE_CONTRACT}:{optional_binding_path}")

    chain_rows, identity_test_blob = make_owner_ledger(repo, output)
    split_count, group_count, access_raw_hash, access_gzip_hash = make_destination_ledgers(repo, output)
    source_baseline_commit = run_git(repo, "rev-parse", "HEAD")
    source_baseline_tree = run_git(repo, "rev-parse", "HEAD^{tree}")
    design_reference = (
        "# Definition-binding design attached to the member ledger\n\n"
        f"Architecture source: {ARCHITECTURE_CONTRACT}.\n\n"
        f"- Selected owner and shared graph boundary: {definition_binding_path} (blob {definition_binding_blob}).\n"
        f"- Selected site and optional local descriptors: {optional_binding_path} (blob {optional_binding_blob}).\n\n"
        "split-host-destination-groups.csv is the source-linked member ledger: each grouped caller/target row "
        "retains its concrete source/member, exact current target signature, declaration sites, destination, and "
        "links to every original compiler row by ordinal and input-row hash. Read the two design documents above "
        "for the approved future constructor/accessor signatures and immutable definition-node bindings. This "
        "qualification records current ownership and access destinations only; it does not implement those "
        "definition APIs, create stubs for empty selected content, or mark the Shared Runtime/Application/Host "
        "ready for implementation.\n"
    )
    (output / "member-access/DEFINITION_BINDING_DESIGN.md").write_text(design_reference, encoding="utf-8")
    test_ids = {
        "military_and_five_site_adventure_knowledge": ("Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs", "IntegratedMilitaryAndSiteKnowledgeOwnersPersistRestoreAndContinueWithExactParity"),
        "integrated_rejection_and_retry": ("Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs", "IntegratedOwnerImageRejectionIsAtomicAndValidImageCanBeRetried"),
        "general_p9_site_daily_rejection": ("Assets/_Project/Tests/EditMode/Editor/P12GOwnerEntryQualificationTests.cs", "GenericP9SiteProfileValidatesButDailyV1AdmissionDoesNotPublishIt"),
        "local_topology_place_capture_rejection": ("Assets/_Project/Tests/EditMode/Editor/P12DNpcRootOwnerSnapshotTests.cs", "Capture_RejectsDailyV1LocalTopologyKnowledge"),
        "local_topology_connection_capture_rejection": ("Assets/_Project/Tests/EditMode/Editor/P12DNpcRootOwnerSnapshotTests.cs", "Capture_RejectsDailyV1LocalTopologyConnectionKnowledge"),
        "expedition_foreign_site_identity_guard": ("Assets/_Project/Tests/EditMode/Editor/ExpeditionTests.cs", ""),
        "political_existing_integrated_round_trip": ("Assets/_Project/Tests/EditMode/Editor/P12GPoliticalBindingRestoreTests.cs", "PopulatedPoliticalOwnersRoundTripFromRealAdvanceAndContinueWithExactParity"),
    }
    identity_source_path = "Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs"
    case_sources = {path: (repo / path).read_text(encoding="utf-8-sig") for path, _ in test_ids.values()}
    identity_source = (repo / identity_source_path).read_text(encoding="utf-8-sig")
    for label, (case_path, case_name) in test_ids.items():
        source_text = case_sources[case_path]
        if label == "expedition_foreign_site_identity_guard":
            match = re.search(r"public\s+void\s+(\w+)\s*\([^)]*\)\s*\{[^}]*foreignSite", source_text, re.S)
            if not match:
                raise RuntimeError("Missing required grouped test case: " + label + " in " + case_path)
            test_ids[label] = (case_path, match.group(1))
        elif case_name not in source_text:
            raise RuntimeError("Missing required grouped test case: " + label + " => " + case_name + " in " + case_path)

    identity_oracle_checks = {
        "omission_and_same_count_symbol_substitution": "protocolExpectedIds, Is.EqualTo(expectedIds)",
        "registered_key_set": "registeredIds, Is.EqualTo(expectedIds)",
        "duplicate_key_rejection": "expected.ContainsKey(sectionId), Is.False",
        "provider_owner_identity": "witness.OwnerInstanceIdentity, Is.SameAs(expected.Value)",
        "role_binding": "registeredContract.Role, Is.EqualTo(expectedRole)",
        "schema_binding": "registeredContract.SchemaVersion, Is.EqualTo(expectedContract.SchemaVersion)",
        "revision_stability": "repeated.Revision, Is.EqualTo(witness.Revision)",
        "initial_zero_only_boundary": "Assert only this initial profile boundary",
        "documented_dynamic_cardinality": "61 + (22 * npcCount)",
    }
    for label, assertion in identity_oracle_checks.items():
        if assertion not in identity_source:
            raise RuntimeError("Selected-profile owner identity oracle is missing " + label + ": " + assertion)

    files = [
        "owner-entry-chain-ledger.csv",
        "split-host-destination-rows.csv",
        "split-host-destination-groups.csv",
        "member-access/selected-member-access-sites.csv.gz",
        "member-access/selected-member-access-sites.manifest.txt",
        "member-access/DEFINITION_BINDING_DESIGN.md",
    ]
    source_identity = {}
    for relative_path in sorted(allowed_delta):
        path = repo / relative_path
        if not path.is_file():
            raise RuntimeError("A selected test/source delta is unavailable for identity binding: " + relative_path)
        source_identity[relative_path] = {
            "gitBlob": run_git(repo, "hash-object", "--", relative_path),
            "sha256": sha256(path.read_bytes()),
            "bytes": path.stat().st_size,
        }
    tooling_sources = {}
    for relative_path in (
        "docs/validation/CORE_F_OwnerEntryQualification/audit-source/OwnerEntryQualification.py",
        "docs/validation/CORE_F_SourceClosure_PostInstitution/audit-source/MemberAccessCut.cs",
        "docs/validation/CORE_F_SourceClosure_PostInstitution/audit-source/MemberAccessCut.csproj",
    ):
        path = repo / relative_path
        tooling_sources[relative_path] = {
            "gitBlob": run_git(repo, "hash-object", "--", relative_path),
            "sha256": sha256(path.read_bytes()),
            "bytes": path.stat().st_size,
        }
    manifest = {
        "format": "CORE-F-OWNER-ENTRY-QUALIFICATION-v1",
        "sourceBaselineCommit": source_baseline_commit,
        "sourceBaselineTree": source_baseline_tree,
        "candidateCommit": source_baseline_commit,
        "candidateTree": source_baseline_tree,
        "coreCanonicalBase": CORE_CANONICAL_BASE,
        "remoteCoreCanonical": remote_core,
        "architectureContractCommit": ARCHITECTURE_CONTRACT,
        "architectureContractPath": ARCHITECTURE_CONTRACT_PATH,
        "architectureContractBlob": contract_blob,
        "definitionBindingDesign": {
            "architectureCommit": ARCHITECTURE_CONTRACT,
            "path": definition_binding_path,
            "blob": definition_binding_blob,
            "optionalDescriptorPath": optional_binding_path,
            "optionalDescriptorBlob": optional_binding_blob,
            "readiness": "Design-only; shared Runtime/Application/Host remains WAIT_DEPENDENCY until the complete reviewed integration package is separately ready.",
        },
        "ownerDispositionsPath": OWNER_DISPOSITIONS_PATH,
        "ownerDispositionsBlob": disposition_blob,
        "ownerDispositionRows": len(dispositions),
        "sourceReferenceCommit": SOURCE_REFERENCE,
        "ownerRows": len(chain_rows),
        "selectedIdentityMapTestBlob": identity_test_blob,
        "selectedIdentityOracleChecks": identity_oracle_checks,
        "approvedTestSourceIdentities": source_identity,
        "toolingSourceIdentities": tooling_sources,
        "splitHostSourceCount": 67,
        "splitHostAccessRows": split_count,
        "splitHostAccessGroups": group_count,
        "memberAccessRows": 42625,
        "memberAccessRawCsvSha256": access_raw_hash,
        "memberAccessGzipSha256": access_gzip_hash,
        "newGroupedCases": test_ids,
        "files": {name: {"sha256": sha256((output / name).read_bytes()), "bytes": (output / name).stat().st_size} for name in files},
        "claims": [
            "All 18 no-writer dispositions remain Required and qualify only by the promoted architecture's preservation/restriction and supported-operation conservation rules.",
            "The 89 owner chains combine current owner/writer/capture matrices with exact selected-profile live identity expressions; the grouped cases add only the approved integrated P12-G evidence.",
            "The split-host destination ledger links every selected input row by ordinal and row hash and resolves all Simulation.Core targets to source declarations.",
            "This evidence does not move production source, create writers, widen public APIs, or declare Shared Runtime/Host implementation ready by itself.",
        ],
    }
    manifest_path = output / "qualification-manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(
        f"owner_rows={len(chain_rows)} owner_dispositions={len(dispositions)} split_sources=67 "
        f"split_access_rows={split_count} split_groups={group_count} total_access_rows=42625 "
        f"source_deltas={len(allowed_delta)} contract_blob={contract_blob} manifest_sha256={sha256(manifest_path.read_bytes())}"
    )
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as error:  # fail closed and keep the diagnostic source-relative
        print(f"OwnerEntryQualification failed: {error}", file=sys.stderr)
        sys.exit(1)

#!/usr/bin/env python3
"""Emit fixed C# selected-content constructor data from the retained Unity member capture.

Inputs are the independent `member=` lines in CORE_F_WP1_VALUES_BASELINE.txt and the
reviewed CORE_F_SELECTED_VALUE_MEMBERS.csv schema. The captured P12 catalog bytes are
never read. Run from the repository root with:
  py tools/generate_daily_v1_captured_content_fixture.py
"""
from __future__ import annotations

import base64
from collections import Counter
import csv
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "tests/Simulation.Core.Tests/Fixtures/CORE_F_WP1_VALUES_BASELINE.txt"
SCHEMA = ROOT / "docs/architecture/CORE_F_SELECTED_VALUE_MEMBERS.csv"
OUTPUT = ROOT / "Assets/_Project/Tests/EditMode/Editor/Core/DailyV1CapturedSelectedContentFixture.cs"
EXPECTED_BASELINE_SHA = "422805cbe373cf1410ee6a928d363fa1ef413dbcf50dc062dce7867ed4fdd1b3"
EXPECTED_SCHEMA_ROWS = 164
ASSET_OWNERS = ("CityData", "ItemData", "NpcData", "NpcJobData", "NpcActionData", "NpcStatusData")


def cs_string(value: str) -> str:
    return json.dumps(value, ensure_ascii=True)


def type_token(source_type: str) -> str:
    primitives = {"bool": "System.Boolean", "int": "System.Int32", "long": "System.Int64",
                  "float": "System.Single", "double": "System.Double", "string": "System.String"}
    if source_type in primitives:
        return primitives[source_type]
    if source_type.startswith("List<"):
        element = source_type[5:-1]
        element_token = type_token(element)
        assembly = ("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
                    if element == "int" else
                    "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
        return f"System.Collections.Generic.List`1[[{element_token}, {assembly}]]"
    return source_type


def split_top(value: str, separator: str) -> list[str]:
    result, start = [], 0
    square = curly = paren = 0
    for i, ch in enumerate(value):
        if ch == "[": square += 1
        elif ch == "]": square -= 1
        elif ch == "{": curly += 1
        elif ch == "}": curly -= 1
        elif ch == "(": paren += 1
        elif ch == ")": paren -= 1
        elif ch == separator and square == curly == paren == 0:
            result.append(value[start:i])
            start = i + 1
    if start < len(value):
        result.append(value[start:])
    return result


def scalar_string(raw: str) -> str:
    payload = raw.split(":", 1)[1]
    return base64.b64decode(payload, validate=True).decode("utf-8", errors="strict")


def collect_occurrences(raw: str, source_type: str, schema: dict[str, dict[str, str]],
                        result: dict[tuple[str, str], list[str]]) -> None:
    if raw.startswith("value<"):
        boundary = raw.find("{")
        owner = raw[6:boundary - 1]
        if owner != source_type or owner not in schema:
            raise ValueError(f"Captured nested object does not match schema: {owner}, {source_type}")
        for field in split_top(raw[boundary + 1:-1], ";"):
            if not field:
                continue
            name, encoded = field.split("=", 1)
            field_type = schema[owner][name]
            result.setdefault((owner, name), []).append(encoded)
            collect_occurrences(encoded, field_type, schema, result)
    elif source_type.startswith("List<"):
        boundary = raw.find(">[")
        if boundary < 0:
            raise ValueError(f"Malformed captured list while checking occurrences: {raw[:100]}")
        element = source_type[5:-1]
        for item in split_top(raw[boundary + 2:-1], ";"):
            if item:
                collect_occurrences(item, element, schema, result)


def value_expr(raw: str, source_type: str, schema: dict[str, dict[str, str]], depth=0) -> str:
    if depth > 64:
        raise ValueError("Captured value nesting exceeds the reviewed depth bound")
    token = cs_string(type_token(source_type))
    if raw == "null":
        return f"DailyV1SelectedContentValue.Null({token})"
    if raw.startswith("asset("):
        owner, key = raw[6:-1].split(",", 1)
        if owner != source_type:
            raise ValueError(f"Reference token mismatch: {raw} versus {source_type}")
        return f"DailyV1SelectedContentValue.Reference({token}, {cs_string(owner + '/' + key)})"
    if raw.startswith("enum("):
        enum_type, numeric, spelling = raw[5:-1].split(",", 2)
        if enum_type != source_type:
            raise ValueError(f"Enum token mismatch: {raw} versus {source_type}")
        return f"DailyV1SelectedContentValue.Enum({token}, {cs_string(spelling)})"
    if source_type.startswith("List<"):
        boundary = raw.find(">[")
        if not raw.startswith("list<") or boundary < 0 or not raw.endswith("]"):
            raise ValueError(f"Malformed captured list: {raw[:100]}")
        items = split_top(raw[boundary + 2:-1], ";")
        element = source_type[5:-1]
        entries = ",\n".join("            " + value_expr(item, element, schema, depth + 1)
                               for item in items if item)
        return f"DailyV1SelectedContentValue.List({token}, new DailyV1SelectedContentValue[] {{\n{entries}\n        }})"
    if raw.startswith("value<"):
        boundary = raw.find("{")
        object_type = raw[6:boundary - 1]
        if object_type != source_type or object_type not in schema:
            raise ValueError(f"Object token mismatch or missing schema: {object_type}, {source_type}")
        fields = []
        for row in split_top(raw[boundary + 1:-1], ";"):
            if not row:
                continue
            name, encoded = row.split("=", 1)
            field_type = schema[object_type][name]
            fields.append(f"            new DailyV1SelectedContentField({cs_string(object_type)}, "
                          f"{cs_string(name)}, {value_expr(encoded, field_type, schema, depth + 1)})")
        return "DailyV1SelectedContentValue.Object(" + token + ", new DailyV1SelectedContentField[] {\n" + ",\n".join(fields) + "\n        })"

    scalar = raw.split(":", 1)[1]
    if source_type == "string":
        return f"DailyV1SelectedContentValue.Scalar({token}, {cs_string(base64.b64decode(scalar, validate=True).decode('utf-8', 'strict'))})"
    if source_type == "bool":
        return f"DailyV1SelectedContentValue.Scalar({token}, {scalar.lower()})"
    if source_type in ("int", "long"):
        return f"DailyV1SelectedContentValue.Scalar({token}, {scalar}{'L' if source_type == 'long' else ''})"
    if source_type == "float":
        bits = int(scalar[2:], 16) if scalar.startswith("0x") else None
        if bits is None:
            import struct
            bits = struct.unpack("<I", struct.pack("<f", float(scalar)))[0]
        return f"DailyV1SelectedContentValue.Scalar({token}, F32(0x{bits:08x}u))"
    if source_type == "double":
        if scalar.startswith("0x"):
            bits = int(scalar[2:], 16)
        else:
            import struct
            bits = struct.unpack("<Q", struct.pack("<d", float(scalar)))[0]
        return f"DailyV1SelectedContentValue.Scalar({token}, F64(0x{bits:016x}ul))"
    raise ValueError(f"Unsupported captured type {source_type}: {raw[:100]}")


def parse_value(raw: str, source_type: str, schema: dict[str, dict[str, str]], depth=0):
    if depth > 64:
        raise ValueError("Captured value nesting exceeds the reviewed depth bound")
    if raw == "null":
        return ("null", source_type, None)
    if raw.startswith("asset("):
        owner, key = raw[6:-1].split(",", 1)
        if owner != source_type:
            raise ValueError(f"Reference token mismatch: {raw} versus {source_type}")
        return ("reference", source_type, key)
    if raw.startswith("enum("):
        enum_type, numeric, spelling = raw[5:-1].split(",", 2)
        if enum_type != source_type:
            raise ValueError(f"Enum token mismatch: {raw} versus {source_type}")
        return ("enum", source_type, (int(numeric), spelling))
    if source_type.startswith("List<"):
        boundary = raw.find(">[")
        if boundary < 0 or not raw.endswith("]"):
            raise ValueError(f"Malformed captured list: {raw[:100]}")
        element = source_type[5:-1]
        return ("list", source_type, [parse_value(item, element, schema, depth + 1)
                                      for item in split_top(raw[boundary + 2:-1], ";") if item])
    if raw.startswith("value<"):
        boundary = raw.find("{")
        owner = raw[6:boundary - 1]
        if owner != source_type or owner not in schema:
            raise ValueError(f"Object token mismatch or missing schema: {owner}, {source_type}")
        children = {}
        for field in split_top(raw[boundary + 1:-1], ";"):
            if not field:
                continue
            name, encoded = field.split("=", 1)
            children[name] = parse_value(encoded, schema[owner][name], schema, depth + 1)
        return ("object", owner, children)
    scalar = raw.split(":", 1)[1]
    if source_type == "string":
        scalar = base64.b64decode(scalar, validate=True).decode("utf-8", "strict")
    elif source_type == "bool":
        scalar = scalar.lower() == "true"
    elif source_type in ("int", "long"):
        scalar = int(scalar, 10)
    elif source_type == "float":
        if scalar.startswith("0x"):
            scalar = int(scalar[2:], 16)
        else:
            import struct
            scalar = struct.unpack("<I", struct.pack("<f", float(scalar)))[0]
    elif source_type == "double":
        if scalar.startswith("0x"):
            scalar = int(scalar[2:], 16)
        else:
            import struct
            scalar = struct.unpack("<Q", struct.pack("<d", float(scalar)))[0]
    else:
        raise ValueError(f"Unsupported captured type {source_type}: {raw[:100]}")
    return ("scalar", source_type, scalar)


def scalar_expr(node) -> str:
    kind, source_type, value = node
    if kind == "null":
        return "null"
    if kind == "reference":
        return cs_string(value)
    if kind == "scalar":
        if source_type == "string": return cs_string(value)
        if source_type == "bool": return "true" if value else "false"
        if source_type == "long": return f"{value}L"
        if source_type == "float": return f"F32(0x{value:08x}u)"
        if source_type == "double": return f"F64(0x{value:016x}ul)"
        return str(value)
    raise ValueError(f"Expected a scalar/reference, found {kind} {source_type}")


def enum_expr(node) -> str:
    if node[0] == "null": return "null"
    if node[0] != "enum": raise ValueError(f"Expected enum value, got {node[0]}")
    numeric, spelling = node[2]
    return f"new DailyV1EnumValue({numeric}, {cs_string(spelling)})"


def list_expr(node, converter, empty_type: str) -> str:
    if node[0] == "null": return "null"
    if node[0] != "list": raise ValueError(f"Expected list value, got {node[0]}")
    if not node[2]: return f"Array.Empty<{empty_type}>()"
    return "new[] { " + ", ".join(converter(item) for item in node[2]) + " }"


def object_expr(node, name: str) -> str:
    if node[0] == "null": return "null"
    if node[0] != "object" or node[1] != name:
        raise ValueError(f"Expected {name} object, got {node[0]} {node[1]}")
    return node[2]


def typed_expr(node, owner: str) -> str:
    if node[0] == "null": return "null"
    if owner in ("CalendarDefinition", "CommercialKnowledgeSettings", "CrimeActionSettings",
                 "MarketLiquidityConfig", "PopulationConsumptionConfig", "SimulationLogSettings"):
        f = object_expr(node, owner)
        if owner == "CalendarDefinition":
            args = [scalar_expr(f[x]) for x in ("monthsPerYear", "weeksPerMonth", "daysPerWeek")]
            args.append(list_expr(f["monthLengths"], scalar_expr, "int"))
            return "new DailyV1CalendarDefinition(" + ", ".join(args) + ")"
        if owner == "CommercialKnowledgeSettings":
            names = ("freshForDays", "maxUsefulAgeDays", "maxSharedObservationsPerInteraction")
            ctor = "DailyV1CommercialKnowledgeSettings"
        elif owner == "CrimeActionSettings":
            names = ("amount", "bounty", "sentenceDays", "hiddenDays", "escapeBountyPenalty", "failedEscapeSentencePenalty")
            ctor = "DailyV1CrimeActionSettings"
        elif owner == "MarketLiquidityConfig":
            names = ("liquidityMode", "initialPurchasingPower")
            return "new DailyV1MarketLiquidityConfig(" + enum_expr(f[names[0]]) + ", " + scalar_expr(f[names[1]]) + ")"
        elif owner == "PopulationConsumptionConfig":
            names = ("paymentMode", "initialPurchasingPower")
            return "new DailyV1PopulationConsumptionConfig(" + enum_expr(f[names[0]]) + ", " + scalar_expr(f[names[1]]) + ")"
        else:
            names = ("showDay", "npcActions", "trade", "travel", "crime", "justice", "economyProduction", "economyConsumption", "market")
            ctor = "DailyV1SimulationLogSettings"
        return f"new {ctor}(" + ", ".join(scalar_expr(f[x]) for x in names) + ")"
    if owner == "CapabilityAttributeModifier":
        f = object_expr(node, owner)
        return "new DailyV1CapabilityAttributeModifier(" + scalar_expr(f["attribute"]) + ", " + scalar_expr(f["additiveValue"]) + ")"
    if owner == "CapabilityAttributeValue":
        f = object_expr(node, owner)
        return "new DailyV1CapabilityAttributeValue(" + scalar_expr(f["attribute"]) + ", " + scalar_expr(f["value"]) + ")"
    if owner == "CityConnection":
        f = object_expr(node, owner)
        return "new DailyV1CityConnection(" + scalar_expr(f["destination"]) + ", " + scalar_expr(f["travelDays"]) + ")"
    if owner == "CityProductionConfig":
        f = object_expr(node, owner)
        names = ("item", "amountPerDay", "initialReserve", "productionSourceId", "contentRevision")
        args = [scalar_expr(f[x]) for x in names] + [enum_expr(f["sourceKind"])]
        return "new DailyV1CityProductionConfig(" + ", ".join(args) + ")"
    if owner == "MarketItemConfig":
        f = object_expr(node, owner)
        names = ("item", "initialAmount", "desiredAmount", "consumptionPer1000Population")
        return "new DailyV1MarketItemConfig(" + ", ".join(scalar_expr(f[x]) for x in names) + ")"
    if owner == "CityData":
        f = object_expr(node, owner)
        names = ("id", "cityName", "settlementSemanticId", "materialFlowLocationId", "marketStoreSemanticId")
        args = [scalar_expr(f[x]) for x in names] + [enum_expr(f["materialFlowProfile"]), scalar_expr(f["initialPopulation"]),
            typed_expr(f["marketLiquidity"], "MarketLiquidityConfig"),
            typed_expr(f["populationConsumption"], "PopulationConsumptionConfig"),
            list_expr(f["marketItems"], lambda x: typed_expr(x, "MarketItemConfig"), "DailyV1MarketItemConfig"),
            list_expr(f["productionConfigs"], lambda x: typed_expr(x, "CityProductionConfig"), "DailyV1CityProductionConfig"),
            list_expr(f["connections"], lambda x: typed_expr(x, "CityConnection"), "DailyV1CityConnection")]
        return "new DailyV1CityData(" + ", ".join(args) + ")"
    if owner == "ItemData":
        f = object_expr(node, owner)
        args = [scalar_expr(f[x]) for x in ("id", "itemName", "basePrice")]
        args.append(list_expr(f["capabilityModifiers"], lambda x: typed_expr(x, "CapabilityAttributeModifier"), "DailyV1CapabilityAttributeModifier"))
        return "new DailyV1ItemData(" + ", ".join(args) + ")"
    if owner == "NPCDefaultAction":
        f = object_expr(node, owner)
        return "new DailyV1NPCDefaultAction(" + scalar_expr(f["action"]) + ", " + scalar_expr(f["baseUtility"]) + ")"
    if owner == "NpcInitialInventoryItemConfig":
        f = object_expr(node, owner)
        return "new DailyV1NpcInitialInventoryItemConfig(" + ", ".join(scalar_expr(f[x]) for x in ("item", "amount", "averageUnitCost")) + ")"
    if owner == "NpcJobData":
        f = object_expr(node, owner)
        names = ("id", "jobName")
        args = [scalar_expr(f[x]) for x in names] + [enum_expr(f["jobType"]), enum_expr(f["merchantBehavior"])]
        args += [scalar_expr(f[x]) for x in ("minimumProfitPerItem", "workAction", "workUtility")]
        args.append(list_expr(f["preferredTradeItems"], lambda x: typed_expr(x, "TradeItemPreference"), "DailyV1TradeItemPreference"))
        return "new DailyV1NpcJobData(" + ", ".join(args) + ")"
    if owner == "TradeItemPreference":
        f = object_expr(node, owner)
        return "new DailyV1TradeItemPreference(" + scalar_expr(f["item"]) + ", " + scalar_expr(f["utilityMultiplier"]) + ")"
    if owner == "NpcSimulationConfig":
        f = object_expr(node, owner)
        args = [scalar_expr(f[x]) for x in ("npc", "startingCity", "initialMoney")]
        args.append(list_expr(f["initialInventory"], lambda x: typed_expr(x, "NpcInitialInventoryItemConfig"), "DailyV1NpcInitialInventoryItemConfig"))
        args.append(list_expr(f["initialKnownExplorableSites"], scalar_expr, "string"))
        return "new DailyV1NpcSimulationConfig(" + ", ".join(args) + ")"
    if owner == "NpcStatusData":
        f = object_expr(node, owner)
        return "new DailyV1NpcStatusData(" + scalar_expr(f["id"]) + ", " + scalar_expr(f["statusName"]) + ")"
    if owner == "StatusWeightModifier":
        f = object_expr(node, owner)
        return "new DailyV1StatusWeightModifier(" + scalar_expr(f["status"]) + ", " + scalar_expr(f["multiplier"]) + ")"
    if owner == "NpcActionData":
        f = object_expr(node, owner)
        names = ("id", "statusNecessariosParaFazerAcao", "actionName", "normalActionLogText")
        args = [scalar_expr(f["id"]), list_expr(f["statusNecessariosParaFazerAcao"], scalar_expr, "string"),
            scalar_expr(f["actionName"]), scalar_expr(f["normalActionLogText"]), enum_expr(f["actionCategory"]),
            enum_expr(f["actionType"]), scalar_expr(f["baseUtility"]),
            list_expr(f["statusModifiers"], lambda x: typed_expr(x, "StatusWeightModifier"), "DailyV1StatusWeightModifier"),
            scalar_expr(f["canFail"]), scalar_expr(f["baseSuccessChance"])]
        args += [list_expr(f[x], scalar_expr, "string") for x in ("statusToAdd", "statusToRemove", "targetStatusToAdd", "targetStatusToRemove")]
        args.append(typed_expr(f["crimeSettings"], "CrimeActionSettings"))
        return "new DailyV1NpcActionData(" + ", ".join(args) + ")"
    if owner == "NpcData":
        f = object_expr(node, owner)
        args = [scalar_expr(f[x]) for x in ("id", "name", "job")]
        args += [list_expr(f["acoesPadrao"], lambda x: typed_expr(x, "NPCDefaultAction"), "DailyV1NPCDefaultAction"),
            list_expr(f["statusPadrao"], scalar_expr, "string"),
            list_expr(f["capabilityValues"], lambda x: typed_expr(x, "CapabilityAttributeValue"), "DailyV1CapabilityAttributeValue"),
            list_expr(f["traits"], scalar_expr, "string")]
        return "new DailyV1NpcData(" + ", ".join(args) + ")"
    if owner == "ExplorableSiteConfig":
        f = object_expr(node, owner)
        if f["site"][0] != "null":
            raise ValueError("Non-empty explorable-site graph needs a captured site-kind owner row")
        return "new DailyV1ExplorableSiteConfig(null, " + scalar_expr(f["anchorCity"]) + ", " + scalar_expr(f["travelDaysFromAnchor"]) + ", null)"
    if owner == "InitialWantedRecordConfig":
        f = object_expr(node, owner)
        return "new DailyV1InitialWantedRecordConfig(" + ", ".join(scalar_expr(f[x]) for x in ("target", "city", "bounty", "sentenceDays")) + ")"
    if owner == "ScheduledDirectiveConfig":
        f = object_expr(node, owner)
        args = [scalar_expr(f["absoluteDay"]), enum_expr(f["mode"]), enum_expr(f["operation"]), scalar_expr(f["actor"]), scalar_expr(f["action"])]
        return "new DailyV1ScheduledDirectiveConfig(" + ", ".join(args) + ")"
    if owner == "SimulationConfigData":
        f = object_expr(node, owner)
        args = [scalar_expr(f["simulationName"]), typed_expr(f["calendar"], "CalendarDefinition"),
            list_expr(f["enabledModules"], enum_expr, "DailyV1EnumValue"), list_expr(f["cities"], scalar_expr, "string"),
            list_expr(f["explorableSites"], lambda x: typed_expr(x, "ExplorableSiteConfig"), "DailyV1ExplorableSiteConfig"),
            list_expr(f["npcs"], lambda x: typed_expr(x, "NpcSimulationConfig"), "DailyV1NpcSimulationConfig"),
            list_expr(f["actions"], scalar_expr, "string"), list_expr(f["statuses"], scalar_expr, "string"), list_expr(f["jobs"], scalar_expr, "string"),
            list_expr(f["initialWarrants"], lambda x: typed_expr(x, "InitialWantedRecordConfig"), "DailyV1InitialWantedRecordConfig"),
            list_expr(f["scheduledDirectives"], lambda x: typed_expr(x, "ScheduledDirectiveConfig"), "DailyV1ScheduledDirectiveConfig")]
        ordered = ("useAuthoredGeographyProfile", "authoredHexId", "authoredHexQ", "authoredHexR",
            "authoredTerrainDefinitionId", "authoredTerrainRevisionToken", "authoredLocationId", "authoredScaleConventionId",
            "authoredScaleSourceIdentity", "authoredScaleSourceVersion", "authoredDistancePerNeighborStep", "authoredScaleUnit")
        args += [scalar_expr(f[x]) for x in ordered]
        args += [scalar_expr(f["authoredP10RuinSite"]), scalar_expr(f["genesisProfileContractIdentity"]), scalar_expr(f["p10bStableSiteKey"])]
        args += [scalar_expr(f[x]) for x in ("travelCostPerDay", "allowMerchantTradeRepositioning", "maxMerchantTradeAmount",
            "localMerchantWholesalePriceMultiplier", "localMerchantReserveRatio", "maxUnprofitablePlanWaitDays", "crimeAutonomousEnabled")]
        args += [typed_expr(f["commercialKnowledge"], "CommercialKnowledgeSettings")]
        args += [scalar_expr(f[x]) for x in ("useFixedSimulationSeed", "simulationSeed", "includeEconomySnapshots", "economySnapshotIntervalDays")]
        args += [typed_expr(f["logSettings"], "SimulationLogSettings")]
        args += [scalar_expr(f[x]) for x in ("naturalMortalityEnabled", "naturalMortalityAnnualProbability", "aggregateDemographyEnabled",
            "aggregateAnnualBirthRate", "aggregateAnnualDeathRate", "freeStatus", "wantedStatus", "arrestedStatus", "hiddenStatus")]
        return "new DailyV1SimulationConfigData(" + ", ".join(args) + ")"
    raise ValueError(f"No reviewed typed constructor mapping for {owner}")


def main() -> None:
    baseline_bytes = BASELINE.read_bytes()
    baseline_sha = hashlib.sha256(baseline_bytes).hexdigest()
    if baseline_sha != EXPECTED_BASELINE_SHA:
        raise SystemExit(f"Baseline SHA mismatch: {baseline_sha}")
    schema_rows = list(csv.DictReader(SCHEMA.open(encoding="utf-8", newline="")))
    if len(schema_rows) != EXPECTED_SCHEMA_ROWS:
        raise SystemExit(f"Expected {EXPECTED_SCHEMA_ROWS} reviewed schema rows, found {len(schema_rows)}")
    schema: dict[str, dict[str, str]] = {}
    for row in schema_rows:
        schema.setdefault(row["declaring_type"], {})[row["member"]] = row["source_type"]

    fields: dict[str, dict[str, list[str]]] = {}
    members = 0
    for line in baseline_bytes.decode("utf-8", errors="strict").splitlines():
        if not line.startswith("member="):
            continue
        text = base64.b64decode(line[7:], validate=True).decode("utf-8", errors="strict")
        left, raw = text.split("=", 1)
        owner, member = left.split(".", 1)
        fields.setdefault(owner, {}).setdefault(member, []).append(raw)
        members += 1
    if members != 718:
        raise SystemExit(f"Expected 718 captured occurrences, found {members}")

    # Prove that profile/asset constructor inputs account for every separately captured
    # member occurrence, including nested inline value objects and lists.
    observed: dict[tuple[str, str], list[str]] = {}
    for owner in ("SimulationConfigData",) + ASSET_OWNERS:
        for member, source_type in schema[owner].items():
            for raw in fields.get(owner, {}).get(member, []):
                observed.setdefault((owner, member), []).append(raw)
                collect_occurrences(raw, source_type, schema, observed)
    expected = {(owner, member): Counter(values)
                for owner, members_by_name in fields.items()
                for member, values in members_by_name.items()}
    actual = {key: Counter(values) for key, values in observed.items()}
    if expected != actual:
        missing = sum((expected.get(key, Counter()) - actual.get(key, Counter())).total()
                      for key in expected.keys() | actual.keys())
        extra = sum((actual.get(key, Counter()) - expected.get(key, Counter())).total()
                    for key in expected.keys() | actual.keys())
        raise SystemExit(f"Captured value walk failed 718-row field review: missing={missing}, extra={extra}")

    def object_expression(owner: str, index: int, key: str) -> str:
        entries = []
        for member, source_type in schema[owner].items():
            occurrences = fields.get(owner, {}).get(member, [])
            if index >= len(occurrences):
                raise ValueError(f"Missing captured occurrence {owner}.{member}[{index}]")
            entries.append(f"            new DailyV1SelectedContentField({cs_string(owner)}, {cs_string(member)}, "
                           f"{value_expr(occurrences[index], source_type, schema)})")
        return (f"new DailyV1SelectedContentAsset({cs_string(key)}, {cs_string(owner)}, "
                "new DailyV1SelectedContentField[] {\n" + ",\n".join(entries) + "\n        })")

    profile = object_expression("SimulationConfigData", 0, "profile-config/SimulationConfigData")
    assets = []
    captured_nodes = {owner: [] for owner in ("SimulationConfigData",) + ASSET_OWNERS}
    for owner in ("SimulationConfigData",) + ASSET_OWNERS:
        occurrences = fields.get(owner, {}).get("id", []) if owner in ASSET_OWNERS else [None]
        count = len(occurrences)
        if owner == "SimulationConfigData": count = 1
        for index in range(count):
            body = ";".join(f"{member}={fields[owner][member][index]}" for member in schema[owner])
            captured_nodes[owner].append(parse_value(f"value<{owner}>{{{body}}}", owner, schema))
    for owner in ASSET_OWNERS:
        occurrences = fields.get(owner, {}).get("id", [])
        for index, raw_id in enumerate(occurrences):
            identity = scalar_string(raw_id)
            assets.append(object_expression(owner, index, owner + "/" + identity))

    typed_config = typed_expr(captured_nodes["SimulationConfigData"][0], "SimulationConfigData")
    typed_calendar = typed_expr(captured_nodes["SimulationConfigData"][0][2]["calendar"], "CalendarDefinition")
    typed_assets = {}
    for owner in ASSET_OWNERS:
        typed_assets[owner] = "new[] { " + ", ".join(typed_expr(node, owner)
            for node in captured_nodes[owner]) + " }"
    typed_stage_order = ("p9.genesis.resolve-profile/v1", "p9.genesis.authored-world/v1",
        "p9.genesis.authored-geography/v1", "p9.genesis.authored-actors/v1",
        "p9.genesis.validate-profile/v1", "p9.genesis.publish/v1")
    effective = ("new EffectiveSimulationConfiguration(" +
        "new EffectivePopulationConfiguration(PopulationRepresentationMode.Aggregate, NpcDecisionSimulationScope.RelevantOnly, 18), " +
        "new EffectiveEconomyConfiguration(true), new EffectiveTravelConfiguration(10f), " +
        "new EffectiveCrimeConfiguration(true, true), new EffectiveGuardCrimeConfiguration(true), " +
        "new EffectiveNaturalMortalityConfiguration(NaturalMortalityPolicy.Disabled, 0d), " +
        "new EffectiveAggregateDemographyConfiguration(AggregateDemographyPolicy.Disabled, 0d, 0d), " +
        "new EffectiveMerchantTradeConfiguration(true, false, 5, 0.7f, 0.25f, 3), " +
        "new EffectiveCommercialKnowledgeConfiguration(7, 30, 2))")
    p9_graph = [
        "    public static DailyV1EffectiveDefinitions CreateDefinitions()",
        "    {",
        "        Create(out DailyV1SelectedContentAsset profile, out DailyV1SelectedContentAsset[] contentAssets);",
        "        if (!DailyV1SelectedContentProjectionFactory.TryCreate(contentAssets, profile,",
        "            out DailyV1SelectedContentProjection projection, out string diagnostic))",
        "            throw new InvalidOperationException(diagnostic);",
        "        DailyV1SimulationConfigData config = " + typed_config + ";",
        "        return DailyV1EffectiveDefinitions.CreateSelectedSchema(config, " + effective + ",",
        "            " + typed_calendar + ", " + typed_assets["CityData"] + ", " + typed_assets["ItemData"] + ",",
        "            " + typed_assets["NpcData"] + ", " + typed_assets["NpcJobData"] + ",",
        "            " + typed_assets["NpcActionData"] + ", " + typed_assets["NpcStatusData"] + ",",
        "            new[] { " + ", ".join(cs_string(x) for x in typed_stage_order) + " },",
        "            " + cs_string("unity-authored-bootstrap/genesis-v1") + ", " + cs_string("unity-authored-bootstrap/authored-geography-v1") + ",",
        "            " + cs_string("p9.genesis.authored-geography/v1") + ", " + cs_string("axial-hex-v1") + ", " + cs_string("q-then-r") + ").WithContentProjection(projection);",
        "    }",
    ]

    source = [
        "// Generated by tools/generate_daily_v1_captured_content_fixture.py.",
        f"// Source baseline SHA-256: {baseline_sha}; reviewed inventory rows: 164; captured occurrences: {members}.",
        "using System;", "",
        "public static class DailyV1CapturedSelectedContentFixture", "{",
        "    public static void Create(out DailyV1SelectedContentAsset profile, out DailyV1SelectedContentAsset[] assets)",
        "    {", "        profile = " + profile + ";", "        assets = new DailyV1SelectedContentAsset[]", "        {",
        ",\n".join("            " + asset for asset in assets),
        "        };", "    }",
        *p9_graph,
        "    private static float F32(uint bits) => BitConverter.ToSingle(BitConverter.GetBytes(unchecked((int)bits)), 0);",
        "    private static double F64(ulong bits) => BitConverter.ToDouble(BitConverter.GetBytes(unchecked((long)bits)), 0);",
        "}", "",
    ]
    OUTPUT.write_text("\n".join(source), encoding="utf-8", newline="\n")
    print(f"Wrote {OUTPUT.relative_to(ROOT)} from {members} captured occurrences ({len(assets)} selected assets).")


if __name__ == "__main__":
    main()

using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class RuntimeIdAllocator
{
    private long nextNpcSequence = 1;
    private long nextCitySequence = 1;

    public string AllocateNpcId()
    {
        return Allocate("npc", ref nextNpcSequence);
    }

    public string AllocateCityId()
    {
        return Allocate("city", ref nextCitySequence);
    }

    private static string Allocate(string prefix, ref long nextSequence)
    {
        if (nextSequence == long.MaxValue)
        {
            throw new InvalidOperationException($"RuntimeId sequence exhausted for type '{prefix}'.");
        }

        string runtimeId = prefix + "-" + nextSequence.ToString("D6", CultureInfo.InvariantCulture);
        nextSequence++;
        return runtimeId;
    }
}

public sealed class RuntimeIdentityRegistry
{
    private readonly Dictionary<string, NpcRuntime> npcsByRuntimeId = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
    private readonly Dictionary<string, CityRuntime> citiesByRuntimeId = new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
    private readonly SimulationLogger logger;

    public RuntimeIdentityRegistry(SimulationLogger logger = null)
    {
        this.logger = logger ?? new SimulationLogger(null);
    }

    public bool RegisterNpc(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            logger.LogError("Cannot register NPC runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId) == true)
        {
            logger.LogError("Cannot register NPC runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(npcRuntime.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{npcRuntime.RuntimeId}' while registering NPC; it is already registered as {registeredType}.");
            return false;
        }

        npcsByRuntimeId.Add(npcRuntime.RuntimeId, npcRuntime);
        return true;
    }

    public bool RegisterCity(CityRuntime cityRuntime)
    {
        if (cityRuntime == null)
        {
            logger.LogError("Cannot register City runtime identity: runtime instance is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(cityRuntime.RuntimeId) == true)
        {
            logger.LogError("Cannot register City runtime identity: RuntimeId is empty.");
            return false;
        }

        if (TryGetRegisteredType(cityRuntime.RuntimeId, out string registeredType) == true)
        {
            logger.LogError($"Duplicate RuntimeId '{cityRuntime.RuntimeId}' while registering City; it is already registered as {registeredType}.");
            return false;
        }

        citiesByRuntimeId.Add(cityRuntime.RuntimeId, cityRuntime);
        return true;
    }

    public bool TryGetNpc(string runtimeId, out NpcRuntime npcRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && npcsByRuntimeId.TryGetValue(runtimeId, out npcRuntime) == true)
        {
            return true;
        }

        npcRuntime = null;

        if (string.IsNullOrWhiteSpace(runtimeId) == false && citiesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            logger.LogWarning($"NPC runtime resolution failed: RuntimeId '{runtimeId}' is registered as City, not NPC.");
            return false;
        }

        logger.LogWarning($"NPC runtime resolution failed: RuntimeId '{FormatRuntimeId(runtimeId)}' is not registered.");
        return false;
    }

    public bool TryGetCity(string runtimeId, out CityRuntime cityRuntime)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == false && citiesByRuntimeId.TryGetValue(runtimeId, out cityRuntime) == true)
        {
            return true;
        }

        cityRuntime = null;

        if (string.IsNullOrWhiteSpace(runtimeId) == false && npcsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            logger.LogWarning($"City runtime resolution failed: RuntimeId '{runtimeId}' is registered as NPC, not City.");
            return false;
        }

        logger.LogWarning($"City runtime resolution failed: RuntimeId '{FormatRuntimeId(runtimeId)}' is not registered.");
        return false;
    }

    private bool TryGetRegisteredType(string runtimeId, out string registeredType)
    {
        if (npcsByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "NPC";
            return true;
        }

        if (citiesByRuntimeId.ContainsKey(runtimeId) == true)
        {
            registeredType = "City";
            return true;
        }

        registeredType = null;
        return false;
    }

    private static string FormatRuntimeId(string runtimeId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) == true ? "<empty>" : runtimeId;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

public sealed class DeterministicWorldCommandNaturalLanguageTranslator : IWorldCommandNaturalLanguageTranslator
{
    private const RegexOptions PatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    public NaturalLanguageTranslationResult Translate(
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context)
    {
        if (request == null)
        {
            return Invalid("RequestMissing", "A natural-language request is required.", "request");
        }

        if (Enum.IsDefined(typeof(WorldCommandOrigin), request.Origin) == false)
        {
            return Invalid("OriginInvalid", "The request origin is not a valid WorldCommandOrigin.", "origin");
        }

        string text = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Missing(new[] { "text" }, "Text is required.", "text");
        }

        bool suggest = StartsWithWord(text, "sugira");
        if (suggest)
        {
            text = text.Substring("sugira".Length).Trim();
        }

        if (IsNormalTravelRequest(text))
        {
            return Unsupported(
                "NormalTravelUnsupported",
                "Normal travel requests are not represented by an existing WorldCommand.");
        }

        if (Regex.IsMatch(text, "^(?:crie|criar|gere|gerar)\\s+.*\\b(?:npc|pessoa|entidade)\\b", PatternOptions))
        {
            return Unsupported(
                "NewEntityUnsupported",
                "Creating arbitrary entities is not represented by an existing WorldCommand.");
        }

        NaturalLanguageTranslationResult result;
        if (TryTranslateRelocation(text, request, context, suggest, out result)) return result;
        if (TryTranslateSiteKnowledge(text, request, context, suggest, out result)) return result;
        if (TryTranslateAdventureRumor(text, request, context, suggest, out result)) return result;
        if (TryTranslateConflict(text, request, context, suggest, out result)) return result;
        if (TryTranslateLocalConnection(text, request, context, suggest, out result)) return result;
        if (TryTranslateLocalPlace(text, request, context, suggest, out result)) return result;
        if (TryTranslateStackResource(text, request, context, suggest, out result)) return result;
        if (TryTranslateNotableItem(text, request, context, suggest, out result)) return result;

        return Invalid(
            "UnrecognizedInput",
            "The input does not match a supported deterministic WorldCommand phrase.");
    }

    private static bool TryTranslateRelocation(
        string text,
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context,
        bool suggest,
        out NaturalLanguageTranslationResult result)
    {
        result = null;
        Match match = Regex.Match(text, "^(?:coloque|declare)\\s+(.+?)\\s+(?:em|no|na)\\s+(.+?)$", PatternOptions);
        bool appeared = false;
        if (match.Success == false)
        {
            match = Regex.Match(text, "^(.+?)\\s+apareceu\\s+(?:em|no|na)\\s+(.+?)$", PatternOptions);
            appeared = match.Success;
        }

        if (match.Success == false)
        {
            return false;
        }

        string rawNpcReference = match.Groups[1].Value.Trim();
        string npcReference = CleanReference(rawNpcReference);
        if (appeared == false
            && (LooksLikeNonNpcReference(rawNpcReference)
                || (text.StartsWith("coloque", StringComparison.OrdinalIgnoreCase)
                    && context?.DefinitionLookup != null
                    && context.DefinitionLookup.FindItemDefinitions(npcReference).Count > 0)))
        {
            return false;
        }

        if (TryResolveNpc(context, npcReference, "npc", out WorldCommandTranslationEntity npc, out result) == false)
        {
            return true;
        }

        if (TryResolveMacroLocation(context, CleanReference(match.Groups[2].Value), "destination", out WorldCommandTranslationEntity location, out result) == false)
        {
            return true;
        }

        WorldCommandAuthorityMode authority = ResolveAuthority(
            request,
            suggest ? WorldCommandAuthorityMode.Suggest : WorldCommandAuthorityMode.Declare);
        WorldCommand command = new WorldCommand(
            WorldCommandKind.RelocateNpc,
            request.Origin,
            authority,
            new RelocateNpcWorldCommandPayload(npc.RuntimeId, location.RuntimeId));
        result = NaturalLanguageTranslationResult.Resolved(command, new[] { npc, location });
        return true;
    }

    private static bool TryTranslateSiteKnowledge(
        string text,
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context,
        bool suggest,
        out NaturalLanguageTranslationResult result)
    {
        result = null;
        Match match = Regex.Match(
            text,
            "^(.+?)\\s+(?:sabe|conhece)\\s+que\\s+(?:existe|há|ha)\\s+(.+?)$",
            PatternOptions);
        if (match.Success == false)
        {
            return false;
        }

        if (TryResolveNpc(context, CleanReference(match.Groups[1].Value), "npc", out WorldCommandTranslationEntity npc, out result) == false)
        {
            return true;
        }

        if (TryResolveSite(context, CleanReference(match.Groups[2].Value), "site", out WorldCommandTranslationEntity site, out result) == false)
        {
            return true;
        }

        WorldCommand command = new WorldCommand(
            WorldCommandKind.GrantSiteKnowledge,
            request.Origin,
            ResolveAuthority(request, suggest ? WorldCommandAuthorityMode.Suggest : WorldCommandAuthorityMode.Declare),
            new GrantSiteKnowledgeWorldCommandPayload(
                npc.RuntimeId,
                site.RuntimeId,
                ExplorableSiteKnowledgeSource.DirectObservation));
        result = NaturalLanguageTranslationResult.Resolved(command, new[] { npc, site });
        return true;
    }

    private static bool TryTranslateAdventureRumor(
        string text,
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context,
        bool suggest,
        out NaturalLanguageTranslationResult result)
    {
        result = null;
        Match match = Regex.Match(
            text,
            "^(.+?)\\s+(?:ouviu dizer|ouviu falar)\\s+que\\s+(?:há|ha|existe)\\s+(.+?)\\s+(?:em|no|na)\\s+(.+?)$",
            PatternOptions);
        if (match.Success == false)
        {
            return false;
        }

        if (TryResolveNpc(context, CleanReference(match.Groups[1].Value), "npc", out WorldCommandTranslationEntity npc, out result) == false)
        {
            return true;
        }

        if (TryResolveItem(context, CleanReference(match.Groups[2].Value), "item", out WorldCommandTranslationEntity item, out result) == false)
        {
            return true;
        }

        if (TryResolveSite(context, CleanReference(match.Groups[3].Value), "site", out WorldCommandTranslationEntity site, out result) == false)
        {
            return true;
        }

        WorldCommand command = new WorldCommand(
            WorldCommandKind.GrantAdventureIntel,
            request.Origin,
            ResolveAuthority(request, suggest ? WorldCommandAuthorityMode.Suggest : WorldCommandAuthorityMode.Declare),
            new GrantAdventureIntelWorldCommandPayload(
                npc.RuntimeId,
                AdventureIntelDeclarationKind.NotableItem,
                site.RuntimeId,
                itemDefinitionId: item.DefinitionId,
                accessState: PlaceAccessState.Accessible,
                source: AdventureIntelSource.DirectObservation));
        result = NaturalLanguageTranslationResult.Resolved(command, new[] { npc, item, site });
        return true;
    }

    private static bool TryTranslateConflict(
        string text,
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context,
        bool suggest,
        out NaturalLanguageTranslationResult result)
    {
        result = null;
        bool explicitOpposition = false;
        bool declaredWinner = false;
        bool declaredDeath = false;
        string oppositionReference = null;
        string firstReference;
        string secondReference;
        string locationReference;
        WorldCommandTranslationEntity opposition = null;
        Match match = Regex.Match(
            text,
            "^resolva\\s+(?:a\\s+)?oposi(?:ção|cao)\\s+(.+?)\\s+entre\\s+(.+?)\\s+e\\s+(.+?)(?:\\s+(?:em|no|na)\\s+(.+))?$",
            PatternOptions);
        if (match.Success)
        {
            explicitOpposition = true;
            oppositionReference = CleanReference(match.Groups[1].Value);
            firstReference = CleanReference(match.Groups[2].Value);
            secondReference = CleanReference(match.Groups[3].Value);
            locationReference = CleanReference(match.Groups[4].Value);
        }
        else
        {
            match = Regex.Match(
                text,
                "^resolva\\s+(?:o\\s+)?(?:combate|conflito)\\s+entre\\s+(.+?)\\s+e\\s+(.+?)(?:\\s+(?:em|no|na)\\s+(.+))?$",
                PatternOptions);
            if (match.Success)
            {
                firstReference = CleanReference(match.Groups[1].Value);
                secondReference = CleanReference(match.Groups[2].Value);
                locationReference = CleanReference(match.Groups[3].Value);
            }
            else
            {
                match = Regex.Match(
                    text,
                    "^(.+?)\\s+atacou\\s+(.+?)\\s+e\\s+(?:venceu|ganhou)(?:\\s+(?:em|no|na)\\s+(.+))?$",
                    PatternOptions);
                declaredWinner = match.Success;
                if (match.Success == false)
                {
                    match = Regex.Match(
                        text,
                        "^(.+?)\\s+venceu\\s+e\\s+(.+?)\\s+morreu(?:\\s+(?:em|no|na)\\s+(.+))?$",
                        PatternOptions);
                    declaredWinner = match.Success;
                    declaredDeath = match.Success;
                }

                if (match.Success == false)
                {
                    match = Regex.Match(
                        text,
                        "^(.+?)\\s+atacou\\s+(.+?)(?:\\s+(?:em|no|na)\\s+(.+))?$",
                        PatternOptions);
                }

                if (match.Success == false)
                {
                    return false;
                }

                firstReference = CleanReference(match.Groups[1].Value);
                secondReference = CleanReference(match.Groups[2].Value);
                locationReference = CleanReference(match.Groups[3].Value);
            }
        }

        if (TryResolveNpc(context, firstReference, "attacker", out WorldCommandTranslationEntity attacker, out result) == false)
        {
            return true;
        }

        if (TryResolveNpc(context, secondReference, "defender", out WorldCommandTranslationEntity defender, out result) == false)
        {
            return true;
        }

        WorldCommandTranslationEntity location = null;
        if (string.IsNullOrWhiteSpace(locationReference) == false
            && TryResolveMacroLocation(context, locationReference, "location", out location, out result) == false)
        {
            return true;
        }

        if (explicitOpposition
            && TryResolveOpposition(context, oppositionReference, "opposition", out opposition, out result) == false)
        {
            return true;
        }

        List<WorldConflictParticipantConstraintPayload> constraints = new List<WorldConflictParticipantConstraintPayload>();
        string forcedWinningSideId = null;
        WorldCommandAuthorityMode inferredAuthority;
        if (declaredWinner)
        {
            forcedWinningSideId = "attacker";
            inferredAuthority = WorldCommandAuthorityMode.ForceOutcome;
            if (declaredDeath)
            {
                constraints.Add(new WorldConflictParticipantConstraintPayload(
                    defender.RuntimeId,
                    forceDeath: true));
            }
        }
        else if (text.StartsWith("resolva", StringComparison.OrdinalIgnoreCase))
        {
            inferredAuthority = WorldCommandAuthorityMode.Request;
        }
        else
        {
            inferredAuthority = WorldCommandAuthorityMode.Declare;
        }

        WorldCommandKind kind = explicitOpposition ? WorldCommandKind.PlaceOpposition : WorldCommandKind.ResolveConflict;
        WorldCommandAuthorityMode authority = ResolveAuthority(
            request,
            suggest ? WorldCommandAuthorityMode.Suggest : inferredAuthority);
        WorldCommandPayload payload = new ResolveConflictWorldCommandPayload(
            location?.RuntimeId,
            new[]
            {
                new WorldConflictSidePayload(
                    "attacker",
                    ConflictObjectiveType.Defeat,
                    ConflictStakes.Meaningful,
                    new[] { new WorldConflictParticipantPayload(attacker.RuntimeId) }),
                new WorldConflictSidePayload(
                    "defender",
                    ConflictObjectiveType.Defend,
                    ConflictStakes.Meaningful,
                    new[] { new WorldConflictParticipantPayload(defender.RuntimeId) })
            },
            explicitOpposition ? opposition.RuntimeId : null,
            forcedWinningSideId,
            declaredWinner ? ConflictOutcomeType.Victory : null,
            constraints);
        WorldCommand command = new WorldCommand(kind, request.Origin, authority, payload);
        List<WorldCommandTranslationEntity> entities = new List<WorldCommandTranslationEntity> { attacker, defender };
        if (location != null) entities.Add(location);
        if (explicitOpposition) entities.Add(opposition);
        result = NaturalLanguageTranslationResult.Resolved(command, entities);
        return true;
    }

    private static bool TryTranslateLocalConnection(
        string text,
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context,
        bool suggest,
        out NaturalLanguageTranslationResult result)
    {
        result = null;
        Match match = Regex.Match(
            text,
            "^(?:adicione|declare)\\s+(?:uma\\s+)?conex(?:ão|ao)\\s+entre\\s+(.+?)\\s+e\\s+(.+?)\\s+(?:em|no|na)\\s+(.+?)(?:\\s+com\\s+custo\\s+([0-9]+(?:[.,][0-9]+)?))?$",
            PatternOptions);
        if (match.Success == false)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(match.Groups[4].Value))
        {
            result = Missing(new[] { "traversal cost" }, "Traversal cost is required for a local connection.", "traversal cost");
            return true;
        }

        if (float.TryParse(
            match.Groups[4].Value.Replace(',', '.'),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float traversalCost) == false
            || LocalTopologyConnectionRuntime.IsValidTraversalCost(traversalCost) == false)
        {
            result = Invalid("TraversalCostInvalid", "Traversal cost must be a positive finite number.", "traversal cost");
            return true;
        }

        if (TryResolveLocalPlace(context, CleanReference(match.Groups[1].Value), "origin", out WorldCommandTranslationEntity origin, out result) == false)
        {
            return true;
        }

        if (TryResolveLocalPlace(context, CleanReference(match.Groups[2].Value), "destination", out WorldCommandTranslationEntity destination, out result) == false)
        {
            return true;
        }

        if (TryResolveTopologyOwner(context, CleanReference(match.Groups[3].Value), "topology owner", out WorldCommandTranslationEntity owner, out result) == false)
        {
            return true;
        }

        WorldCommand command = new WorldCommand(
            WorldCommandKind.AddLocalConnection,
            request.Origin,
            ResolveAuthority(request, suggest ? WorldCommandAuthorityMode.Suggest : WorldCommandAuthorityMode.Declare),
            new AddLocalConnectionWorldCommandPayload(
                owner.RuntimeId,
                origin.RuntimeId,
                destination.RuntimeId,
                traversalCost));
        result = NaturalLanguageTranslationResult.Resolved(command, new[] { origin, destination, owner });
        return true;
    }

    private static bool TryTranslateLocalPlace(
        string text,
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context,
        bool suggest,
        out NaturalLanguageTranslationResult result)
    {
        result = null;
        Match match = Regex.Match(
            text,
            "^(?:adicione|declare)\\s+(?:um\\s+|uma\\s+|o\\s+|a\\s+)?(?:local\\s+)?(?:lugar|place|sala)\\s+(.+?)\\s+(?:em|no|na)\\s+(.+?)$",
            PatternOptions);
        if (match.Success == false)
        {
            return false;
        }

        if (TryResolveTopologyOwner(context, CleanReference(match.Groups[2].Value), "topology owner", out WorldCommandTranslationEntity owner, out result) == false)
        {
            return true;
        }

        WorldCommand command = new WorldCommand(
            WorldCommandKind.AddLocalPlace,
            request.Origin,
            ResolveAuthority(request, suggest ? WorldCommandAuthorityMode.Suggest : WorldCommandAuthorityMode.Declare),
            new AddLocalPlaceWorldCommandPayload(owner.RuntimeId, CleanReference(match.Groups[1].Value)));
        result = NaturalLanguageTranslationResult.Resolved(command, new[] { owner });
        return true;
    }

    private static bool TryTranslateStackResource(
        string text,
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context,
        bool suggest,
        out NaturalLanguageTranslationResult result)
    {
        result = null;
        Match match = Regex.Match(
            text,
            "^(?:adicione|declare)\\s+(?:(\\d+)\\s+)?(.+?)\\s+(?:em|no|na)\\s+(.+?)$",
            PatternOptions);
        if (match.Success == false)
        {
            match = Regex.Match(text, "^(?:adicione|declare)\\s+(.+?)$", PatternOptions);
            if (match.Success == false || Regex.IsMatch(text, "^(?:adicione|declare)\\s+(?:um\\s+|uma\\s+)?(?:local\\s+)?(?:lugar|place|sala|conex(?:ão|ao))\\b", PatternOptions))
            {
                return false;
            }

            result = Missing(
                new[] { "amount", "target owner" },
                "A resource declaration requires an amount and a target owner.",
                "amount");
            return true;
        }

        if (string.IsNullOrWhiteSpace(match.Groups[1].Value))
        {
            result = Missing(new[] { "amount" }, "Amount is required for a common resource declaration.", "amount");
            return true;
        }

        if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) == false || amount <= 0)
        {
            result = Invalid("AmountInvalid", "Amount must be a positive integer.", "amount");
            return true;
        }

        if (TryResolveItem(context, CleanReference(match.Groups[2].Value), "item", out WorldCommandTranslationEntity item, out result) == false)
        {
            return true;
        }

        if (TryResolveContentOwner(context, CleanReference(match.Groups[3].Value), "target owner", out WorldCommandTranslationEntity owner, out result) == false)
        {
            return true;
        }

        if (TryBuildContentOwner(owner, out WorldContentOwnerReferencePayload ownerPayload) == false)
        {
            result = Missing(new[] { "owner macro location" }, "The target owner has no resolved macro location.", "target owner");
            return true;
        }

        WorldCommand command = new WorldCommand(
            WorldCommandKind.DeclareStackResource,
            request.Origin,
            ResolveAuthority(request, suggest ? WorldCommandAuthorityMode.Suggest : WorldCommandAuthorityMode.Declare),
            new DeclareStackResourceWorldCommandPayload(
                ownerPayload,
                item.DefinitionId,
                amount,
                PlaceContentPersistencePolicy.Durable));
        result = NaturalLanguageTranslationResult.Resolved(command, new[] { item, owner });
        return true;
    }

    private static bool TryTranslateNotableItem(
        string text,
        NaturalLanguageWorldCommandRequest request,
        WorldCommandTranslationContext context,
        bool suggest,
        out NaturalLanguageTranslationResult result)
    {
        result = null;
        bool explicitNotable = false;
        bool explicitVerb = false;
        Match match = Regex.Match(
            text,
            "^(coloque|declare|crie)\\s+(?:um\\s+|uma\\s+|o\\s+|a\\s+)?(.+?)\\s+(?:em|no|na)\\s+(.+?)$",
            PatternOptions);
        if (match.Success)
        {
            explicitNotable = true;
            explicitVerb = true;
        }
        else
        {
            match = Regex.Match(
                text,
                "^(?:há|ha|existe)\\s+(?:um\\s+|uma\\s+|o\\s+|a\\s+)?(.+?)\\s+(?:em|no|na)\\s+(.+?)$",
                PatternOptions);
            explicitNotable = match.Success && Regex.IsMatch(
                text,
                "(notável|notavel|lendári|lendari|relíquia|reliquia)",
                PatternOptions);
        }

        if (match.Success == false)
        {
            return false;
        }

        string itemReference = CleanReference(explicitVerb ? match.Groups[2].Value : match.Groups[1].Value);
        if (TryResolveItem(context, itemReference, "item", out WorldCommandTranslationEntity item, out result) == false)
        {
            return true;
        }

        if (explicitNotable == false)
        {
            result = NaturalLanguageTranslationResult.Ambiguous(
                new[] { new WorldCommandTranslationCandidate(item) },
                new[] { new WorldCommandTranslationDiagnostic(
                    "ItemDeclarationKindAmbiguous",
                    "The phrase does not identify whether the item is a common resource or a notable item.",
                    "item") });
            return true;
        }

        string ownerReference = explicitVerb ? match.Groups[3].Value : match.Groups[2].Value;
        if (TryResolveContentOwner(context, CleanReference(ownerReference), "target owner", out WorldCommandTranslationEntity owner, out result) == false)
        {
            return true;
        }

        if (TryBuildContentOwner(owner, out WorldContentOwnerReferencePayload ownerPayload) == false)
        {
            result = Missing(new[] { "owner macro location" }, "The target owner has no resolved macro location.", "target owner");
            return true;
        }

        WorldCommand command = new WorldCommand(
            WorldCommandKind.DeclareNotableItem,
            request.Origin,
            ResolveAuthority(request, suggest ? WorldCommandAuthorityMode.Suggest : WorldCommandAuthorityMode.Declare),
            new DeclareNotableItemWorldCommandPayload(ownerPayload, item.DefinitionId));
        result = NaturalLanguageTranslationResult.Resolved(command, new[] { item, owner });
        return true;
    }

    private static bool TryResolveNpc(
        WorldCommandTranslationContext context,
        string reference,
        string field,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        return TryResolve(
            context,
            field,
            reference,
            context?.EntityResolver == null ? null : context.EntityResolver.FindNpcs(reference),
            out entity,
            out failure);
    }

    private static bool TryResolveMacroLocation(
        WorldCommandTranslationContext context,
        string reference,
        string field,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        return TryResolve(
            context,
            field,
            reference,
            context?.EntityResolver == null ? null : context.EntityResolver.FindMacroLocations(reference),
            out entity,
            out failure);
    }

    private static bool TryResolveSite(
        WorldCommandTranslationContext context,
        string reference,
        string field,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        return TryResolve(
            context,
            field,
            reference,
            context?.EntityResolver == null ? null : context.EntityResolver.FindExplorableSites(reference),
            out entity,
            out failure);
    }

    private static bool TryResolveLocalPlace(
        WorldCommandTranslationContext context,
        string reference,
        string field,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        return TryResolve(
            context,
            field,
            reference,
            context?.EntityResolver == null ? null : context.EntityResolver.FindLocalPlaces(reference),
            out entity,
            out failure);
    }

    private static bool TryResolveOpposition(
        WorldCommandTranslationContext context,
        string reference,
        string field,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        return TryResolve(
            context,
            field,
            reference,
            context?.EntityResolver == null ? null : context.EntityResolver.FindOppositions(reference),
            out entity,
            out failure);
    }

    private static bool TryResolveItem(
        WorldCommandTranslationContext context,
        string reference,
        string field,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        IReadOnlyList<WorldCommandTranslationEntity> matches = null;
        if (context != null && context.DefinitionLookup != null)
        {
            matches = context.DefinitionLookup.FindItemDefinitions(reference);
        }

        if (matches == null && context?.DefinitionResolver != null)
        {
            if (context.DefinitionResolver.TryResolveItem(reference, out ItemData item) && item != null)
            {
                matches = new[]
                {
                    new WorldCommandTranslationEntity(
                        WorldCommandTranslationEntityKind.ItemDefinition,
                        definitionId: item.DefinitionId,
                        displayName: item.itemName)
                };
            }
        }

        return TryResolve(context, field, reference, matches, out entity, out failure);
    }

    private static bool TryResolveContentOwner(
        WorldCommandTranslationContext context,
        string reference,
        string field,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        entity = null;
        failure = null;
        if (context?.EntityResolver == null)
        {
            failure = Invalid("EntityResolverMissing", "Entity resolution requires a read-only entity resolver.", field);
            return false;
        }

        List<WorldCommandTranslationEntity> matches = new List<WorldCommandTranslationEntity>();
        AddUnique(matches, context.EntityResolver.FindCities(reference));
        AddUnique(matches, context.EntityResolver.FindExplorableSites(reference));
        AddUnique(matches, context.EntityResolver.FindLocalPlaces(reference));
        return TryResolve(context, field, reference, matches, out entity, out failure);
    }

    private static bool TryResolveTopologyOwner(
        WorldCommandTranslationContext context,
        string reference,
        string field,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        entity = null;
        failure = null;
        if (context?.EntityResolver == null)
        {
            failure = Invalid("EntityResolverMissing", "Entity resolution requires a read-only entity resolver.", field);
            return false;
        }

        List<WorldCommandTranslationEntity> matches = new List<WorldCommandTranslationEntity>();
        AddTopologyOwners(matches, context.EntityResolver.FindCities(reference));
        AddTopologyOwners(matches, context.EntityResolver.FindExplorableSites(reference));
        AddTopologyOwners(matches, context.EntityResolver.FindLocalPlaces(reference));
        return TryResolve(context, field, reference, matches, out entity, out failure);
    }

    private static void AddTopologyOwners(
        List<WorldCommandTranslationEntity> target,
        IReadOnlyList<WorldCommandTranslationEntity> candidates)
    {
        if (candidates == null)
        {
            return;
        }

        foreach (WorldCommandTranslationEntity candidate in candidates)
        {
            if (candidate == null)
            {
                continue;
            }

            string topologyOwnerId = string.IsNullOrWhiteSpace(candidate.TopologyOwnerRuntimeId)
                ? candidate.RuntimeId
                : candidate.TopologyOwnerRuntimeId;
            if (string.IsNullOrWhiteSpace(topologyOwnerId))
            {
                continue;
            }

            AddUnique(target, new WorldCommandTranslationEntity(
                candidate.Kind,
                topologyOwnerId,
                candidate.DefinitionId,
                candidate.DisplayName,
                candidate.MacroLocationRuntimeId,
                topologyOwnerId));
        }
    }

    private static bool TryBuildContentOwner(
        WorldCommandTranslationEntity entity,
        out WorldContentOwnerReferencePayload payload)
    {
        payload = null;
        if (entity == null || string.IsNullOrWhiteSpace(entity.RuntimeId) || string.IsNullOrWhiteSpace(entity.MacroLocationRuntimeId))
        {
            return false;
        }

        PlaceContentOwnerKind ownerKind;
        switch (entity.Kind)
        {
            case WorldCommandTranslationEntityKind.City:
                ownerKind = PlaceContentOwnerKind.City;
                break;
            case WorldCommandTranslationEntityKind.ExplorableSite:
                ownerKind = PlaceContentOwnerKind.ExplorableSite;
                break;
            case WorldCommandTranslationEntityKind.LocalPlace:
                ownerKind = PlaceContentOwnerKind.LocalPlace;
                break;
            default:
                return false;
        }

        payload = new WorldContentOwnerReferencePayload(
            ownerKind,
            entity.RuntimeId,
            entity.MacroLocationRuntimeId,
            entity.TopologyOwnerRuntimeId);
        return true;
    }

    private static bool TryResolve(
        WorldCommandTranslationContext context,
        string field,
        string reference,
        IReadOnlyList<WorldCommandTranslationEntity> matches,
        out WorldCommandTranslationEntity entity,
        out NaturalLanguageTranslationResult failure)
    {
        entity = null;
        failure = null;
        if (context == null)
        {
            failure = Invalid("TranslationContextMissing", "Translation requires a read-only resolution context.", field);
            return false;
        }

        if (matches == null || matches.Count == 0)
        {
            failure = Missing(
                new[] { field },
                "No entity matched the supplied reference.",
                field,
                "EntityNotFound");
            return false;
        }

        if (matches.Count > 1)
        {
            List<WorldCommandTranslationCandidate> candidates = new List<WorldCommandTranslationCandidate>();
            foreach (WorldCommandTranslationEntity match in matches)
            {
                candidates.Add(new WorldCommandTranslationCandidate(match));
            }

            failure = NaturalLanguageTranslationResult.Ambiguous(
                candidates,
                new[] { new WorldCommandTranslationDiagnostic(
                    "EntityReferenceAmbiguous",
                    "More than one entity matches the supplied reference.",
                    field) });
            return false;
        }

        entity = matches[0];
        return true;
    }

    private static void AddUnique(
        List<WorldCommandTranslationEntity> target,
        IReadOnlyList<WorldCommandTranslationEntity> candidates)
    {
        if (candidates == null)
        {
            return;
        }

        foreach (WorldCommandTranslationEntity candidate in candidates)
        {
            AddUnique(target, candidate);
        }
    }

    private static void AddUnique(
        List<WorldCommandTranslationEntity> target,
        WorldCommandTranslationEntity candidate)
    {
        if (candidate == null || string.IsNullOrWhiteSpace(candidate.StableId))
        {
            return;
        }

        foreach (WorldCommandTranslationEntity existing in target)
        {
            if (existing != null
                && string.Equals(existing.StableId, candidate.StableId, StringComparison.Ordinal))
            {
                return;
            }
        }

        target.Add(candidate);
    }

    private static WorldCommandAuthorityMode ResolveAuthority(
        NaturalLanguageWorldCommandRequest request,
        WorldCommandAuthorityMode inferred)
    {
        return request.PreferredAuthority ?? inferred;
    }

    private static bool IsNormalTravelRequest(string text)
    {
        return Regex.IsMatch(
            text,
            "\\b(viajar|viaje|viaja|vai)\\b.*\\b(para|até|ate)\\b",
            PatternOptions);
    }

    private static bool StartsWithWord(string text, string word)
    {
        return string.Equals(text, word, StringComparison.OrdinalIgnoreCase)
            || text.StartsWith(word + " ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeNonNpcReference(string reference)
    {
        return Regex.IsMatch(reference, "^(?:o|a|um|uma|uma\\s+conexão|uma\\s+conexao|local|lugar|place|100|[0-9]+)\\b", PatternOptions);
    }

    private static string CleanReference(string value)
    {
        string result = (value ?? string.Empty).Trim();
        if (result.Length >= 2
            && ((result[0] == '"' && result[result.Length - 1] == '"')
                || (result[0] == '\'' && result[result.Length - 1] == '\'')))
        {
            result = result.Substring(1, result.Length - 2).Trim();
        }

        result = Regex.Replace(result, "^(?:o|a|um|uma)\\s+", string.Empty, PatternOptions);

        return result;
    }

    private static NaturalLanguageTranslationResult Missing(
        IEnumerable<string> fields,
        string message,
        string field,
        string code = "MissingInformation")
    {
        return NaturalLanguageTranslationResult.MissingInformation(
            fields,
            new[] { new WorldCommandTranslationDiagnostic(code, message, field) });
    }

    private static NaturalLanguageTranslationResult Unsupported(string code, string message)
    {
        return NaturalLanguageTranslationResult.Unsupported(
            new[] { new WorldCommandTranslationDiagnostic(code, message) });
    }

    private static NaturalLanguageTranslationResult Invalid(string code, string message, string field = null)
    {
        return NaturalLanguageTranslationResult.Invalid(
            new[] { new WorldCommandTranslationDiagnostic(code, message, field) });
    }
}

public sealed class DeterministicWorldCommandEntityResolver : IWorldCommandEntityResolver
{
    private readonly IReadOnlyList<WorldCommandTranslationEntity> npcs;
    private readonly IReadOnlyList<WorldCommandTranslationEntity> macroLocations;
    private readonly IReadOnlyList<WorldCommandTranslationEntity> cities;
    private readonly IReadOnlyList<WorldCommandTranslationEntity> sites;
    private readonly IReadOnlyList<WorldCommandTranslationEntity> localPlaces;
    private readonly IReadOnlyList<WorldCommandTranslationEntity> oppositions;

    public DeterministicWorldCommandEntityResolver(
        IEnumerable<NpcRuntime> npcs = null,
        IEnumerable<CityRuntime> cities = null,
        IEnumerable<SpatialLocationRuntime> macroLocations = null,
        ExplorableSiteStore siteStore = null,
        LocalTopologyStore topologyStore = null,
        PlaceContentStore contentStore = null)
    {
        Dictionary<string, WorldCommandTranslationEntity> npcMap = new Dictionary<string, WorldCommandTranslationEntity>(StringComparer.Ordinal);
        Dictionary<string, WorldCommandTranslationEntity> locationMap = new Dictionary<string, WorldCommandTranslationEntity>(StringComparer.Ordinal);
        Dictionary<string, WorldCommandTranslationEntity> cityMap = new Dictionary<string, WorldCommandTranslationEntity>(StringComparer.Ordinal);
        Dictionary<string, WorldCommandTranslationEntity> siteMap = new Dictionary<string, WorldCommandTranslationEntity>(StringComparer.Ordinal);
        Dictionary<string, WorldCommandTranslationEntity> localPlaceMap = new Dictionary<string, WorldCommandTranslationEntity>(StringComparer.Ordinal);
        Dictionary<string, WorldCommandTranslationEntity> oppositionMap = new Dictionary<string, WorldCommandTranslationEntity>(StringComparer.Ordinal);

        if (npcs != null)
        {
            foreach (NpcRuntime npc in npcs)
            {
                if (npc != null)
                {
                    AddOrMerge(npcMap, new WorldCommandTranslationEntity(
                        WorldCommandTranslationEntityKind.Npc,
                        npc.RuntimeId,
                        npc.DefinitionId,
                        npc.NpcName));
                }
            }
        }

        if (macroLocations != null)
        {
            foreach (SpatialLocationRuntime location in macroLocations)
            {
                if (location != null)
                {
                    AddOrMerge(locationMap, new WorldCommandTranslationEntity(
                        WorldCommandTranslationEntityKind.MacroLocation,
                        location.RuntimeId));
                }
            }
        }

        if (cities != null)
        {
            foreach (CityRuntime city in cities)
            {
                if (city == null) continue;
                AddOrMerge(cityMap, new WorldCommandTranslationEntity(
                    WorldCommandTranslationEntityKind.City,
                    city.RuntimeId,
                    city.DefinitionId,
                    city.CityName,
                    city.Location?.RuntimeId,
                    city.RuntimeId));
                if (city.Location != null)
                {
                    AddOrMerge(locationMap, new WorldCommandTranslationEntity(
                        WorldCommandTranslationEntityKind.MacroLocation,
                        city.Location.RuntimeId,
                        city.DefinitionId,
                        city.CityName));
                }
            }
        }

        if (siteStore != null)
        {
            foreach (ExplorableSiteRuntime site in siteStore.Sites)
            {
                if (site == null) continue;
                AddOrMerge(siteMap, new WorldCommandTranslationEntity(
                    WorldCommandTranslationEntityKind.ExplorableSite,
                    site.RuntimeId,
                    site.DefinitionId,
                    site.Definition?.DisplayName,
                    site.Location?.RuntimeId,
                    site.RuntimeId));
                if (site.Location != null)
                {
                    AddOrMerge(locationMap, new WorldCommandTranslationEntity(
                        WorldCommandTranslationEntityKind.MacroLocation,
                        site.Location.RuntimeId,
                        site.DefinitionId,
                        site.Definition?.DisplayName));
                }
            }
        }

        if (topologyStore != null)
        {
            foreach (LocalTopologyRuntime topology in topologyStore.Topologies)
            {
                if (topology == null || topology.Owner == null) continue;
                foreach (LocalPlaceRuntime place in topology.Places)
                {
                    if (place != null)
                    {
                        AddOrMerge(localPlaceMap, new WorldCommandTranslationEntity(
                            WorldCommandTranslationEntityKind.LocalPlace,
                            place.RuntimeId,
                            place.TypeDefinitionId,
                            place.DisplayName,
                            topology.Owner.MacroLocationRuntimeId,
                            topology.Owner.OwnerRuntimeId));
                    }
                }
            }
        }

        if (contentStore != null)
        {
            foreach (PlaceContentRuntime content in contentStore.Places)
            {
                if (content?.Owner == null) continue;
                foreach (PlaceOppositionRuntime opposition in content.Oppositions)
                {
                    if (opposition != null)
                    {
                        AddOrMerge(oppositionMap, new WorldCommandTranslationEntity(
                            WorldCommandTranslationEntityKind.Opposition,
                            opposition.RuntimeId,
                            displayName: opposition.DisplayName,
                            macroLocationRuntimeId: content.Owner.MacroLocationRuntimeId,
                            topologyOwnerRuntimeId: content.Owner.TopologyOwnerRuntimeId));
                    }
                }
            }
        }

        this.npcs = ToList(npcMap);
        this.macroLocations = ToList(locationMap);
        this.cities = ToList(cityMap);
        sites = ToList(siteMap);
        localPlaces = ToList(localPlaceMap);
        oppositions = ToList(oppositionMap);
    }

    public IReadOnlyList<WorldCommandTranslationEntity> FindNpcs(string reference) => WorldCommandTranslationMatching.Resolve(npcs, reference);
    public IReadOnlyList<WorldCommandTranslationEntity> FindMacroLocations(string reference) => WorldCommandTranslationMatching.Resolve(macroLocations, reference);
    public IReadOnlyList<WorldCommandTranslationEntity> FindCities(string reference) => WorldCommandTranslationMatching.Resolve(cities, reference);
    public IReadOnlyList<WorldCommandTranslationEntity> FindExplorableSites(string reference) => WorldCommandTranslationMatching.Resolve(sites, reference);
    public IReadOnlyList<WorldCommandTranslationEntity> FindLocalPlaces(string reference) => WorldCommandTranslationMatching.Resolve(localPlaces, reference);
    public IReadOnlyList<WorldCommandTranslationEntity> FindOppositions(string reference) => WorldCommandTranslationMatching.Resolve(oppositions, reference);

    private static void AddOrMerge(
        Dictionary<string, WorldCommandTranslationEntity> values,
        WorldCommandTranslationEntity candidate)
    {
        if (candidate == null || string.IsNullOrWhiteSpace(candidate.StableId)) return;
        if (values.TryGetValue(candidate.StableId, out WorldCommandTranslationEntity existing) == false)
        {
            values.Add(candidate.StableId, candidate);
            return;
        }

        values[candidate.StableId] = new WorldCommandTranslationEntity(
            existing.Kind,
            existing.RuntimeId ?? candidate.RuntimeId,
            existing.DefinitionId ?? candidate.DefinitionId,
            existing.DisplayName ?? candidate.DisplayName,
            existing.MacroLocationRuntimeId ?? candidate.MacroLocationRuntimeId,
            existing.TopologyOwnerRuntimeId ?? candidate.TopologyOwnerRuntimeId);
    }

    private static IReadOnlyList<WorldCommandTranslationEntity> ToList(
        Dictionary<string, WorldCommandTranslationEntity> values)
    {
        List<WorldCommandTranslationEntity> result = new List<WorldCommandTranslationEntity>(values.Values);
        result.Sort((left, right) => StringComparer.Ordinal.Compare(left.StableId, right.StableId));
        return result.AsReadOnly();
    }
}

public sealed class DeterministicWorldCommandDefinitionLookup : IWorldCommandDefinitionLookup
{
    private readonly IReadOnlyList<WorldCommandTranslationEntity> items;
    private readonly IReadOnlyList<WorldCommandTranslationEntity> placeTypes;
    private readonly IReadOnlyList<WorldCommandTranslationEntity> connectionTypes;

    public DeterministicWorldCommandDefinitionLookup(
        IEnumerable<ItemData> items = null,
        IEnumerable<LocalPlaceTypeData> placeTypes = null,
        IEnumerable<LocalConnectionTypeData> connectionTypes = null)
    {
        this.items = BuildItems(items);
        this.placeTypes = BuildPlaceTypes(placeTypes);
        this.connectionTypes = BuildConnectionTypes(connectionTypes);
    }

    public IReadOnlyList<WorldCommandTranslationEntity> FindItemDefinitions(string reference)
        => WorldCommandTranslationMatching.Resolve(items, reference);

    public IReadOnlyList<WorldCommandTranslationEntity> FindLocalPlaceTypes(string reference)
        => WorldCommandTranslationMatching.Resolve(placeTypes, reference);

    public IReadOnlyList<WorldCommandTranslationEntity> FindLocalConnectionTypes(string reference)
        => WorldCommandTranslationMatching.Resolve(connectionTypes, reference);

    private static IReadOnlyList<WorldCommandTranslationEntity> BuildItems(IEnumerable<ItemData> source)
    {
        List<WorldCommandTranslationEntity> result = new List<WorldCommandTranslationEntity>();
        if (source != null)
        {
            foreach (ItemData item in source)
            {
                if (item != null)
                {
                    result.Add(new WorldCommandTranslationEntity(
                        WorldCommandTranslationEntityKind.ItemDefinition,
                        definitionId: item.DefinitionId,
                        displayName: item.itemName));
                }
            }
        }

        return result.AsReadOnly();
    }

    private static IReadOnlyList<WorldCommandTranslationEntity> BuildPlaceTypes(IEnumerable<LocalPlaceTypeData> source)
    {
        return BuildDefinitionEntities(source, WorldCommandTranslationEntityKind.LocalPlaceType,
            value => value.DefinitionId,
            value => value.DisplayName);
    }

    private static IReadOnlyList<WorldCommandTranslationEntity> BuildConnectionTypes(IEnumerable<LocalConnectionTypeData> source)
    {
        return BuildDefinitionEntities(source, WorldCommandTranslationEntityKind.LocalConnectionType,
            value => value.DefinitionId,
            value => value.DisplayName);
    }

    private static IReadOnlyList<WorldCommandTranslationEntity> BuildDefinitionEntities<T>(
        IEnumerable<T> source,
        WorldCommandTranslationEntityKind kind,
        Func<T, string> idSelector,
        Func<T, string> nameSelector)
        where T : UnityEngine.Object
    {
        List<WorldCommandTranslationEntity> result = new List<WorldCommandTranslationEntity>();
        if (source != null)
        {
            foreach (T definition in source)
            {
                if (definition != null)
                {
                    result.Add(new WorldCommandTranslationEntity(
                        kind,
                        definitionId: idSelector(definition),
                        displayName: nameSelector(definition)));
                }
            }
        }

        return result.AsReadOnly();
    }
}

internal static class WorldCommandTranslationMatching
{
    public static IReadOnlyList<WorldCommandTranslationEntity> Resolve(
        IEnumerable<WorldCommandTranslationEntity> source,
        string reference)
    {
        List<WorldCommandTranslationEntity> values = new List<WorldCommandTranslationEntity>();
        if (source != null)
        {
            foreach (WorldCommandTranslationEntity value in source)
            {
                if (value != null && string.IsNullOrWhiteSpace(value.StableId) == false)
                {
                    values.Add(value);
                }
            }
        }

        string normalized = (reference ?? string.Empty).Trim();
        List<WorldCommandTranslationEntity> exactRuntime = Filter(values, candidate =>
            string.Equals(candidate.RuntimeId, normalized, StringComparison.Ordinal));
        if (exactRuntime.Count > 0)
        {
            return Sort(exactRuntime);
        }

        List<WorldCommandTranslationEntity> exactDefinition = Filter(values, candidate =>
            string.Equals(candidate.DefinitionId, normalized, StringComparison.Ordinal));
        if (exactDefinition.Count > 0)
        {
            return Sort(exactDefinition);
        }

        List<WorldCommandTranslationEntity> displayName = Filter(values, candidate =>
            string.IsNullOrWhiteSpace(candidate.DisplayName) == false
            && string.Equals(candidate.DisplayName.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
        return Sort(displayName);
    }

    private static List<WorldCommandTranslationEntity> Filter(
        List<WorldCommandTranslationEntity> source,
        Func<WorldCommandTranslationEntity, bool> predicate)
    {
        List<WorldCommandTranslationEntity> result = new List<WorldCommandTranslationEntity>();
        foreach (WorldCommandTranslationEntity value in source)
        {
            if (predicate(value))
            {
                bool duplicate = false;
                foreach (WorldCommandTranslationEntity existing in result)
                {
                    if (existing.StableId == value.StableId)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (duplicate == false) result.Add(value);
            }
        }

        return result;
    }

    private static IReadOnlyList<WorldCommandTranslationEntity> Sort(List<WorldCommandTranslationEntity> values)
    {
        values.Sort((left, right) =>
        {
            int comparison = StringComparer.Ordinal.Compare(left.StableId, right.StableId);
            if (comparison != 0) return comparison;
            return StringComparer.Ordinal.Compare(left.DisplayName, right.DisplayName);
        });
        return values.AsReadOnly();
    }
}

using System;
using System.Globalization;

internal static class Program
{
    private static int Main()
    {
        try
        {
            FiniteSourceScenarioDefinition definition = FiniteSourceScenarioParityVectors.CreateDefinition(
                out FiniteSourceScenarioSnapshot snapshot);
            if (!FiniteSourceScenarioRuntime.TryCreate(definition, snapshot,
                    out FiniteSourceScenarioRuntime runtime, out string rejection))
                throw new InvalidOperationException("Scenario admission rejected: " + rejection);

            Console.WriteLine("scenario location={0} settlement={1} source={2} market={3} item={4}",
                definition.LocationId, definition.SettlementSemanticId, definition.ProductionSourceId,
                definition.MarketStoreSemanticId, definition.ItemDefinitionId);
            foreach (long day in FiniteSourceScenarioParityVectors.AdmittedDays)
            {
                if (!runtime.TryPrepareAdvance(snapshot, day, out PreparedFiniteSourceAdvance prepared,
                        out rejection))
                    throw new InvalidOperationException("Day " + day + " preparation rejected: " + rejection);
                if (!runtime.TryApplyPrepared(snapshot, prepared,
                        out FiniteSourceScenarioSnapshot next, out rejection))
                    throw new InvalidOperationException("Day " + day + " application rejected: " + rejection);
                snapshot = next;
                MarketStockRow target = snapshot.MarketRows[0];
                Console.WriteLine(
                    "day={0} status={1} produced={2} reserve={3} sourceRevision={4} marketRevision={5} stock={6} price={7} priceBits={8:X8}",
                    day, prepared.Status, prepared.Quantity, snapshot.Source.RemainingReserve,
                    snapshot.Source.Revision, snapshot.MarketRevision, target.Amount,
                    target.CurrentPrice.ToString("R", CultureInfo.InvariantCulture),
                    unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(target.CurrentPrice), 0)));
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}

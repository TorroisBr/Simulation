using System;
using System.IO;

namespace Simulation.WorldExchangeProducer
{
    public sealed class WorldExchangePublishResult
    {
        internal WorldExchangePublishResult(bool succeeded, string failureCode, string failureMessage)
        {
            Succeeded = succeeded;
            FailureCode = failureCode;
            FailureMessage = failureMessage;
        }

        public bool Succeeded { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }
    }

    /// <summary>Validates and atomically publishes one portable *.world.json artifact.</summary>
    public static class WorldExchangeV2FilePublisher
    {
        public static WorldExchangePublishResult WriteAtomically(
            WorldExchangeV2Artifact artifact,
            string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
                return Failure("destination.invalid", "A destination path is required.");
            if (!destinationPath.EndsWith(".world.json", StringComparison.OrdinalIgnoreCase))
                return Failure("destination.extension", "World Exchange artifacts must use the .world.json extension.");
            if (!WorldExchangeV2JsonWriter.TrySerialize(artifact, out byte[] bytes, out string code, out string message))
                return Failure(code, message);

            string temporaryPath = null;
            try
            {
                string fullPath = Path.GetFullPath(destinationPath);
                string directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory))
                    return Failure("destination.invalid", "A destination directory is required.");
                Directory.CreateDirectory(directory);
                temporaryPath = Path.Combine(
                    directory,
                    Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }

                if (File.Exists(fullPath))
                    File.Replace(temporaryPath, fullPath, null);
                else
                    File.Move(temporaryPath, fullPath);
                temporaryPath = null;
                return new WorldExchangePublishResult(true, null, null);
            }
            catch (Exception)
            {
                return Failure("artifact.publish-failed", "The validated artifact could not be atomically published.");
            }
            finally
            {
                if (temporaryPath != null)
                {
                    try
                    {
                        if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                    }
                    catch (Exception)
                    {
                        // The original destination remains intact; a leftover temp file is not a published artifact.
                    }
                }
            }
        }

        private static WorldExchangePublishResult Failure(string code, string message)
        {
            return new WorldExchangePublishResult(false, code, message);
        }
    }
}

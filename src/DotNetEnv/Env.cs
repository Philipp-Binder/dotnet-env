using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DotNetEnv.Extensions;

namespace DotNetEnv
{
    public class Env
    {
        public const string DEFAULT_ENVFILENAME = ".env";

        public static IEnumerable<KeyValuePair<string, string>> LoadMulti(string[] paths, EnvLoadOptions? options = null)
        {
            return paths.Aggregate(
                Array.Empty<KeyValuePair<string, string>>(),
                (kvps, path) => kvps.Concat(Load(path, options, kvps)).ToArray()
            );
        }

        public static IEnumerable<KeyValuePair<string, string>> Load(string path = null, EnvLoadOptions? options = null)
            => Load(path, options, null);

        private static IEnumerable<KeyValuePair<string, string>> Load(string path, EnvLoadOptions? options,
            IEnumerable<KeyValuePair<string, string>> previousValues)
        {
            options ??= EnvLoadOptions.Default;

            var file = Path.GetFileName(path);
            if (file == null || file == string.Empty) file = DEFAULT_ENVFILENAME;
            var dir = Path.GetDirectoryName(path);
            if (dir == null || dir == string.Empty) dir = Directory.GetCurrentDirectory();
            path = Path.Combine(dir, file);

            if (options.Value.OnlyExactPath)
            {
                if (!File.Exists(path)) path = null;
            }
            else
            {
                while (!File.Exists(path))
                {
                    var parent = Directory.GetParent(dir);
                    if (parent == null)
                    {
                        path = null;
                        break;
                    }

                    dir = parent.FullName;
                    path = Path.Combine(dir, file);
                }
            }

            // in production, there should be no .env file, so this should be the common code path
            if (path == null)
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }

            return LoadContents(File.ReadAllText(path), options, previousValues);
        }

        public static IEnumerable<KeyValuePair<string, string>> Load(Stream file, EnvLoadOptions? options = null)
        {
            using var reader = new StreamReader(file);
            return LoadContents(reader.ReadToEnd(), options);
        }

        public static IEnumerable<KeyValuePair<string, string>> LoadContents(string contents,
            EnvLoadOptions? options = null)
            => LoadContents(contents, options, null);

        private static IEnumerable<KeyValuePair<string, string>> LoadContents(string contents,
            EnvLoadOptions? options, IEnumerable<KeyValuePair<string, string>> previousValues)
        {
            options ??= EnvLoadOptions.Default;
            previousValues = previousValues?.ToArray() ?? Array.Empty<KeyValuePair<string, string>>();

            var clobber = options.Value.ClobberOption is ClobberOption.Clobber;
            var dictionaryOption = clobber ? CreateDictionaryOption.TakeLast : CreateDictionaryOption.TakeFirst;

            var actualValues =
                (options.Value.EnvVarOption.HasFlag(EnvVarOption.IncludeForInterpolation)
                    ? GetEnvVarSnapshot()
                    : Array.Empty<KeyValuePair<string, string>>())
                .Concat(previousValues)
                .ToDotEnvDictionary(dictionaryOption);

            var pairs = Parsers.ParseDotenvFile(contents, clobber, actualValues);

            // for NoClobber, remove pairs which are exactly contained in previousValues or present in EnvironmentVariables
            var envDictionary = (clobber || options.Value.ClobberOption != ClobberOption.NoClobberButReturnActualValues
                    ? pairs
                    : pairs.Where(p =>
                        previousValues.All(pv => pv.Key != p.Key) &&
                        Environment.GetEnvironmentVariable(p.Key) == null))
                .ToDotEnvDictionary(dictionaryOption);

            if (options.Value.EnvVarOption >= EnvVarOption.SetProcess)
                SetEnvironmentVariables(options.Value, envDictionary);

            return envDictionary;
        }

        private static void SetEnvironmentVariables(EnvLoadOptions options,
            IEnumerable<KeyValuePair<string, string>> unClobberedPairs)
        {
            var checkExistence =
                options.ClobberOption is ClobberOption.NoClobberButReturnActualValues or ClobberOption.NoClobber;
            foreach (var pair in unClobberedPairs)
            {
                if (checkExistence && Environment.GetEnvironmentVariable(pair.Key) != null)
                    continue;

                if (options.EnvVarOption.HasFlag(EnvVarOption.SetProcess))
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                if (options.EnvVarOption.HasFlag(EnvVarOption.SetUser))
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.User);
                if (options.EnvVarOption.HasFlag(EnvVarOption.SetMachine))
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value, EnvironmentVariableTarget.Machine);
            }
        }

        private static KeyValuePair<string, string>[] GetEnvVarSnapshot() =>
            Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                .Select(entry => new KeyValuePair<string, string>(entry.Key.ToString(), entry.Value.ToString()))
                .ToArray();

        public static string GetString(string key, string fallback = null) =>
            Environment.GetEnvironmentVariable(key) ?? fallback;

        public static bool GetBool(string key, bool fallback = false) =>
            bool.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : fallback;

        public static int GetInt(string key, int fallback = 0) =>
            int.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : fallback;

        public static double GetDouble(string key, double fallback = 0) =>
            double.TryParse(Environment.GetEnvironmentVariable(key), NumberStyles.Any, CultureInfo.InvariantCulture,
                out var value)
                ? value
                : fallback;

        [Obsolete($"Use {nameof(EnvLoadOptions)} instead")]
        public static LoadOptions DoNotSetEnvVars() => LoadOptions.DoNotSetEnvVars();
        [Obsolete($"Use {nameof(EnvLoadOptions)} instead")]
        public static LoadOptions NoClobber() => LoadOptions.NoClobber();
        [Obsolete($"Use {nameof(EnvLoadOptions)} instead")]
        public static LoadOptions TraversePath() => LoadOptions.TraversePath();
        [Obsolete($"Use {nameof(EnvLoadOptions)} instead")]
        public static LoadOptions ExcludeEnvVars() => LoadOptions.ExcludeEnvVars();
    }
}

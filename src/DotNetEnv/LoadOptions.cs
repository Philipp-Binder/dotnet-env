using System;
using System.Collections.Generic;

namespace DotNetEnv
{
    public record struct EnvLoadOptions
    {
        public static EnvLoadOptions Default = new()
        {
            EnvVarOption = EnvVarOption.Default,
            ClobberOption = ClobberOption.Clobber,
            OnlyExactPath = true,
        };

        public EnvVarOption EnvVarOption { get; set; }
        public ClobberOption ClobberOption { get; set; }
        public bool OnlyExactPath { get; set; }

        public static EnvLoadOptions DoNotSetEnvVars(EnvLoadOptions options) =>
            options with { EnvVarOption = options.EnvVarOption & ~EnvVarOption.SetAll };

        public static EnvLoadOptions NoClobber(EnvLoadOptions options) =>
            options with { ClobberOption = ClobberOption.NoClobber };

        public static EnvLoadOptions TraversePath(EnvLoadOptions options) =>
            options with { OnlyExactPath = false };
    }

    public enum ClobberOption
    {
        Clobber = 0,
        NoClobber = 1,
        NoClobberButReturnActualValues = 2,
    }

    [Flags]
    public enum EnvVarOption
    {
        Default = IncludeForInterpolation | SetProcess,
        SetAll = SetProcess | SetUser | SetMachine,

        None = 0,
        IncludeForInterpolation = 1,
        SetProcess = 2,
        SetUser = 4,
        SetMachine = 8,
    }

    [Obsolete]
    public class LoadOptions
    {
        public static implicit operator EnvLoadOptions(LoadOptions loadOptions)
        {
            loadOptions ??= DEFAULT;
            return new EnvLoadOptions
            {
                EnvVarOption = (loadOptions.SetEnvVars
                    ? EnvVarOption.Default
                    : EnvVarOption.None) | (loadOptions.IncludeEnvVars ? EnvVarOption.IncludeForInterpolation : 0),
                ClobberOption = loadOptions.ClobberExistingVars
                    ? ClobberOption.Clobber
                    : ClobberOption.NoClobberButReturnActualValues,
                OnlyExactPath = loadOptions.OnlyExactPath,
            };
        }

        public static readonly LoadOptions DEFAULT = new LoadOptions();

        /// <summary>Whether Environment Variables are set or not (process only; not persistent).</summary>
        public bool SetEnvVars { get; }
        /// <summary>When set, the last found occurence is taken, otherwise the first.</summary>
        public bool ClobberExistingVars { get; }
        /// <summary>
        /// When set, only the exact given path of env files is taken. <br />
        /// Otherwise, the filename gets extracted and searched in every parent directory until a match is found.
        /// </summary>
        public bool OnlyExactPath { get; }
        /// <summary>Whether Environment Variables are included for Interpolation.</summary>
        public bool IncludeEnvVars { get; }

        public LoadOptions(
            bool setEnvVars = true,
            bool clobberExistingVars = true,
            bool onlyExactPath = true,
            bool includeEnvVars = true
        ) {
            SetEnvVars = setEnvVars;
            ClobberExistingVars = clobberExistingVars;
            OnlyExactPath = onlyExactPath;
            IncludeEnvVars = includeEnvVars;
        }

        public LoadOptions(
            LoadOptions old,
            bool? setEnvVars = null,
            bool? clobberExistingVars = null,
            bool? onlyExactPath = null,
            bool? includeEnvVars = null
        ) {
            SetEnvVars = setEnvVars ?? old.SetEnvVars;
            ClobberExistingVars = clobberExistingVars ?? old.ClobberExistingVars;
            OnlyExactPath = onlyExactPath ?? old.OnlyExactPath;
            IncludeEnvVars = includeEnvVars ?? old.IncludeEnvVars;
        }

        [Obsolete("Use DoNotSetEnvVars instead")]
        public static LoadOptions NoEnvVars(LoadOptions options = null) =>
            DoNotSetEnvVars(options);
        public static LoadOptions DoNotSetEnvVars (LoadOptions options = null) =>
            options == null ? DEFAULT.DoNotSetEnvVars() : options.DoNotSetEnvVars();

        public static LoadOptions NoClobber (LoadOptions options = null) =>
            options == null ? DEFAULT.NoClobber() : options.NoClobber();

        public static LoadOptions TraversePath (LoadOptions options = null) =>
            options == null ? DEFAULT.TraversePath() : options.TraversePath();

        public static LoadOptions ExcludeEnvVars (LoadOptions options = null) =>
            options == null ? DEFAULT.ExcludeEnvVars() : options.ExcludeEnvVars();

        [Obsolete("Use DoNotSetEnvVars instead")]
        public LoadOptions NoEnvVars () => DoNotSetEnvVars();
        public LoadOptions DoNotSetEnvVars () => new LoadOptions(this, setEnvVars: false);
        public LoadOptions NoClobber () => new LoadOptions(this, clobberExistingVars: false);
        public LoadOptions TraversePath () => new LoadOptions(this, onlyExactPath: false);
        public LoadOptions ExcludeEnvVars () => new LoadOptions(this, includeEnvVars: false);

        public IEnumerable<KeyValuePair<string, string>> Load (string path = null) => Env.Load(path, this);
        public IEnumerable<KeyValuePair<string, string>> LoadMulti (string[] paths) => Env.LoadMulti(paths, this);
    }
}

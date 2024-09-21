using System.Collections.Generic;

namespace DotNetEnv
{
    public record LoadOptions(bool SetEnvVars = true, bool ClobberExistingVars = true, bool OnlyExactPath = true)
    {
        public static readonly LoadOptions DEFAULT = new LoadOptions();

        public static LoadOptions NoEnvVars(LoadOptions options = null) => (options ?? new LoadOptions()) with { SetEnvVars = false };
        public static LoadOptions NoClobber (LoadOptions options = null) => (options ?? new LoadOptions()) with { ClobberExistingVars = false };
        public static LoadOptions TraversePath (LoadOptions options = null) => (options ?? new LoadOptions()) with { OnlyExactPath = false };

        public LoadOptions NoEnvVars() => this with { SetEnvVars = false };
        public LoadOptions NoClobber () => this with { ClobberExistingVars = false };
        public LoadOptions TraversePath () => this with { OnlyExactPath = false };

        public IEnumerable<KeyValuePair<string, string>> Load (string path = null) => Env.Load(path, this);
        public IEnumerable<KeyValuePair<string, string>> LoadMulti (string[] paths) => Env.LoadMulti(paths, this);
    }
}

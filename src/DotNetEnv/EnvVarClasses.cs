using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotNetEnv
{
    public class EnvVarEnumeration : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly IEnumerable<KeyValuePair<string, string>> _envVars;

        public EnvVarEnumeration(IEnumerable<KeyValuePair<string, string>> envVars)
        {
            _envVars = envVars;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _envVars.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_envVars).GetEnumerator();
        }
    }

    public class EnvVarDictionary : Dictionary<string, string>
    {
        public EnvVarDictionary(IDictionary<string, string> envVars)
            : base(envVars)
        {
        }
    }

    public static class Extensions
    {
        public static EnvVarDictionary UseLastOccurrence(this EnvVarEnumeration @this)
            => new EnvVarDictionary(@this
                .GroupBy(kv => kv.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value));
    }
}
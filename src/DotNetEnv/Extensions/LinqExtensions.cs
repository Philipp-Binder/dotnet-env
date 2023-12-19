using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetEnv.Extensions
{
    public static class LinqExtensions
    {
        public static Dictionary<string, string> ToDotEnvDictionary(this IEnumerable<KeyValuePair<string, string>> @this, CreateDictionaryOption option = CreateDictionaryOption.TakeLast)
        {
            switch (option)
            {
                case CreateDictionaryOption.Throw:
                    return @this.ToDictionary(x => x.Key, x => x.Value);
                case CreateDictionaryOption.TakeFirst:
                    return @this.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.First().Value);
                case CreateDictionaryOption.TakeLast:
                    return @this.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Last().Value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(option), option, null);
            }
        }
    }
}

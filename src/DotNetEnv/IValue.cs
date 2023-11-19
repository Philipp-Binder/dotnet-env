using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DotNetEnv
{
    public interface IValue
    {
        string GetValue(Dictionary<string, string> actualValues = null);
    }

    public class ValueInterpolated : IValue
    {
        private readonly string _id;
        private readonly string _origin;

        public ValueInterpolated(string id, string origin)
        {
            _id = id;
            _origin = origin;
        }

        public string GetValue(Dictionary<string, string> actualValues)
        {
            return actualValues?.TryGetValue(_id, out var actualValue) == true ? actualValue : string.Empty;
        }
    }

    public class ValueActual : IValue
    {
        private readonly string _value;

        public ValueActual (IEnumerable<string> strs)
            : this(string.Join(string.Empty, strs)) {}

        public ValueActual (string str)
        {
            _value = str;
        }

        public string GetValue(Dictionary<string, string> actualValues)
        {
            return _value;
        }
    }

    public class ValueCalculator
    {
        private readonly IEnumerable<IValue> _values;
        public string Value => GetValue(null);

        public ValueCalculator (IEnumerable<IValue> values)
        {
            _values = values;
        }

        public string GetValue(Dictionary<string, string> actualValues)
        {
            return string.Join(string.Empty, _values.Select(val => val.GetValue(actualValues)));
        }
    }
}

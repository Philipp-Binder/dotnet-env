using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Sprache;
using DotNetEnv;

namespace DotNetEnv.Tests
{
    public class ParserTests : IDisposable
    {
        // C# wow that you can't handle 32 bit unicode as chars. wow. strings for 4 byte chars.
        private static readonly string RocketChar = char.ConvertFromUtf32(0x1F680); // 🚀

        private const string EXCEPT_CHARS = "'\"$";

        private const string EV_TEST = "ENVVAR_TEST";
        private const string EV_DNE = "EV_DNE";
        private const string EV_TEST_1 = "EV_TEST_1";
        private const string EV_TEST_2 = "EV_TEST_2";

        private Dictionary<string,string> oldEnvvars = new Dictionary<string,string>();
        private static readonly string[] ALL_EVS = { EV_TEST, EV_DNE, EV_TEST_1, EV_TEST_2 };

        public ParserTests ()
        {
            foreach (var ev in ALL_EVS)
            {
                oldEnvvars[ev] = Environment.GetEnvironmentVariable(ev);
            }

            Environment.SetEnvironmentVariable(EV_TEST, "ENV value");
        }

        public void Dispose ()
        {
            foreach (var ev in ALL_EVS)
            {
                Environment.SetEnvironmentVariable(ev, oldEnvvars[ev]);
            }
        }

        [Fact]
        public void ParseIdentifier ()
        {
            Assert.Equal("name", Parsers.Identifier.Parse("name"));
            Assert.Equal("_n", Parsers.Identifier.Parse("_n"));
            Assert.Equal("__", Parsers.Identifier.Parse("__"));
            Assert.Equal("_0", Parsers.Identifier.Parse("_0"));
            Assert.Equal("a_b", Parsers.Identifier.Parse("a_b"));
            Assert.Equal("_a_b", Parsers.Identifier.Parse("_a_b"));
            Assert.Equal("a.b", Parsers.Identifier.Parse("a.b"));
            Assert.Equal("a-b", Parsers.Identifier.Parse("a-b"));
            Assert.Throws<ParseException>(() => Parsers.Identifier.End().Parse("\"name"));
            Assert.Throws<ParseException>(() => Parsers.Identifier.End().Parse("0name"));
            Assert.Throws<ParseException>(() => Parsers.Identifier.End().Parse(".a.b"));
            Assert.Throws<ParseException>(() => Parsers.Identifier.End().Parse("-a.b"));
            Assert.Throws<ParseException>(() => Parsers.Identifier.End().Parse("a!b"));
            Assert.Throws<ParseException>(() => Parsers.Identifier.End().Parse("a?b"));
            Assert.Throws<ParseException>(() => Parsers.Identifier.End().Parse("a*b"));
        }

        [Fact]
        public void ParseOctalByte ()
        {
            Assert.Equal(33, Parsers.OctalByte.Parse(@"\41"));
            Assert.Equal(33, Parsers.OctalByte.Parse(@"\041"));
            Assert.Equal(90, Parsers.OctalByte.Parse(@"\132"));

            // NOTE that bash accepts values outside of ASCII range?
            // printf "\412"
            //Assert.Equal(???, Parsers.OctalChar.Parse(@"\412"));
        }

        [Fact]
        public void ParseOctalChar ()
        {
            Assert.Equal("!", Parsers.OctalChar.Parse(@"\41"));
            Assert.Equal("!", Parsers.OctalChar.Parse(@"\041"));
            Assert.Equal("Z", Parsers.OctalChar.Parse(@"\132"));

            // as above for values outside of ASCII range

            // TODO: tests for octal combinations to utf8?
        }

        [Fact]
        public void ParseHexByte ()
        {
            Assert.Equal(90, Parsers.HexByte.Parse(@"\x5a"));
        }

        [Fact]
        public void ParseUtf8Char ()
        {
            // https://stackoverflow.com/questions/602912/how-do-you-echo-a-4-digit-unicode-character-in-bash
            // printf '\xE2\x98\xA0'
            // printf ☠ | hexdump  # hexdump has bytes flipped per word (2 bytes, 4 hex)

            Assert.Equal("Z", Parsers.Utf8Char.Parse(@"\x5A"));
            Assert.Equal("®", Parsers.Utf8Char.Parse(@"\xc2\xae"));
            Assert.Equal("☠", Parsers.Utf8Char.Parse(@"\xE2\x98\xA0"));
            Assert.Equal(RocketChar, Parsers.Utf8Char.Parse(@"\xF0\x9F\x9A\x80"));

            Assert.Equal(
                "日本",
                Parsers.Utf8Char.Parse(@"\xe6\x97\xa5")
                    + Parsers.Utf8Char.Parse(@"\xe6\x9c\xac")
            );
        }

        [Fact]
        public void ParseUtf16Char ()
        {
            Assert.Equal("®", Parsers.Utf16Char.Parse(@"\u00ae"));
            Assert.Equal("®", Parsers.Utf16Char.Parse(@"\uae"));

            Assert.Equal("®", Encoding.Unicode.GetString(new byte[] { 0xae, 0x00 }));
        }

        [Fact]
        public void ParseUtf32Char ()
        {
            Assert.Equal(RocketChar, Parsers.Utf32Char.Parse(@"\U0001F680"));
            Assert.Equal(RocketChar, Parsers.Utf32Char.Parse(@"\U1F680"));

            Assert.Equal(RocketChar, Encoding.UTF32.GetString(new byte[] { 0x80, 0xf6, 0x01, 0x00 }));
            Assert.Equal(RocketChar, Encoding.UTF32.GetString(new byte[] { 0x80, 0xf6, 0x1, 0x0 }));
        }

        [Fact]
        public void ParseEscapedChar ()
        {
            Assert.Equal("\b", Parsers.EscapedChar.Parse("\\b"));
            Assert.Equal("'", Parsers.EscapedChar.Parse("\\'"));
            Assert.Equal("\"", Parsers.EscapedChar.Parse("\\\""));
            Assert.Throws<ParseException>(() => Parsers.EscapedChar.End().Parse("\n"));
        }

        [Fact]
        public void ParseInterpolatedEnvVar ()
        {
            Assert.Equal("ENV value", Parsers.InterpolatedEnvVar.Parse("$ENVVAR_TEST").GetValue());
            Assert.Equal("ENV value", Parsers.InterpolatedBracesEnvVar.Parse("${ENVVAR_TEST}").GetValue());
        }

        [Fact]
        public void ParseInterpolated ()
        {
            Assert.Equal("ENV value", Parsers.InterpolatedValue.Parse("$ENVVAR_TEST").GetValue());
            Assert.Equal("ENV value", Parsers.InterpolatedValue.Parse("${ENVVAR_TEST}").GetValue());
            Assert.Equal("", Parsers.InterpolatedValue.Parse("${ENVVAR_TEST_DNE}").GetValue());
        }

        [Fact]
        public void ParseNotControlNorWhitespace ()
        {
            Assert.Equal("a", Parsers.NotControlNorWhitespace(EXCEPT_CHARS).Parse("a"));
            Assert.Equal("%", Parsers.NotControlNorWhitespace(EXCEPT_CHARS).Parse("%"));
            Assert.Throws<ParseException>(() => Parsers.NotControlNorWhitespace(EXCEPT_CHARS).End().Parse(" "));
            Assert.Throws<ParseException>(() => Parsers.NotControlNorWhitespace(EXCEPT_CHARS).End().Parse("\n"));
            Assert.Throws<ParseException>(() => Parsers.NotControlNorWhitespace(EXCEPT_CHARS).End().Parse("'"));
            Assert.Throws<ParseException>(() => Parsers.NotControlNorWhitespace(EXCEPT_CHARS).End().Parse("\""));
            Assert.Throws<ParseException>(() => Parsers.NotControlNorWhitespace(EXCEPT_CHARS).End().Parse("$"));
        }

        [Fact]
        public void ParseSpecialChar ()
        {
            Assert.Equal("Z", Parsers.SpecialChar.Parse(@"\x5A"));
            Assert.Equal("®", Parsers.SpecialChar.Parse(@"\xc2\xae"));
            Assert.Equal("☠", Parsers.SpecialChar.Parse(@"\xE2\x98\xA0"));
            Assert.Equal(RocketChar, Parsers.SpecialChar.Parse(@"\xF0\x9F\x9A\x80"));
            Assert.Equal(
                "日本",
                Parsers.SpecialChar.Parse(@"\xe6\x97\xa5")
                    + Parsers.SpecialChar.Parse(@"\xe6\x9c\xac")
            );

            Assert.Equal("®", Parsers.SpecialChar.Parse(@"\u00ae"));
            Assert.Equal("®", Parsers.SpecialChar.Parse(@"\uae"));

            Assert.Equal(RocketChar, Parsers.SpecialChar.Parse(@"\U0001F680"));
            Assert.Equal(RocketChar, Parsers.SpecialChar.Parse(@"\U1F680"));

            Assert.Equal("\b", Parsers.SpecialChar.Parse("\\b"));
            Assert.Equal("\\m", Parsers.SpecialChar.Parse("\\m"));
            Assert.Equal("'", Parsers.SpecialChar.Parse("\\'"));
            Assert.Equal("\"", Parsers.SpecialChar.Parse("\\\""));

            // this caught a bug, once upon a time, where backslashes were
            // getting processed by NCNW rather than EscapedChar inside SpecialChar
            var parser = Parsers.SpecialChar
                .Or(Parsers.NotControlNorWhitespace("\"$"))
                .Or(Parse.WhiteSpace.AtLeastOnce().Text());
            Assert.Equal("\"", parser.Parse("\\\""));

            Assert.Throws<ParseException>(() => Parsers.SpecialChar.End().Parse("a"));
            Assert.Throws<ParseException>(() => Parsers.SpecialChar.End().Parse("%"));
            Assert.Throws<ParseException>(() => Parsers.SpecialChar.End().Parse(" "));
            Assert.Throws<ParseException>(() => Parsers.SpecialChar.End().Parse("\n"));
        }

        [Fact]
        public void ParseComment ()
        {
            Assert.Equal(" comment 1", Parsers.Comment.Parse("# comment 1"));
            Assert.Equal("", Parsers.Comment.Parse("#"));
            Assert.Equal(" ", Parsers.Comment.Parse("# "));
        }

        [Fact]
        public void ParseEmpty ()
        {
            var kvp = new KeyValuePair<string, string>(null, null);

            Assert.Equal(kvp, Parsers.Empty.Parse("# comment 1"));
            Assert.Equal(kvp, Parsers.Empty.Parse("# comment 2\r\n"));
            Assert.Equal(kvp, Parsers.Empty.Parse("# comment 3\n"));

            Assert.Equal(kvp, Parsers.Empty.Parse(""));
            Assert.Equal(kvp, Parsers.Empty.Parse("\r\n"));
            Assert.Equal(kvp, Parsers.Empty.Parse("\n"));

            Assert.Equal(kvp, Parsers.Empty.Parse("   # comment 1"));
            Assert.Equal(kvp, Parsers.Empty.Parse("    \r\n"));
            Assert.Equal(kvp, Parsers.Empty.Parse("#export EV_DNE=\"\\xe6\\x97\\xa5 $ENVVAR_TEST 本\"#ccccc\n"));
        }

        [Fact]
        public void ParseUnquotedValue ()
        {
            Assert.Equal("abc", Parsers.UnquotedValue.Parse("abc").Value);
            Assert.Equal("a b c", Parsers.UnquotedValue.Parse("a b c").Value);
            Assert.Equal("041", Parsers.UnquotedValue.Parse("041").Value);
            Assert.Equal("日本", Parsers.UnquotedValue.Parse("日本").Value);
            // TODO: is it possible to get the system to recognize when a complete unicode char is present and start the next one then, without a space?
//            Assert.Equal("日本", Parsers.UnquotedValue.Parse(@"\xe6\x97\xa5\xe6\x9c\xac"));

            Assert.Throws<ParseException>(() => Parsers.UnquotedValue.End().Parse("0\n1"));
            Assert.Throws<ParseException>(() => Parsers.UnquotedValue.End().Parse("'"));

            Assert.Equal("a\\?b", Parsers.UnquotedValue.Parse("a\\?b").Value);
            Assert.Equal(@"\xe6\x97\xa5ENV value本", Parsers.UnquotedValue.Parse("\\xe6\\x97\\xa5${ENVVAR_TEST}本").Value);
        }

        [Fact]
        public void ParseDoubleQuotedValueContents ()
        {
            Assert.Equal("abc", Parsers.DoubleQuotedValueContents.Parse("abc").Value);
            Assert.Equal("a b c", Parsers.DoubleQuotedValueContents.Parse("a b c").Value);
            Assert.Equal("0\n1", Parsers.DoubleQuotedValueContents.Parse("0\n1").Value);
            Assert.Equal("日 本", Parsers.DoubleQuotedValueContents.Parse(@"\xe6\x97\xa5 \xe6\x9c\xac").Value);
            Assert.Equal("☠ ®", Parsers.DoubleQuotedValueContents.Parse(@"\xE2\x98\xA0 \uae").Value);

            Assert.Equal("日 ENV value 本", Parsers.DoubleQuotedValueContents.Parse("\\xe6\\x97\\xa5 $ENVVAR_TEST 本").Value);

            Assert.Equal("a\"b c", Parsers.DoubleQuotedValueContents.Parse("a\\\"b c").Value);
            Assert.Equal("a'b c", Parsers.DoubleQuotedValueContents.Parse("a'b c").Value);
        }

        [Fact]
        public void ParseSingleQuotedValueContents ()
        {
            Assert.Equal("abc", Parsers.SingleQuotedValueContents.Parse("abc").Value);
            Assert.Equal("a b c", Parsers.SingleQuotedValueContents.Parse("a b c").Value);
            Assert.Equal("0\n1", Parsers.SingleQuotedValueContents.Parse("0\n1").Value);
            Assert.Equal(@"\xe6\x97\xa5 \xe6\x9c\xac", Parsers.SingleQuotedValueContents.Parse(@"\xe6\x97\xa5 \xe6\x9c\xac").Value);
            Assert.Equal(@"\xE2\x98\xA0 \uae", Parsers.SingleQuotedValueContents.Parse(@"\xE2\x98\xA0 \uae").Value);

            Assert.Equal("\\xe6\\x97\\xa5 $ENVVAR_TEST 本", Parsers.SingleQuotedValueContents.Parse("\\xe6\\x97\\xa5 $ENVVAR_TEST 本").Value);

            Assert.Equal("a\"b c", Parsers.SingleQuotedValueContents.Parse("a\"b c").Value);
        }

        [Fact]
        public void ParseSingleQuotedValue ()
        {
            Assert.Equal("abc", Parsers.SingleQuotedValue.Parse("'abc'").Value);
            Assert.Equal("a b c", Parsers.SingleQuotedValue.Parse("'a b c'").Value);
            Assert.Equal("0\n1", Parsers.SingleQuotedValue.Parse("'0\n1'").Value);
            Assert.Equal("a\"bc", Parsers.SingleQuotedValue.Parse("'a\"bc'").Value);

            Assert.Equal("\\xe6\\x97\\xa5 $ENVVAR_TEST 本", Parsers.SingleQuotedValue.Parse("'\\xe6\\x97\\xa5 $ENVVAR_TEST 本'").Value);

            Assert.Throws<ParseException>(() => Parsers.SingleQuotedValue.End().Parse("'a\\'b c'").Value);
        }

        [Fact]
        public void ParseDoubleQuotedValue ()
        {
            Assert.Equal("abc", Parsers.DoubleQuotedValue.Parse("\"abc\"").Value);
            Assert.Equal("a b c", Parsers.DoubleQuotedValue.Parse("\"a b c\"").Value);
            Assert.Equal("0\n1", Parsers.DoubleQuotedValue.Parse("\"0\n1\"").Value);
            Assert.Equal("a'bc", Parsers.DoubleQuotedValue.Parse("\"a'bc\"").Value);
            Assert.Equal("a\"bc", Parsers.DoubleQuotedValue.Parse("\"a\\\"bc\"").Value);

            Assert.Equal("日 ENV value 本", Parsers.DoubleQuotedValue.Parse("\"\\xe6\\x97\\xa5 $ENVVAR_TEST 本\"").Value);
        }

        [Fact]
        public void ParseValue ()
        {
            Assert.Equal("abc", Parsers.Value.Parse("abc").Value);
            Assert.Equal("a b c", Parsers.Value.Parse("a b c").Value);
            Assert.Equal("041", Parsers.Value.Parse("041").Value);
            Assert.Equal("日本", Parsers.Value.Parse("日本").Value);
            // TODO: is it possible to get the system to recognize when a complete unicode char is present and start the next one then, without a space?
//            Assert.Equal("日本", Parsers.Value.Parse(@"\xe6\x97\xa5\xe6\x9c\xac"));

            Assert.Throws<ParseException>(() => Parsers.Value.End().Parse("0\n1"));
            Assert.Throws<ParseException>(() => Parsers.Value.End().Parse("'"));

            Assert.Equal(@"\xe6\x97\xa5", Parsers.Value.Parse(@"\xe6\x97\xa5").Value);
            Assert.Equal(@"\xE2\x98\xA0", Parsers.Value.Parse(@"\xE2\x98\xA0").Value);

            Assert.Equal("abc", Parsers.Value.Parse("'abc'").Value);
            Assert.Equal("a b c", Parsers.Value.Parse("'a b c'").Value);
            Assert.Equal("0\n1", Parsers.Value.Parse("'0\n1'").Value);
            Assert.Equal(@"\xe6\x97\xa5 \xe6\x9c\xac", Parsers.Value.Parse(@"'\xe6\x97\xa5 \xe6\x9c\xac'").Value);
            Assert.Equal(@"\xE2\x98\xA0 \uae", Parsers.Value.Parse(@"'\xE2\x98\xA0 \uae'").Value);

            Assert.Equal("abc", Parsers.Value.Parse("\"abc\"").Value);
            Assert.Equal("a b c", Parsers.Value.Parse("\"a b c\"").Value);
            Assert.Equal("0\n1", Parsers.Value.Parse("\"0\n1\"").Value);
            Assert.Equal("日 本", Parsers.Value.Parse("\"\\xe6\\x97\\xa5 \\xe6\\x9c\\xac\"").Value);
            Assert.Equal("☠ ®", Parsers.Value.Parse("\"\\xE2\\x98\\xA0 \\uae\"").Value);

            Assert.Equal("日 ENV value 本", Parsers.Value.Parse("\"\\xe6\\x97\\xa5 $ENVVAR_TEST 本\"").Value);
        }

        [Fact]
        public void ParseAssignment ()
        {
            Action<string, string, string> testParse = (key, value, input) =>
            {
                var kvp = Parsers.Assignment.Parse(input);
                Assert.Equal(key, kvp.Key);
                Assert.Equal(value, kvp.Value);
            };

            testParse("EV_DNE", "abc", "EV_DNE=abc");
            testParse("EV_DNE", "a b c", "EV_DNE=a b c");
            testParse("EV_DNE", "041", "EV_DNE=041 # comment");
            // Note that there are no comments without whitespace in unquoted strings!
            testParse("EV_DNE", "日本#c", "EV_DNE=日本#c");

            testParse("EV_DNE", @"\xe6\x97\xa5 \xe6\x9c\xac", @"EV_DNE=\xe6\x97\xa5 \xe6\x9c\xac");
            testParse("EV_DNE", @"\xE2\x98\xA0 \uae", @"EV_DNE=\xE2\x98\xA0 \uae");

            var kvp = Parsers.Assignment.Parse("EV_DNE=");
            Assert.Equal("EV_DNE", kvp.Key);
            Assert.Equal("", kvp.Value);
            // Note that dotnet returns null if the env var is empty -- even if it was set to empty!
            Assert.Null(Environment.GetEnvironmentVariable("EV_DNE"));

            // TODO: is it possible to get the system to recognize when a complete unicode char is present and start the next one then, without a space?
//            Assert.Equal("EV_DNE=日本", Parsers.Assignment.Parse(@"EV_DNE=\xe6\x97\xa5\xe6\x9c\xac"));

            Assert.Throws<ParseException>(() => Parsers.Assignment.End().Parse("EV_DNE='"));
            Assert.Throws<ParseException>(() => Parsers.Assignment.End().Parse("EV_DNE=0\n1"));

            testParse("EV_DNE", "abc", "EV_DNE='abc'");
            testParse("EV_DNE", "a b c", "EV_DNE='a b c' # comment");
            testParse("EV_DNE", "0\n1", "EV_DNE='0\n1'");
            testParse("EV_DNE", @"\xe6\x97\xa5 \xe6\x9c\xac", @"set -x EV_DNE='\xe6\x97\xa5 \xe6\x9c\xac'#c");
            testParse("EV_DNE", @"\xE2\x98\xA0 \uae", @"EV_DNE='\xE2\x98\xA0 \uae'#c");

            testParse("EV_DNE", "abc", "EV_DNE=\"abc\"");
            testParse("EV_DNE", "a b c", "set EV_DNE=\"a b c\" # comment");
            testParse("EV_DNE", "0\n1", "EV_DNE=\"0\n1\"");
            testParse("EV_DNE", "日 本", "export EV_DNE=\"\\xe6\\x97\\xa5 \\xe6\\x9c\\xac\"#c");
            testParse("EV_DNE", "☠ ®", "EV_DNE=\"\\xE2\\x98\\xA0 \\uae\"");

            testParse("EV_DNE", "日 ENV value 本", "export EV_DNE=\"\\xe6\\x97\\xa5 $ENVVAR_TEST 本\"#ccccc");

            testParse("exportEV_DNE", "abc", "exportEV_DNE=\"abc\"");

            testParse("EV_DNE", "a b c", "EV_DNE = 'a b c' # comment");
            testParse("EV_DNE", "a b c", "EV_DNE= \"a b c\" # comment");
            testParse("EV_DNE", "a b c", "EV_DNE ='a b c' # comment");
            testParse("EV_DNE", "abc", "EV_DNE = abc # comment");
            testParse("EV_DNE", "VAL UE", "EV_DNE=VAL UE");
            testParse("EV_DNE", "VAL UE", "EV_DNE=VAL UE #comment");

            Assert.Throws<ParseException>(() => Parsers.Assignment.End().Parse("EV_DNE='a b c'EV_TEST_1=more"));
            Assert.Throws<ParseException>(() => Parsers.Assignment.End().Parse("EV_DNE='a b c' EV_TEST_1=more"));
        }

        [Fact]
        public void ParseDotenvFile ()
        {
            Action<KeyValuePair<string, string>[], string> testParse = (expecteds, input) =>
            {
                var outputs = Parsers.ParseDotenvFile(input, Parsers.SetEnvVar).ToArray();
                Assert.Equal(expecteds.Length, outputs.Length);

                for (var i = 0; i < outputs.Length; i++)
                {
                    Assert.Equal(expecteds[i].Key, outputs[i].Key);
                    Assert.Equal(expecteds[i].Value, outputs[i].Value);
                    Assert.Equal(expecteds[i].Value, Environment.GetEnvironmentVariable(outputs[i].Key));
                }
            };

            string contents;
            KeyValuePair<string, string>[] expecteds;

            contents = @"";
            expecteds = new KeyValuePair<string, string>[] {};
            testParse(expecteds, contents);

            contents = @"EV_DNE=abc";
            expecteds = new[] {
                new KeyValuePair<string, string>("EV_DNE", "abc"),
            };
            testParse(expecteds, contents);

            contents = "SET EV_DNE=\"0\n1\"";
            expecteds = new[] {
                new KeyValuePair<string, string>("EV_DNE", "0\n1"),
            };
            testParse(expecteds, contents);

            contents = "EV_DNE=0\n1";
            Assert.Throws<ParseException>(() => Parsers.ParseDotenvFile(contents, Parsers.SetEnvVar));

            contents = @"
# this is a header

export EV_DNE='a b c' #works!
";
            expecteds = new[] {
                new KeyValuePair<string, string>("EV_DNE", "a b c"),
            };
            testParse(expecteds, contents);

            contents = "# this is a header\nexport EV_DNE='d e f' #works!";
            expecteds = new[] {
                new KeyValuePair<string, string>("EV_DNE", "d e f"),
            };
            testParse(expecteds, contents);

            contents = "#\n# this is a header\n#\n\nexport EV_DNE='g h i' #yep still\n";
            expecteds = new[] {
                new KeyValuePair<string, string>("EV_DNE", "g h i"),
            };
            testParse(expecteds, contents);

            contents = "#\n# this is a header\n#\n\nexport EV_DNE=\"\\xe6\\x97\\xa5 $ENVVAR_TEST 本\" #yep still\n";
            expecteds = new[] {
                new KeyValuePair<string, string>("EV_DNE", "日 ENV value 本"),
            };
            testParse(expecteds, contents);

            contents = @"#
# this is a header
#

export EV_DNE='x y z' #yep still

     EV_TEST_1='日 $ENVVAR_TEST 本'#ccccc
#export EV_DNE='日 $ENVVAR_TEST 本'#ccccc

SET EV_TEST_2='☠
®'#c

ENVVAR_TEST = ' yahooooo '
";
            expecteds = new[] {
                new KeyValuePair<string, string>("EV_DNE", "x y z"),
                new KeyValuePair<string, string>("EV_TEST_1", "日 $ENVVAR_TEST 本"),
                new KeyValuePair<string, string>("EV_TEST_2", "☠\n®"),
                new KeyValuePair<string, string>("ENVVAR_TEST", " yahooooo "),
            };
            testParse(expecteds, contents);
        }
    }
}

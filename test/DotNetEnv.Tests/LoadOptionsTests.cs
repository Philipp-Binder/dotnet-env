using System;
using DotNetEnv.Tests.XUnit;
using Xunit;

namespace DotNetEnv.Tests
{
    public class LoadOptionsTests
    {
        /// <summary>
        /// Data: _, optionsUnderTest, expectedSetEnvVars, expectedClobberExistingVars, expectedOnlyExactPath, expectedIncludeEnvVars
        /// </summary>
        public static readonly TheoryData<string, LoadOptions, bool , bool, bool, bool>
            LoadOptionTestCombinations = new IndexedTheoryData<LoadOptions, bool, bool, bool, bool>()
            {
                { Env.DoNotSetEnvVars(), false, true, true, true },
                { Env.NoClobber(), true, false, true, true },
                { Env.TraversePath(), true, true, false, true },
                { Env.ExcludeEnvVars(), true, true, true, false },

                { LoadOptions.DoNotSetEnvVars(), false, true, true, true },
                { LoadOptions.NoClobber(), true, false, true, true },
                { LoadOptions.TraversePath(), true, true, false, true },
                { LoadOptions.ExcludeEnvVars(), true, true, true, false },

                { new LoadOptions(), true, true, true, true },
                { new LoadOptions().DoNotSetEnvVars(), false, true, true, true },
                { new LoadOptions().NoClobber(), true, false, true, true },
                { new LoadOptions().TraversePath(), true, true, false, true },
                { new LoadOptions().ExcludeEnvVars(), true, true, true, false },

                { Env.DoNotSetEnvVars().NoClobber().TraversePath().ExcludeEnvVars(), false, false, false, false },

                { Env.DoNotSetEnvVars().NoClobber().TraversePath(), false, false, false, true },
                { Env.DoNotSetEnvVars().NoClobber().ExcludeEnvVars(), false, false, true, false },
                { Env.DoNotSetEnvVars().TraversePath().ExcludeEnvVars(), false, true, false, false },
                { Env.NoClobber().TraversePath().ExcludeEnvVars(), true, false, false, false },

                { Env.DoNotSetEnvVars().NoClobber(), false, false, true, true },
                { Env.NoClobber().TraversePath(), true, false, false, true },
                { Env.TraversePath().ExcludeEnvVars(), true, true, false, false },
                { Env.DoNotSetEnvVars().ExcludeEnvVars(), false, true, true, false },
                { Env.DoNotSetEnvVars().TraversePath(), false, true, false, true },
                { Env.NoClobber().ExcludeEnvVars(), true, false, true, false },
            };

        [Theory]
        [MemberData(nameof(LoadOptionTestCombinations))]
        public void LoadOptionsShouldHaveCorrectPropertiesSet(string _, LoadOptions optionsUnderTest,
            bool expectedSetEnvVars, bool expectedClobberExistingVars, bool expectedOnlyExactPath, bool expectedIncludeEnvVars)
        {
            Assert.Equal(expectedSetEnvVars, optionsUnderTest.SetEnvVars);
            Assert.Equal(expectedClobberExistingVars, optionsUnderTest.ClobberExistingVars);
            Assert.Equal(expectedOnlyExactPath, optionsUnderTest.OnlyExactPath);
            Assert.Equal(expectedIncludeEnvVars, optionsUnderTest.IncludeEnvVars);
        }
    }
}

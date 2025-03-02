using Microsoft.Extensions.Configuration;

namespace DotNetEnv.Configuration
{
    public class EnvConfigurationSource : IConfigurationSource
    {
        private readonly string[] paths;

        private readonly EnvLoadOptions? options;

        public EnvConfigurationSource(
            string[] paths,
            EnvLoadOptions? options)
        {
            this.paths = paths;
            this.options = options;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvConfigurationProvider(paths, options);
        }
    }
}

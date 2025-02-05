using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using System;
using System.Collections.Generic;
using Metalama.Framework.Code;

namespace Doc.AspectConfiguration
{
    // The aspect itself, consuming the configuration.
    public class LogAttribute : OverrideMethodAspect, IHierarchicalOptionsProvider
    {
        public string? Category { get; init; }

        public override dynamic? OverrideMethod()
        {
            var options = meta.Target.Method.Enhancements().GetOptions<LoggingOptions>();

            var message = $"{options.Category}: Executing {meta.Target.Method}.";
            Console.WriteLine( message );

            return meta.Proceed();
        }

        public IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context )
        {
            return new[] { new LoggingOptions() { Category = Category } };
        }
    }
}
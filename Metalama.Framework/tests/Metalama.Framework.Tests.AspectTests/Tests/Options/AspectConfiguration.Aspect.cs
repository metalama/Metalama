// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
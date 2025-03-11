// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Doc.AspectConfiguration
{
    // Options for the [Log] aspects.
    public class LoggingOptions : IHierarchicalOptions<IMethod>, IHierarchicalOptions<INamedType>,
                                  IHierarchicalOptions<INamespace>, IHierarchicalOptions<ICompilation>
    {
        public string? Category { get; init; }

        object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
        {
            var other = (LoggingOptions)changes;

            return new LoggingOptions { Category = other.Category ?? Category };
        }

        IHierarchicalOptions? IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context ) => null;
    }
}
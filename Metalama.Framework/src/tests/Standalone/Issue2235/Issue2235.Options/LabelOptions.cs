// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Issue2235.Options;

[RunTimeOrCompileTime]
[HierarchicalOptions( InheritedByDerivedTypes = false, InheritedByOverridingMembers = false )]
public sealed class LabelOptions : IHierarchicalOptions<IDeclaration>
{
    public string? Label { get; init; }

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (LabelOptions) changes;

        return new LabelOptions { Label = other.Label ?? this.Label };
    }

    IHierarchicalOptions IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context ) => new LabelOptions { Label = "default" };
}

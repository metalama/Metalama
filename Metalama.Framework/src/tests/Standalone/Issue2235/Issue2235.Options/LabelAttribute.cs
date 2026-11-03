// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using System;
using System.Collections.Generic;

namespace Issue2235.Options;

[RunTimeOrCompileTime]
[AttributeUsage( AttributeTargets.Class | AttributeTargets.Method )]
public sealed class LabelAttribute : Attribute, IHierarchicalOptionsProvider
{
    public LabelAttribute( string label )
    {
        this.Label = label;
    }

    public string Label { get; }

    IEnumerable<IHierarchicalOptions> IHierarchicalOptionsProvider.GetOptions( in OptionsProviderContext context )
        => new[] { new LabelOptions { Label = this.Label } };
}

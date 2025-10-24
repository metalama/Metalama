// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Queries.Diagnostics;
using System;

namespace Metalama.Framework.Engine.Extensibility;

public abstract class ContributorKind
{
    protected ContributorKind( string name ) 
    {
        this.Name = name;
    }

    public string Name { get; }
    
    internal bool IsExtension { get; init; } = true;
    
    public bool IsDesignTimeValidator { get; init; }

    public abstract Type Type { get; }

    internal static ContributorKind<IAspectSource> AspectSource { get; } = new( nameof(AspectSource) ) { IsExtension = false };

    internal static ContributorKind<IHierarchicalOptionsSource> HierarchicalOptionsSource { get; } =
        new( nameof(HierarchicalOptionsSource) ) { IsExtension = false };

    internal static ContributorKind<IDiagnosticSource> DiagnosticSource { get; } = new( nameof(DiagnosticSource) );

    internal static ContributorKind<TransitiveAspectInstance> TransitiveAspectInstance { get; } =
        new( nameof(TransitiveAspectInstance) );

    internal static ContributorKind<SerializableTransitiveAspectInstance> SerializableTransitiveAspectInstance { get; } =
        new( nameof(SerializableTransitiveAspectInstance) );

    public override string ToString() => this.Name;
}

#pragma warning disable SA1402
public class ContributorKind<T> : ContributorKind
#pragma warning restore SA1402
    where T : IContributor
{
    public ContributorKind( string name ) : base( name ) { }

    public override Type Type => typeof(T);
}
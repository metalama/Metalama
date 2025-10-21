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
    public bool IsExtension { get; }

    public bool IsTransitive { get; }

    public abstract Type Type { get; }

    protected ContributorKind( bool isExtension, bool isTransitive )
    {
        this.IsExtension = isExtension;
        this.IsTransitive = isTransitive;
    }

    internal static ContributorKind<IAspectSource> AspectSource { get; } = new( false, false );

    internal static ContributorKind<IHierarchicalOptionsSource> HierarchicalOptionsSource { get; } = new( false, false );

    internal static ContributorKind<IDiagnosticSource> DiagnosticSource { get; } = new( true, false );

    internal static ContributorKind<TransitiveAspectInstance> TransitiveAspect { get; } = new( true, true );

    internal static ContributorKind<SerializableTransitiveAspectInstance> SerializableTransitiveAspect { get; } = new( true, true );
}

#pragma warning disable SA1402
public class ContributorKind<T> : ContributorKind
#pragma warning restore SA1402
    where T : IContributor
{
    public ContributorKind( bool isExtension, bool isTransitive ) : base( isExtension, isTransitive ) { }

    public override Type Type => typeof(T);
}
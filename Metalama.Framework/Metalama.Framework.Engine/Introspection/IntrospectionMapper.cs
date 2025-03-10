// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection;

[PublicAPI]
public static class IntrospectionMapper
{
    public static ImmutableArray<IIntrospectionDiagnostic> ToIntrospectionDiagnostics(
        this ImmutableArray<Diagnostic> diagnostics,
        ICompilation compilation,
        IntrospectionDiagnosticSource source )
        => diagnostics.Select( x => new IntrospectionDiagnostic( x, compilation, source ) )
            .ToImmutableArray<IIntrospectionDiagnostic>();

    public static IIntrospectionAspectClass AggregateAspectClasses( IAspectClass aspectClass, IEnumerable<IIntrospectionAspectInstance> instances )
        => new AggregatedIntrospectionAspectClass( aspectClass, instances );

    public static IIntrospectionAspectLayer AggregateAspectLayers( IIntrospectionAspectClass aspectClass, IEnumerable<IIntrospectionAspectLayer> layers )
        => new AggregatedIntrospectionAspectLayer( aspectClass, layers.First() );
}
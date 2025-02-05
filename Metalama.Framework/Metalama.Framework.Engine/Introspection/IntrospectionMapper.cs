// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
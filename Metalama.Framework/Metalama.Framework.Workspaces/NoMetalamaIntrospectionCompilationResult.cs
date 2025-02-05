// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Introspection;
using System.Collections.Immutable;

namespace Metalama.Framework.Workspaces;

internal sealed class NoMetalamaIntrospectionCompilationResult : IIntrospectionCompilationResult
{
    public NoMetalamaIntrospectionCompilationResult( bool isSuccessful, ICompilation transformedCode, ImmutableArray<IIntrospectionDiagnostic> diagnostics )
    {
        this.HasMetalamaSucceeded = isSuccessful;
        this.TransformedCode = transformedCode;
        this.Diagnostics = diagnostics;
    }

    public ImmutableArray<IIntrospectionDiagnostic> Diagnostics { get; }

    public ImmutableArray<IIntrospectionAspectLayer> AspectLayers => ImmutableArray<IIntrospectionAspectLayer>.Empty;

    public ImmutableArray<IIntrospectionAspectInstance> AspectInstances => ImmutableArray<IIntrospectionAspectInstance>.Empty;

    public ImmutableArray<IIntrospectionAspectClass> AspectClasses => ImmutableArray<IIntrospectionAspectClass>.Empty;

    public ImmutableArray<IIntrospectionAdvice> Advice => ImmutableArray<IIntrospectionAdvice>.Empty;

    public ImmutableArray<IIntrospectionTransformation> Transformations => ImmutableArray<IIntrospectionTransformation>.Empty;

    public string Name => this.TransformedCode.DeclaringAssembly.Identity.Name;

    public bool HasMetalamaSucceeded { get; }

    public ICompilation TransformedCode { get; }

    public bool IsMetalamaEnabled => false;
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Represents the result of a method of <see cref="IAdviceFactory"/>. We use a single class to implement all supported interfaces.
/// </summary>
internal abstract class AdviceResult : IAdviceResult
{
    public AdviceKind AdviceKind { get; init; }

    public AdviceOutcome Outcome { get; init; }

    public ImmutableArray<Diagnostic> Diagnostics { get; init; } = ImmutableArray<Diagnostic>.Empty;

    // This property is used only by the introspection API.
    public ImmutableArray<ITransformation> Transformations { get; internal set; } = ImmutableArray<ITransformation>.Empty;

    public CompilationModel? Compilation { get; set; }

    protected T Resolve<T>( IRef<T>? reference, [CallerMemberName] string? caller = null )
        where T : class, ICompilationElement
    {
        if ( reference == null )
        {
            throw this.CreateException( caller );
        }

        return reference.GetTarget( this.Compilation.AssertNotNull() )
            .Assert( d => d is not IDeclarationBuilder );
    }

    private InvalidOperationException CreateException( [CallerMemberName] string? caller = null )
        => new( $"Cannot get {caller} when the outcome is {this.Outcome}." );
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Pipeline;

[PublicAPI]
public readonly struct FallibleResult<T>
{
    private readonly T _result;

    public bool IsSuccessful { get; }

    public T Value => this.IsSuccessful ? this._result : throw new InvalidOperationException( "Cannot get the result of the operation because it failed." );

    public static FallibleResult<T> Failed => new( default!, false );

    public static FallibleResult<T> Succeeded( T value ) => new( value, true );

    public static implicit operator FallibleResult<T>( T value ) => new( value, true );

    private FallibleResult( T result, bool isSuccessful )
    {
        this._result = result;
        this.IsSuccessful = isSuccessful;
    }

    public override string ToString() => this.IsSuccessful ? this._result?.ToString() ?? "null" : "<Failed>";
}

[PublicAPI]
public readonly struct FallibleResultWithDiagnostics<T>
{
    private readonly T _result;

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public bool IsSuccessful { get; }

    public string? DebugReason { get; }

    public T Value => this.IsSuccessful ? this._result : throw new InvalidOperationException( "Cannot get the result of the operation because it failed." );

    public static FallibleResultWithDiagnostics<T> Failed( ImmutableArray<Diagnostic> diagnostics, string? debugReason = null )
        => new( default!, false, diagnostics, debugReason );

    public static FallibleResultWithDiagnostics<T> Failed( string? debugReason = null )
        => new( default!, false, ImmutableArray<Diagnostic>.Empty, debugReason );

    public static FallibleResultWithDiagnostics<T> Succeeded( T value, ImmutableArray<Diagnostic> diagnostics = default ) => new( value, true, diagnostics );

    public static implicit operator FallibleResultWithDiagnostics<T>( T value ) => new( value, true, default );

    public FallibleResultWithDiagnostics<TOther> CastFailure<TOther>()
    {
        if ( this.IsSuccessful )
        {
            throw new InvalidOperationException( "Can't cast successful result." );
        }

        return FallibleResultWithDiagnostics<TOther>.Failed( this.Diagnostics, this.DebugReason );
    }

    private FallibleResultWithDiagnostics( T result, bool isSuccessful, ImmutableArray<Diagnostic> diagnostics, string? debugReason = null )
    {
        this._result = result;
        this.IsSuccessful = isSuccessful;
        this.Diagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
        this.DebugReason = debugReason;
    }

    public override string ToString() => this.IsSuccessful ? this._result?.ToString() ?? "null" : "<Failed>";
}
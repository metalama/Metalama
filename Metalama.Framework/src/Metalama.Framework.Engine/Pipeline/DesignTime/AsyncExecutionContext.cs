// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.Pipeline.DesignTime;

/// <summary>
/// A context object used for debugging of locking issues in async methods. It has no use in the release build.
/// </summary>
public sealed class AsyncExecutionContext
{
#if DEBUG
    private readonly string _debugName;
    private ImmutableArray<object> _stack;

    public static AsyncExecutionContext Get( [CallerMemberName] string? debugName = null ) => new( debugName ?? "", ImmutableArray<object>.Empty );

    public AsyncExecutionContext Fork() => new( this._debugName, this._stack );

    private AsyncExecutionContext( string debugName, ImmutableArray<object> stack )
    {
        this._debugName = debugName;
        this._stack = stack;
    }

    public void Push( object obj )
    {
        this._stack = this._stack.Add( obj );
    }

    public void DetectCycle( object obj )
    {
        if ( this._stack.Contains( obj ) )
        {
            throw new AssertionFailedException(
                $"A deadlock was detected, involving the following objects: {string.Join( ", ", this._stack.Select( o => $"'{o}'" ) )}, '{obj}'." );
        }
    }

    public void RequireObject( object obj )
    {
        if ( !this._stack.Contains( obj ) )
        {
            throw new AssertionFailedException( $"The current execution context does not own '{obj}'." );
        }
    }

    public void Pop( object obj )
    {
        if ( this._stack[^1] != obj )
        {
            throw new AssertionFailedException( $"The stack {string.Join( ", ", this._stack.Select( o => $"'{o}'" ) )} was expected to end with '{obj}'." );
        }

        this._stack = this._stack.RemoveAt( this._stack.Length - 1 );
    }

    public override string ToString() => $"DebugName='{this._debugName}', StackHeight={this._stack.Length}";
#else
    private static AsyncExecutionContext _instance = new();

    private AsyncExecutionContext() { }

    public static AsyncExecutionContext Get( [CallerMemberName] string? debugName = null ) => _instance;

    public AsyncExecutionContext Fork() => this;

#endif
}
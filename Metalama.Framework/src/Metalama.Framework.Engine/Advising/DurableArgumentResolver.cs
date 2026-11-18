// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Resolves the durable values of template arguments in the compilation where the template is bound.
/// </summary>
/// <remarks>
/// <para>
/// This class is used by an extension that keeps template arguments across compilations, for instance the arguments of a
/// <see cref="DurableTemplateInvocation"/>. The advice of the framework does not use it, because the arguments of an advice are
/// consumed in the compilation in which they are created.
/// </para>
/// <para>
/// An <see cref="IDurableExpression"/> is resolved to an <see cref="IExpression"/>, and an <see cref="IDurableRef"/> is resolved to the
/// declaration or type that it references. A template therefore receives the same value as if the non-durable value had been passed.
/// </para>
/// <para>
/// Only the top-level values of the arguments are resolved. A durable value nested in an array, a list or a dictionary is passed unchanged.
/// </para>
/// </remarks>
public static class DurableArgumentResolver
{
    /// <summary>
    /// Resolves a durable value in a given compilation.
    /// </summary>
    /// <param name="value">The value of a template argument.</param>
    /// <param name="compilation">The compilation in which the value is resolved.</param>
    /// <returns>
    /// An <see cref="IExpression"/> when <paramref name="value"/> is an <see cref="IDurableExpression"/>, the target of the reference when
    /// <paramref name="value"/> is an <see cref="IDurableRef"/>, or <paramref name="value"/> itself otherwise.
    /// </returns>
    /// <remarks>
    /// An <see cref="IDurableRef"/> that cannot be resolved in <paramref name="compilation"/> causes an exception.
    /// </remarks>
    public static object? Resolve( object? value, CompilationModel compilation )
        => value switch
        {
            IDurableExpression expression => expression.ToExpression( compilation ),
            IDurableRef reference => reference.GetTargetInterface( compilation, null, throwIfMissing: true ),
            _ => value
        };

    /// <summary>
    /// Returns an <see cref="IObjectReader"/> that resolves each value of a given <see cref="IObjectReader"/> with
    /// <see cref="Resolve"/> when the value is read.
    /// </summary>
    /// <param name="reader">The template arguments.</param>
    /// <param name="compilation">The compilation in which the values are resolved.</param>
    /// <remarks>
    /// <para>
    /// When <paramref name="reader"/> is itself returned by this method, the new reader wraps the original reader, so that the values are
    /// resolved in <paramref name="compilation"/> and not in the compilation of the first call.
    /// </para>
    /// <para>
    /// The <see cref="IObjectReader.Source"/> property of the returned reader returns the source object of <paramref name="reader"/>, whose
    /// values are not resolved.
    /// </para>
    /// </remarks>
    public static IObjectReader Wrap( IObjectReader reader, CompilationModel compilation )
    {
        if ( reader is ResolvingObjectReader resolvingReader )
        {
            reader = resolvingReader.Inner;
        }

        if ( reader == ObjectReader.Empty )
        {
            return reader;
        }

        return new ResolvingObjectReader( reader, compilation );
    }

    /// <summary>
    /// An <see cref="IObjectReader"/> that resolves the values of another <see cref="IObjectReader"/> with <see cref="Resolve"/>.
    /// </summary>
    private sealed class ResolvingObjectReader : IObjectReader
    {
        private readonly CompilationModel _compilation;

        public ResolvingObjectReader( IObjectReader inner, CompilationModel compilation )
        {
            this.Inner = inner;
            this._compilation = compilation;
        }

        /// <summary>
        /// Gets the <see cref="IObjectReader"/> whose values are resolved.
        /// </summary>
        public IObjectReader Inner { get; }

        public object? this[ string key ] => Resolve( this.Inner[key], this._compilation );

        public object? Source => this.Inner.Source;

        public IEnumerable<string> Keys => this.Inner.Keys;

        public IEnumerable<object?> Values => this.Inner.Values.Select( v => Resolve( v, this._compilation ) );

        public int Count => this.Inner.Count;

        public bool ContainsKey( string key ) => this.Inner.ContainsKey( key );

        public bool TryGetValue( string key, out object? value )
        {
            if ( this.Inner.TryGetValue( key, out var innerValue ) )
            {
                value = Resolve( innerValue, this._compilation );

                return true;
            }
            else
            {
                value = null;

                return false;
            }
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            foreach ( var pair in this.Inner )
            {
                yield return new KeyValuePair<string, object?>( pair.Key, Resolve( pair.Value, this._compilation ) );
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}

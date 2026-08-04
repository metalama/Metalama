// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
#if !NET6_0_OR_GREATER
using System.Linq;
#endif
using System.Reflection;
using System.Reflection.Emit;

namespace Metalama.Framework.Engine.Utilities.ObjectGraph;

/// <summary>
/// Reads, for one type, the value of every instance field that can hold a reference, through a single method emitted
/// for that type.
/// </summary>
/// <remarks>
/// <para>
/// A walk of a large object graph reads tens of millions of fields, and <see cref="FieldInfo.GetValue"/> costs an
/// argument check, a security check and a boxing operation on each of them. This class instead computes the list of
/// fields once per type, including the fields nested inside value-type fields, and emits a method that reads all of
/// them into an array. A caller that visits many instances of the same type therefore pays the reflection cost once.
/// </para>
/// <para>
/// The emitted method is created with <see cref="DynamicMethod"/> rather than with a compiled expression tree, because
/// it must read private fields of types declared in other assemblies, which requires
/// <c>restrictedSkipVisibility</c>. Where emission is unavailable or fails, the reader falls back to
/// <see cref="FieldInfo.GetValue"/> over the same list of fields, so the result is identical and only slower.
/// </para>
/// </remarks>
internal sealed class ObjectGraphTypeReader
{
    /// <summary>
    /// The maximum nesting depth when descending into the fields of a value type.
    /// </summary>
    /// <remarks>
    /// This is a constant rather than an option, because a reader is cached by type and the flattening happens once,
    /// before any caller is known.
    /// </remarks>
    private const int _maxStructDepth = 6;

    private static readonly object?[] _empty = [];

    private readonly ImmutableArray<FieldInfo[]> _paths;
    private readonly Func<object, object?[]>? _read;

    /// <summary>
    /// Gets the label of each value returned by <see cref="Read"/>, in the same order. A value nested in a value-type
    /// field is labelled with the whole path, such as <c>_entry.Key</c>.
    /// </summary>
    public ImmutableArray<string> Labels { get; }

    public ObjectGraphTypeReader( Type type )
    {
        var paths = ImmutableArray.CreateBuilder<FieldInfo[]>();
        var labels = ImmutableArray.CreateBuilder<string>();

        CollectPaths( type, new List<FieldInfo>(), paths, labels );

        this._paths = paths.ToImmutable();
        this.Labels = labels.ToImmutable();
        this._read = this._paths.IsEmpty ? null : TryEmitReader( type, this._paths );
    }

    /// <summary>
    /// Reads the value of every reference-typed field of <paramref name="obj"/>. An element is <c>null</c> when the
    /// corresponding field is null.
    /// </summary>
    public object?[] Read( object obj )
    {
        if ( this._paths.IsEmpty )
        {
            return _empty;
        }

        if ( this._read != null )
        {
            try
            {
                return this._read( obj );
            }
            catch ( Exception )
            {
                // The emitted method must not be able to fail an analysis, therefore an unexpected failure falls back
                // to reflection instead of propagating.
            }
        }

        return this.ReadByReflection( obj );
    }

    private object?[] ReadByReflection( object obj )
    {
        var values = new object?[this._paths.Length];

        for ( var i = 0; i < this._paths.Length; i++ )
        {
            var current = obj;

            foreach ( var field in this._paths[i] )
            {
                try
                {
                    current = field.GetValue( current );
                }
                catch ( Exception )
                {
                    // Some runtime types refuse access to their fields. Such a field cannot be the cause of a managed
                    // retention that the caller could fix.
                    current = null;
                }

                if ( current == null )
                {
                    break;
                }
            }

            values[i] = current;
        }

        return values;
    }

    /// <summary>
    /// Collects the path to every reference-typed field of a type, descending into value-type fields.
    /// </summary>
    private static void CollectPaths(
        Type type,
        List<FieldInfo> prefix,
        ImmutableArray<FieldInfo[]>.Builder paths,
        ImmutableArray<string>.Builder labels )
    {
        for ( var currentType = type; currentType != null && currentType != typeof(object); currentType = currentType.BaseType )
        {
            FieldInfo[] fields;

            try
            {
                fields = currentType.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
            }
            catch ( Exception )
            {
                return;
            }

            foreach ( var field in fields )
            {
                var fieldType = field.FieldType;

                if ( fieldType.IsPrimitive || fieldType.IsEnum || fieldType.IsPointer || fieldType == typeof(IntPtr) || fieldType == typeof(UIntPtr) )
                {
                    continue;
                }

                // A by-reference-like value cannot be stored in an object, so neither the emitted method nor reflection
                // can read it.
                if ( IsByRefLike( fieldType ) )
                {
                    continue;
                }

                prefix.Add( field );

                if ( fieldType.IsValueType )
                {
                    if ( prefix.Count <= _maxStructDepth )
                    {
                        CollectPaths( fieldType, prefix, paths, labels );
                    }
                }
                else
                {
                    paths.Add( prefix.ToArray() );
                    labels.Add( string.Join( ".", prefix.ConvertAll( f => f.Name ) ) );
                }

                prefix.RemoveAt( prefix.Count - 1 );
            }
        }
    }

    private static bool IsByRefLike( Type type )
    {
#if NET6_0_OR_GREATER
        return type.IsByRefLike;
#else
        // IsByRefLike is not available on .NET Framework, where the attribute is the only reliable signal.
        return type.IsValueType && type.GetCustomAttributesData().Any( a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute" );
#endif
    }

    /// <summary>
    /// Emits a method that reads every collected field of an instance into an array, or returns <c>null</c> when the
    /// runtime does not allow emission.
    /// </summary>
    private static Func<object, object?[]>? TryEmitReader( Type type, ImmutableArray<FieldInfo[]> paths )
    {
        try
        {
            var method = new DynamicMethod(
                "ReadObjectGraphFields",
                typeof(object[]),
                [typeof(object)],
                typeof(ObjectGraphTypeReader).Module,
                skipVisibility: true );

            var il = method.GetILGenerator();

            il.Emit( OpCodes.Ldc_I4, paths.Length );
            il.Emit( OpCodes.Newarr, typeof(object) );

            for ( var i = 0; i < paths.Length; i++ )
            {
                il.Emit( OpCodes.Dup );
                il.Emit( OpCodes.Ldc_I4, i );

                // Load the instance. A boxed value is unboxed to a managed pointer, so that the fields it contains can
                // be reached with ldflda without copying the value.
                il.Emit( OpCodes.Ldarg_0 );

                if ( type.IsValueType )
                {
                    il.Emit( OpCodes.Unbox, type );
                }
                else
                {
                    il.Emit( OpCodes.Castclass, type );
                }

                var path = paths[i];

                // Every field but the last is a value-type field, therefore its address is what the next field is read
                // from.
                for ( var j = 0; j < path.Length - 1; j++ )
                {
                    il.Emit( OpCodes.Ldflda, path[j] );
                }

                il.Emit( OpCodes.Ldfld, path[path.Length - 1] );
                il.Emit( OpCodes.Stelem_Ref );
            }

            il.Emit( OpCodes.Ret );

            return (Func<object, object?[]>) method.CreateDelegate( typeof(Func<object, object?[]>) );
        }
        catch ( Exception )
        {
            // Reflection emit is unavailable on some runtimes and refuses some types. The reader then falls back to
            // reflection, which is slower but produces the same values.
            return null;
        }
    }
}

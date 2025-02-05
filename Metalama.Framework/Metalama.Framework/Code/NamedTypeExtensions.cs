// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for the <see cref="INamedType"/> interface.
/// </summary>
[CompileTime]
public static class NamedTypeExtensions
{
    /// <summary>
    /// Gets the name of a named type in metadata format, i.e. the <c>`1</c>, <c>`2</c>, ... suffixes for generic types.
    /// </summary>
    public static string GetMetadataName( this INamedType type ) => ((ICompilationInternal) type.Compilation).Helpers.GetMetadataName( type );

    /// <summary>
    /// Gets the full name of a named type in metadata format, i.e. with <c>+</c> as the nested type separator and the <c>`1</c>, <c>`2</c>, ... suffixes
    /// for generic types.
    /// </summary>
    public static string GetFullMetadataName( this INamedType type ) => ((ICompilationInternal) type.Compilation).Helpers.GetFullMetadataName( type );

    /// <summary>
    /// Gets all methods of a named type, including the accessors of properties and events.
    /// </summary>
    public static IEnumerable<IMethod> MethodsAndAccessors( this INamedType type )
    {
        foreach ( var m in type.Methods )
        {
            yield return m;
        }

        foreach ( var p in type.Properties )
        {
            if ( p.GetMethod != null )
            {
                yield return p.GetMethod;
            }

            if ( p.SetMethod != null )
            {
                yield return p.SetMethod;
            }
        }

        foreach ( var e in type.Events )
        {
            yield return e.AddMethod;
            yield return e.RemoveMethod;
        }
    }

    /// <summary>
    /// Gets all members of the current type, except nested types.
    /// </summary>
    public static IEnumerable<IMember> Members( this INamedType type )
    {
        foreach ( var m in type.Methods )
        {
            yield return m;
        }

        foreach ( var m in type.Properties )
        {
            yield return m;
        }

        foreach ( var m in type.Fields )
        {
            yield return m;
        }

        foreach ( var m in type.Events )
        {
            yield return m;
        }

        foreach ( var m in type.Indexers )
        {
            yield return m;
        }

        foreach ( var m in type.Constructors )
        {
            yield return m;
        }
    }

    /// <summary>
    /// Gets all members of the current type and members inherited from the base type, except nested types.
    /// </summary>
    public static IEnumerable<IMember> AllMembers( this INamedType type )
    {
        foreach ( var m in type.AllMethods )
        {
            yield return m;
        }

        foreach ( var m in type.AllProperties )
        {
            yield return m;
        }

        foreach ( var m in type.AllFields )
        {
            yield return m;
        }

        foreach ( var m in type.AllEvents )
        {
            yield return m;
        }

        foreach ( var m in type.AllIndexers )
        {
            yield return m;
        }

        foreach ( var m in type.Constructors )
        {
            yield return m;
        }
    }

    /// <summary>
    /// Gets all nested types of the current type, and all recursively all nested types of those nested types, but not the current type.
    /// </summary>
    public static IEnumerable<INamedType> NestedTypes( this INamedType type ) => type.SelectManyRecursive( t => t.Types );

    /// <summary>
    /// Gets all nested types of the current type, and all recursively all nested types of those nested types, including the current type.
    /// </summary>
    public static IEnumerable<INamedType> NestedTypesAndSelf( this INamedType type ) => type.SelectManyRecursive( t => t.Types, true );
}
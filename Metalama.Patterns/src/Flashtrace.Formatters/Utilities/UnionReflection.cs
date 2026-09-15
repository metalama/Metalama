// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Flashtrace.Formatters.Utilities;

/// <summary>
/// Recognizes a union of C# 15 at run time and reads the value of the case that it currently carries.
/// </summary>
/// <remarks>
/// <para>
/// The compiler makes every union implement <c>System.Runtime.CompilerServices.IUnion</c>, which declares the
/// <c>Value</c> property that returns the value of the current case. That interface belongs to the base class library
/// of .NET 11, while this assembly targets .NET Framework 4.7.2, .NET Standard 2.0 and .NET 10, so it cannot be named
/// in source. The interface is therefore matched by its full name over the interfaces that the type implements, which
/// gives the same answer when the .NET 10 assembly is loaded into a .NET 11 application.
/// </para>
/// <para>
/// A type of another assembly that declares an interface of the same full name is recognized as a union as well. That
/// is the cost of matching by name, and the name belongs to a namespace that the base class library reserves for the
/// compiler.
/// </para>
/// <para>
/// The results are cached per type, so a repeated query costs one dictionary lookup.
/// </para>
/// </remarks>
[PublicAPI]
public static class UnionReflection
{
    private const string _unionInterfaceFullName = "System.Runtime.CompilerServices.IUnion";
    private const string _valuePropertyName = "Value";

    private static readonly ConcurrentDictionary<Type, Func<object, object?>?> _caseValueGetters = new();

    /// <summary>
    /// Returns a function that reads the value of the current case of a union, or <c>null</c> when the type is not a
    /// union.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>A function whose parameter is the union, boxed when it is a value type, and whose return value is the
    /// value of the current case, or <c>null</c> when <paramref name="type"/> is not a union.</returns>
    public static Func<object, object?>? GetCaseValueGetterOrNull( Type type ) => _caseValueGetters.GetOrAdd( type, CreateCaseValueGetter );

    /// <summary>
    /// Determines whether a type is a union.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns><c>true</c> if <paramref name="type"/> is a union; otherwise, <c>false</c>.</returns>
    public static bool IsUnion( Type type ) => GetCaseValueGetterOrNull( type ) != null;

    private static Func<object, object?>? CreateCaseValueGetter( Type type )
    {
        var unionInterface = GetUnionInterfaceOrNull( type );

        if ( unionInterface == null )
        {
            return null;
        }

        var valueProperty = unionInterface.GetProperty( _valuePropertyName, BindingFlags.Public | BindingFlags.Instance );

        if ( valueProperty?.GetMethod == null )
        {
            return null;
        }

        // The union is reached through the interface rather than through its own Value property, because the attribute
        // form of a union may declare that property on a base type or on a member provider interface.
        var parameter = Expression.Parameter( typeof(object) );

        var body = Expression.Convert(
            Expression.Property( Expression.Convert( parameter, unionInterface ), valueProperty ),
            typeof(object) );

        return Expression.Lambda<Func<object, object?>>( body, parameter ).Compile();
    }

    private static Type? GetUnionInterfaceOrNull( Type type )
    {
        foreach ( var implementedInterface in type.GetInterfaces() )
        {
            if ( !implementedInterface.IsGenericType && implementedInterface.FullName == _unionInterfaceFullName )
            {
                return implementedInterface;
            }
        }

        return null;
    }
}

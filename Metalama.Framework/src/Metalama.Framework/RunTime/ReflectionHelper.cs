// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.RunTime;

/// <summary>
/// Provides extension methods for reflection operations.
/// </summary>
/// <seealso cref="MethodInfo"/>
/// <seealso cref="ConstructorInfo"/>
public static class ReflectionHelper
{
    /// <summary>
    /// Gets a method from a type by its name, binding flags, and <see cref="MemberInfo.ToString()"/> signature.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="bindingFlags">The binding flags to use when searching for the method.</param>
    /// <param name="signature">The full signature of the method as returned by <see cref="MemberInfo.ToString()"/>.</param>
    /// <returns>The <see cref="MethodInfo"/> matching the specified criteria.</returns>
    /// <exception cref="InvalidOperationException">No method with the specified signature was found.</exception>
    /// <exception cref="AmbiguousMatchException">More than one method with the specified signature was found.</exception>
    public static MethodInfo GetMethod( this Type type, string methodName, BindingFlags bindingFlags, string signature )
    {
        var methods = type.GetMethods( bindingFlags )
            .Where( m => m.Name == methodName && m.ToString() == signature )
            .ToList();

        if ( methods.Count == 1 )
        {
            return methods[0];
        }
        else if ( methods.Count == 0 )
        {
            throw new InvalidOperationException( $"The type '{type}' does not contain a method with signature '{signature}'." );
        }
        else
        {
            throw new AmbiguousMatchException( $"There is more than one method in type '{type}' with signature '{signature}'." );
        }
    }

    /// <summary>
    /// Gets a constructor from a type by its binding flags and <see cref="MemberInfo.ToString()"/> signature.
    /// </summary>
    /// <param name="type">The type containing the constructor.</param>
    /// <param name="bindingFlags">The binding flags to use when searching for the constructor.</param>
    /// <param name="signature">The full signature of the constructor as returned by <see cref="MemberInfo.ToString()"/>.</param>
    /// <returns>The <see cref="ConstructorInfo"/> matching the specified criteria.</returns>
    /// <exception cref="InvalidOperationException">No constructor with the specified signature was found.</exception>
    /// <exception cref="AmbiguousMatchException">More than one constructor with the specified signature was found.</exception>
    public static ConstructorInfo GetConstructor( this Type type, BindingFlags bindingFlags, string signature )
    {
        var constructors = type.GetConstructors( bindingFlags )
            .Where( c => c.ToString() == signature )
            .ToList();

        if ( constructors.Count == 1 )
        {
            return constructors[0];
        }
        else if ( constructors.Count == 0 )
        {
            throw new InvalidOperationException( $"The type '{type}' does not contain a constructor with signature '{signature}'." );
        }
        else
        {
            throw new AmbiguousMatchException( $"There is more than one constructor in type '{type}' with signature '{signature}'." );
        }
    }
}
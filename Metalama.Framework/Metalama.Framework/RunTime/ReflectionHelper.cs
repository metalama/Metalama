// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.RunTime;

public static class ReflectionHelper
{
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
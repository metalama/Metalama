// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.TypeExtensions;

internal static class CovariantTypeExtensionFactoryHelpers
{
    public static IEnumerable<Type> GetAssignableTypes( Type type )
    {
        yield return type;

        var baseType = type.BaseType;

        while ( baseType != null && baseType != typeof(object) )
        {
            yield return baseType;

            baseType = baseType.BaseType;
        }

        foreach ( var interfaceType in type.GetInterfaces() )
        {
            yield return interfaceType;
        }

        yield return typeof(object);
    }
}
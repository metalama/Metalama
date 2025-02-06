// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
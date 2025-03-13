// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.TypeExtensions;

// ReSharper disable once ClassNeverInstantiated.Global
internal class CovariantTypeExtensionFactory<T> : TypeExtensionFactory<T>
    where T : class
{
    public CovariantTypeExtensionFactory( Type genericInterfaceType, Type converterType, Type? roleType )
        : base( genericInterfaceType, converterType, roleType ) { }

    protected override IEnumerable<Type> GetAssignableTypes( Type type ) => CovariantTypeExtensionFactoryHelpers.GetAssignableTypes( type );
}
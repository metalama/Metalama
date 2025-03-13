// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.TypeExtensions;

internal sealed class CovariantTypeExtensionFactory<T, TContext> : TypeExtensionFactory<T, TContext>
    where T : class
{
    public CovariantTypeExtensionFactory( Type genericInterfaceType, Type converterType, Type roleType, TContext? context )
        : base( genericInterfaceType, converterType, roleType, context ) { }

    protected override IEnumerable<Type> GetAssignableTypes( Type type ) => CovariantTypeExtensionFactoryHelpers.GetAssignableTypes( type );
}
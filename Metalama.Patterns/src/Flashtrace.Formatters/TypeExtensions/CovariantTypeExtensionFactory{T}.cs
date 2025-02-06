// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.TypeExtensions;

// ReSharper disable once ClassNeverInstantiated.Global
internal class CovariantTypeExtensionFactory<T> : TypeExtensionFactory<T>
    where T : class
{
    public CovariantTypeExtensionFactory( Type genericInterfaceType, Type converterType, Type? roleType )
        : base( genericInterfaceType, converterType, roleType ) { }

    protected override IEnumerable<Type> GetAssignableTypes( Type type ) => CovariantTypeExtensionFactoryHelpers.GetAssignableTypes( type );
}
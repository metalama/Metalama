// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Introspection.Navigation.Facade;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Metalama.Introspection.Navigation.Tests;

public sealed class FacadeTypeFactoryTests
{
    /// <summary>
    /// Creates a type in a collectible assembly, as the type of a projection in a compiled query is.
    /// </summary>
    private static Type CreateCollectibleType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly( new AssemblyName( "Collectible" + Guid.NewGuid().ToString( "N" ) ), AssemblyBuilderAccess.RunAndCollect );
        var module = assembly.DefineDynamicModule( "Collectible" );
        var type = module.DefineType( "Row", TypeAttributes.Public | TypeAttributes.Class );

        return type.CreateType();
    }

    [Fact]
    public void FacadeOfACollectibleTypeIsReused()
    {
        var type = CreateCollectibleType();
        Assert.True( type.Assembly.IsCollectible );

        var factory = new FacadeTypeFactory();

        // Every row of a projection has the same type: its facade must be built once, not once per row.
        Assert.Same( factory.GetFacadeType( type ), factory.GetFacadeType( type ) );
    }

    [Fact]
    public void AccessorOfACollectibleCollectionIsReused()
    {
        var collectionType = typeof(List<>).MakeGenericType( CreateCollectibleType() );

        Assert.Same( EnumerableAccessor.Get( collectionType ), EnumerableAccessor.Get( collectionType ) );
    }
}

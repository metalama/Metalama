// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug597;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce a generic interface.
        var introducedInterface = builder.IntroduceInterface(
            "IIntroducedInterface",
            buildType: b =>
            {
                b.AddTypeParameter( "T" );
            } );

        // Construct a generic instance of the introduced interface with typeof(object).
        var genericInstance = introducedInterface.Declaration.MakeGenericInstance( typeof(object) );

        // Test 1: typeof(T) where T is an introduced generic type instance.
        builder.IntroduceMethod(
            nameof(TestTypeOf),
            args: new { T = genericInstance },
            buildMethod: b => { b.Name = "TestTypeOfIntroducedGenericInstance"; } );

        // Test 2: typeof(T) where T is an array of an introduced generic type instance.
        var arrayOfGenericInstance = genericInstance.MakeArrayType();

        builder.IntroduceMethod(
            nameof(TestTypeOf),
            args: new { T = (IType) arrayOfGenericInstance },
            buildMethod: b => { b.Name = "TestTypeOfIntroducedGenericInstanceArray"; } );

        // Test 3: typeof(T) where T is a 2D array of an introduced generic type instance.
        var array2DOfGenericInstance = genericInstance.MakeArrayType( 2 );

        builder.IntroduceMethod(
            nameof(TestTypeOf),
            args: new { T = (IType) array2DOfGenericInstance },
            buildMethod: b => { b.Name = "TestTypeOfIntroducedGenericInstanceArray2D"; } );
    }

    [Template]
    private void TestTypeOf<[CompileTime] T>()
    {
        Console.WriteLine( typeof(T).Name );
    }
}

// <target>
[MyAspect]
public partial class TargetClass { }

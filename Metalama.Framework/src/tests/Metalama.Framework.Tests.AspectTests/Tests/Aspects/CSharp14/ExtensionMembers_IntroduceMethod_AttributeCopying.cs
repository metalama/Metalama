// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_IntroduceMethod_AttributeCopying;

[AttributeUsage( AttributeTargets.All, AllowMultiple = true )]
public class MyMethodAttribute : Attribute
{
    public string? Name { get; set; }

    public MyMethodAttribute() { }

    public MyMethodAttribute( string name )
    {
        this.Name = name;
    }
}

[AttributeUsage( AttributeTargets.All )]
public class MyParamAttribute : Attribute { }

[AttributeUsage( AttributeTargets.All )]
public class MyReturnAttribute : Attribute { }

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var extensionBlockBuilder = builder.With( builder.Target.ExtensionBlocks.Single() );

        // Introduce the method with attributes.
        var result = extensionBlockBuilder.IntroduceMethod( nameof(this.MethodWithAttributes) );

        if ( result.Outcome != AdviceOutcome.Default )
        {
            throw new InvalidOperationException( $"IntroduceMethod failed with outcome: {result.Outcome}" );
        }

        var introducedMethod = result.Declaration;

        // Verify that the introduced method has the attributes.
        var methodAttr = introducedMethod.Attributes.FirstOrDefault( a => a.Type.Name == "MyMethodAttribute" );

        if ( methodAttr == null )
        {
            throw new InvalidOperationException( "Introduced method is missing MyMethodAttribute." );
        }

        var returnAttr = introducedMethod.ReturnParameter.Attributes.FirstOrDefault( a => a.Type.Name == "MyReturnAttribute" );

        if ( returnAttr == null )
        {
            throw new InvalidOperationException( "Introduced method return parameter is missing MyReturnAttribute." );
        }

        var paramAttr = introducedMethod.Parameters[0].Attributes.FirstOrDefault( a => a.Type.Name == "MyParamAttribute" );

        if ( paramAttr == null )
        {
            throw new InvalidOperationException( "Introduced method parameter is missing MyParamAttribute." );
        }

        // Now verify that the implicit extension implementation method has the copied attributes.
        var implMethod = introducedMethod.ExtensionImplementationMethod;

        if ( implMethod == null )
        {
            throw new InvalidOperationException( "ExtensionImplementationMethod is null." );
        }

        // Check method-level attribute.
        var implMethodAttr = implMethod.Attributes.FirstOrDefault( a => a.Type.Name == "MyMethodAttribute" );

        if ( implMethodAttr == null )
        {
            throw new InvalidOperationException( "Implementation method is missing MyMethodAttribute - attribute was not copied." );
        }

        // Check return parameter attribute.
        var implReturnAttr = implMethod.ReturnParameter.Attributes.FirstOrDefault( a => a.Type.Name == "MyReturnAttribute" );

        if ( implReturnAttr == null )
        {
            throw new InvalidOperationException( "Implementation method return parameter is missing MyReturnAttribute - attribute was not copied." );
        }

        // Check parameter attribute (first param is receiver, second is the actual param).
        // For instance extension methods, the receiver is param[0], actual params start at param[1].
        if ( implMethod.Parameters.Count < 2 )
        {
            throw new InvalidOperationException( $"Implementation method should have at least 2 parameters (receiver + x), but has {implMethod.Parameters.Count}." );
        }

        var implParamAttr = implMethod.Parameters[1].Attributes.FirstOrDefault( a => a.Type.Name == "MyParamAttribute" );

        if ( implParamAttr == null )
        {
            throw new InvalidOperationException( "Implementation method parameter is missing MyParamAttribute - attribute was not copied." );
        }
    }

    [Template]
    [MyMethod( "OnMethod" )]
    [return: MyReturn]
    public int MethodWithAttributes( [MyParam] int x ) => 0;
}

// <target>
[TheAspect]
internal static class C
{
    extension( int test )
    {
    }
}

#endif

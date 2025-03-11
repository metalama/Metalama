// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Tests.AspectTests.Aspects.CodeModel.ComplexTypedConstant;

internal class Aspect : TypeAspect
{
    [Template]
    private object[] P { get; } = null!;

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var typedConstant = TypedConstant.Create( new object[] { new[] { ConsoleColor.Red }, new object[] { ConsoleColor.Red } } );
        builder.IntroduceField( "f", typeof(object[]), buildField: field => field.InitializerExpression = typedConstant );
        builder.IntroduceProperty( nameof(P), buildProperty: property => property.InitializerExpression = typedConstant );

        var attributeConstructor = ( (INamedType)TypeFactory.GetType( typeof(MyAttribute) ) ).Constructors.Single();
        builder.IntroduceAttribute( AttributeConstruction.Create( attributeConstructor, new[] { typedConstant } ) );
    }
}

internal class MyAttribute : Attribute
{
    public MyAttribute( object[] array ) { }
}

// <target>
[Aspect]
internal class TargetCode { }
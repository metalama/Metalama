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
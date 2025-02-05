using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CompileTimeReturningRunTimeAccess;

internal class Aspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        base.BuildAspect( builder );

        builder.OverrideAccessors( nameof(OverrideGetter) );
    }

    [Template]
    private dynamic? OverrideGetter()
    {
        var field = ( meta.Target.Property.ToFieldOrPropertyInfo().GetCustomAttributes( true ).SingleOrDefault( x => x is IEntityField ) as IEntityField )
                    ?? throw new Exception( "Unable to retrieve field info." );

        Console.WriteLine( field );

        return meta.Proceed();
    }
}

internal interface IEntityField { }

internal class EntityFieldAttribute : Attribute, IEntityField { }

internal class Target
{
    // <target>
    [Aspect]
    [EntityField]
    public string? Name { get; set; }
}
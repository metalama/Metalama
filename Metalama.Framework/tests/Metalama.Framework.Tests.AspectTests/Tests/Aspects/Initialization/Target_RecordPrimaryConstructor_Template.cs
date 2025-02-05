using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Initialization.Target_RecordPrimaryConstructor_Template;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.PrimaryConstructor! ).AddInitializer( nameof(Template), InitializerKind.BeforeInstanceConstructor );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( $"{meta.Target.Type.Name}: {meta.AspectInstance.AspectClass.ShortName}" );
    }
}

// <target>
[Aspect]
public record TargetRecord()
{
    private int Method( int a )
    {
        return a;
    }
}
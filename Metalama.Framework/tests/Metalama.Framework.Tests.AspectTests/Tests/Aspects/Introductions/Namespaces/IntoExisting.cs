using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.IntoExisting;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @namespace = builder.With( builder.Target.ContainingNamespace ).WithChildNamespace( "Implementation" );
        var @class = @namespace.IntroduceClass( "TestClass" );

        builder.IntroduceField( "Field", @class.Declaration );
    }
}

#pragma warning disable CS8618

// <target>
[IntroductionAttribute]
public class TargetType { }
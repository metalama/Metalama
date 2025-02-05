using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.Simple;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @namespace = builder.With( builder.Target.Compilation.GlobalNamespace ).WithChildNamespace( "Implementation" );
        var @class = @namespace.IntroduceClass( "Test" );

        builder.IntroduceField( "Field", @class.Declaration );
    }
}

#pragma warning disable CS8618

// <target>
[IntroductionAttribute]
public class TargetType { }
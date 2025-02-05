using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Namespaces.Recursive;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var outerNamespace = builder.With( builder.Target.Compilation ).WithNamespace( "Outer" );
        var middleNamespace = outerNamespace.WithChildNamespace( "Middle" );
        var innerNamespace = middleNamespace.WithChildNamespace( "Inner" );
        var @class = innerNamespace.IntroduceClass( "Test" );

        builder.IntroduceField( "Field", @class.Declaration );
    }
}

#pragma warning disable CS8618

// <target>
[IntroductionAttribute]
public class TargetType { }
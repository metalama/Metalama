using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0169, CS0414

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.UserDeclarativeAdvice_NonTemplate;

[AttributeUsage( AttributeTargets.Field )]
public class MyAdviceAttribute : DeclarativeAdviceAttribute
{
    public override void BuildAdvice( IMemberOrNamedType templateMember, string templateMemberId, IAspectBuilder<IDeclaration> builder )
    {
        builder.With( (INamedType)builder.Target ).IntroduceField( "_field", TypeFactory.GetType( typeof(string) ).ToNullable() );
    }
}

public class MyAspect : TypeAspect
{
    [MyAdvice]
    private RunTimeOnlyClass f = null!;
}

public class RunTimeOnlyClass { }

// <target>
[MyAspect]
public class Target { }
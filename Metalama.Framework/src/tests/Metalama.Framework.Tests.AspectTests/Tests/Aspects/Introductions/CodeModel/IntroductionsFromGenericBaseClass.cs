// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.CodeModel.IntroductionsFromGenericBaseClass;

[Inheritable]
public class MyInheritableAspectWhichIntroducesAMethod : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.Ignore )]
    public void M() { }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var m = builder.Target.AllMethods.OfName( "M" ).SingleOrDefault();

        if (m != null)
        {
            builder.IntroduceMethod( nameof(CallM), args: new { m } );
        }
    }

    [Template]
    private void CallM( IMethod m )
    {
        m.Invoke();
    }
}

public class AspectWhichCallsTheMethod : TypeAspect { }

// <target>
[MyInheritableAspectWhichIntroducesAMethod]
internal class Foo<T> { }

// <target>
internal class Bar : Foo<int> { }
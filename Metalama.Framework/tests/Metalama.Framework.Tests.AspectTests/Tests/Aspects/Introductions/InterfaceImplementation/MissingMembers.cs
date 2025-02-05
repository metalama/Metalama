using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MissingMembers
{
    /*
     * Tests that missing interface members result in an error.
     */

    public interface IInterface
    {
        int Method();

        event EventHandler Event;

        int Property { get; set; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}
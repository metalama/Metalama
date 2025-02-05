using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.IntroducedDerivedType;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(IntroduceClassAspect), typeof(IntroduceMethodInheritableAspect))]
namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.IntroducedDerivedType
{
    [Inheritable]
    internal class IntroduceMethodInheritableAspect : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override, IsVirtual = true )]
        public int Foo()
        {
            Console.WriteLine( "Introduced!" );

            return meta.Proceed();
        }
    }

    internal class IntroduceClassAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceClass(
                "IntroducedDerived",
                buildType: b => { b.BaseType = builder.Target.Types.OfName( "BaseType" ).Single(); } );
        }
    }

    // <target>
    [IntroduceClassAspect]
    public class Targets
    {
        [IntroduceMethodInheritableAspect]
        public class BaseType { }

        public class ManualDerived : BaseType { }
    }
}
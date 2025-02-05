using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.Bug28810;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(TestAspect), typeof(DeepCloneAttribute) )]

#pragma warning disable CS0169, CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.Bug28810
{
    internal class DeepCloneAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var typedMethod = builder.IntroduceMethod(
                nameof(CloneImpl),
                buildMethod: m =>
                {
                    m.Name = "Clone";
                    m.ReturnType = builder.Target;
                } );

            builder.ImplementInterface( typeof(ICloneable), whenExists: OverrideStrategy.Ignore );
        }

        [Template]
        public virtual dynamic? CloneImpl()
        {
            return null;
        }

        [InterfaceMember( IsExplicit = true )]
        private object Clone()
        {
            // This should call final version of introduced Clone method.
            return meta.This.Clone();
        }
    }

    internal class TestAspect : TypeAspect
    {
        [Introduce]
        public void Foo()
        {
            var baseMethod1 = meta.Target.Type.Methods.OfCompatibleSignature( "Clone", Array.Empty<IType>(), Array.Empty<RefKind?>() ).Single();

            baseMethod1.Invoke();

            var baseMethod2 = meta.Target.Type.Methods.OfExactSignature( "Clone", Array.Empty<IType>() )!;

            baseMethod2.Invoke();
        }
    }

    // <target>
    internal class Targets
    {
        private class NaturallyCloneable : ICloneable
        {
            public object Clone()
            {
                return new NaturallyCloneable();
            }
        }

        [DeepClone]
        [TestAspect]
        private class BaseClass
        {
            private int a;
            private NaturallyCloneable? b;
        }
    }
}
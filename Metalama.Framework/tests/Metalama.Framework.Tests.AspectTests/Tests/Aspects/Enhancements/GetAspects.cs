using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Enhancements.GetAspects
{
    internal class Aspect : OverrideMethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            var targetEnhancements = builder.Target.Enhancements();

            if (!targetEnhancements.GetAspects<Aspect>().Any())
            {
                throw new Exception();
            }

            if (!builder.Target.Enhancements().GetAspects<OverrideMethodAspect>().Any())
            {
                throw new Exception();
            }

            var noAspectEnhancements = builder.Target.DeclaringType.Methods.OfName( "NoAspect" ).Single().Enhancements();

            if (noAspectEnhancements.GetAspects<Aspect>().Any())
            {
                throw new Exception();
            }

            if (noAspectEnhancements.GetAspects<OverrideMethodAspect>().Any())
            {
                throw new Exception();
            }
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }

    internal class TargetCode
    {
        // <target>
        [Aspect]
        private int Method( int a )
        {
            return a;
        }

        private void NoAspect() { }
    }
}
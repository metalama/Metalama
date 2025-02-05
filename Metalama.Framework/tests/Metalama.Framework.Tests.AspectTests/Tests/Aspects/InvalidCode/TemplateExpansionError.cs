using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.TemplateExpansionError
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            meta.CompileTime( CompileTimeExceptionMethod() );

            return default;
        }

        private int CompileTimeExceptionMethod()
        {
            throw new Exception();
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
    }
}
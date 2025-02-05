using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Generic.ProgrammaticOverrideGenericTemplate
{
    internal class Aspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( nameof(OverrideMethod) );
        }

        [Template]
        public T OverrideMethod<T>( T a )
        {
            Console.WriteLine( a );

            return meta.Proceed()!;
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private T Method<T>( T a )
        {
            return a;
        }
    }
}
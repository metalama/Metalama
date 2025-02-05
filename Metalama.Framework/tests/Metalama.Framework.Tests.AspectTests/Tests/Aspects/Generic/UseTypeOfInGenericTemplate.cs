using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Generic.UseTypeOfInGenericTemplate
{
    internal class Aspect : TypeAspect
    {
        [Introduce]
        public T GenericMethod<T>( T a )
        {
            Console.WriteLine( typeof(T).Name );

            return a;
        }
    }

    // <target>
    [Aspect]
    internal class TargetCode { }
}
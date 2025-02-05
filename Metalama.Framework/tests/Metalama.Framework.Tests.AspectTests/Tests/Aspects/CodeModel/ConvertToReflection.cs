using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.ConvertToReflection
{
    // This tests serialization to reflection with types that cannot be constructed using a typeof.

    internal class Aspect : TypeAspect
    {
        [Introduce]
        public void Run()
        {
            foreach (var method in meta.Target.Type.Methods)
            {
                foreach (var parameter in method.Parameters)
                {
                    var type = meta.RunTime( parameter.Type.ToType() );
                }
            }
        }
    }

    // <target>
    [Aspect]
    internal unsafe class TargetCode
    {
        private void SystemTypesOnly( dynamic dyn, dynamic[] dynArray, List<dynamic> dynGeneric ) { }
    }
}
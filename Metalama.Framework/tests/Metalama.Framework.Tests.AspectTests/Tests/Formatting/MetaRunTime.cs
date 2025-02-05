using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.MetaRunTime
{   
    class Aspect : IAspect
    {
        [Template]
        void Template()
        {
            var metalamaRelease = meta.RunTime( meta.CompileTime( new DateTime( 2023, 5, 3 ) ) );
            var now = meta.RunTime( DateTime.Now );
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28769
{
    internal class ConvertToRunTimeAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            // The cast to IEnumerable is to avoid referencing the LinqExtensions in the engine assembly.
            var parameterNamesCompileTime = ( (IEnumerable<IParameter>)meta.Target.Parameters ).Select( p => p.Name ).ToArray();
            var parameterNames = meta.RunTime( parameterNamesCompileTime );

            return null;
        }
    }

    internal class TargetCode
    {
        // <target>
        [ConvertToRunTimeAspect]
        private void Method( string a, int c, DateTime e ) { }

        // </target>
    }
}
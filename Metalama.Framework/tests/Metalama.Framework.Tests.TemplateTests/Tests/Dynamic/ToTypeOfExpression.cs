using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.ToTypeOfExpression
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic Template()
        {
            meta.InsertComment( "Should return typeof(int)." );

            return meta.Target.Parameters[0].Type.ToTypeOfExpression();
        }
    }

    internal class TargetCode
    {
        private Type Method( int a )
        {
            // Should return typeof(int).
            return typeof(void);
        }
    }
}
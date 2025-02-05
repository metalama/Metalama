#if TEST_OPTIONS
// @AddHtmlTitles
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.IfStatements.HtmlTitles
{
    internal class RunTimeClass
    {
        public void RunTimeMethod() { }
    }

    [CompileTime]
    internal class CompileTimeClass
    {
        public void CompileTimeMethod() { }
    }

    // Base list should be documented.
    [RunTimeOrCompileTime]
    internal class Aspect : OverrideMethodAspect, IAspect<INamedType>
    {
        // Override should be documented.
        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException();
        }

        public override void BuildAspect( IAspectBuilder<IMethod> builder ) { }

        // Explicit interface implementation should be documented.
        void IAspect<INamedType>.BuildAspect( IAspectBuilder<INamedType> builder ) { }

        [Template]
        private dynamic? Template()
        {
            var m = meta.Target.Method;
            var p = meta.Target.Method.Parameters[0].Value;

            return meta.Proceed();
        }

        void IEligible<INamedType>.BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }
    }
}
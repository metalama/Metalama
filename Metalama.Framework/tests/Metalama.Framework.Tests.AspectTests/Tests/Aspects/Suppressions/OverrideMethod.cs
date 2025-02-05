#if TEST_OPTIONS
// @ClearIgnoredDiagnostics
#endif

#if !TESTRUNNER

// Disable the warning in the main build, not during tests.
#pragma warning disable CS0219
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Suppressions.OverrideMethod
{
    public class SuppressWarningAttribute : MethodAspect
    {
        private static readonly SuppressionDefinition _suppression = new( "CS0219" );

        public SuppressWarningAttribute() { }

        [Template]
        public dynamic? Override()
        {
            var a = 0;

            return meta.Proceed();
        }

        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( nameof(Override) );
            builder.Diagnostics.Suppress( _suppression, builder.Target );
        }
    }

    // <target>
    internal class TargetClass
    {
        [SuppressWarning]
        private void M2( string m )
        {
            var x = 0;
        }

        // CS0219 expected 
        private void M1( string m )
        {
            var y = 0;
        }
    }
}
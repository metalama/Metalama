#if TEST_OPTIONS
// @ClearIgnoredDiagnostics
#endif

#if !TESTRUNNER

// Disable the warning in the main build, not during tests.
#pragma warning disable CS0169
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Suppressions.Fields
{
    public class SuppressWarningAttribute : FieldAspect
    {
        private static readonly SuppressionDefinition _suppression1 = new( "CS0169" );
        private static readonly SuppressionDefinition _suppression2 = new( "CS0649" );

        public SuppressWarningAttribute() { }

        public override void BuildAspect( IAspectBuilder<IField> builder )
        {
            builder.Diagnostics.Suppress( _suppression1, builder.Target );
            builder.Diagnostics.Suppress( _suppression2, builder.Target );
        }
    }

    // <target>
    internal class TargetClass
    {
        // CS0169 expected here.
        private int x;

        [SuppressWarning]
        private int y;

        [SuppressWarning]
        private int w, z;
    }
}
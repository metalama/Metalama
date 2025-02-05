#if TEST_OPTIONS
// @IncludeAllSeverities
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.ReportFromInitialize
{
    public class ErrorAttribute : OverrideMethodAspect
    {
        private static readonly DiagnosticDefinition _error = new( "MY001", Severity.Error, "Error" );
        private static readonly DiagnosticDefinition _warning = new( "MY002", Severity.Warning, "Warning" );
        private static readonly DiagnosticDefinition _info = new( "MY003", Severity.Info, "Info" );
        private static readonly DiagnosticDefinition _hidden = new( "MY004", Severity.Hidden, "Hidden" );

        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Diagnostics.Report( _error );
            builder.Diagnostics.Report( _warning );
            builder.Diagnostics.Report( _info );
            builder.Diagnostics.Report( _hidden );
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException( "This code should not be emitted." );
        }
    }

    // <target>
    internal class TargetClass
    {
        [Error]
        public static int Add( int a, int b )
        {
            if (a == 0)
            {
                throw new ArgumentOutOfRangeException( nameof(a) );
            }

            return a + b;
        }
    }
}
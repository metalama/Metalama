// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using System.Linq;

// Verifies that an aspect applied to several members that the compiler synthesized for a record orders them
// deterministically instead of throwing. See issue #1945.
//
// The comparer that orders the aspect instances of one layer orders them by the position of the primary declaration
// syntax of their target, and every implicitly declared member of a record reports the record declaration as that
// syntax. The comparer used to order such a tie only when both targets were methods, so the pair made of the
// synthesized EqualityContract property and any of the synthesized methods reached an assertion failure.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.AspectOnSynthesizedMembers
{
    internal class Fabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            var records = amender.SelectTypes().Where( t => t.IsRecord );

            records.SelectMany( t => t.Properties ).AddAspect<ReportAspect>();
            records.SelectMany( t => t.Methods ).AddAspect<ReportAspect>();
        }
    }

    internal class ReportAspect : IAspect<IMemberOrNamedType>
    {
        private static readonly DiagnosticDefinition<IDeclaration> _warning =
            new( "MY001", Severity.Warning, "The aspect was applied to {0}." );

        public void BuildAspect( IAspectBuilder<IMemberOrNamedType> builder )
        {
            builder.Diagnostics.Report( _warning.WithArguments( builder.Target ) );
        }

        // The default implementation of the interface member is a default interface implementation, which .NET
        // Framework does not support, so the method is declared here.
        public void BuildEligibility( IEligibilityBuilder<IMemberOrNamedType> builder ) { }
    }

    // <target>
    public record Circle( double Radius );
}

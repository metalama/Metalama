// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(NET8_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Fabrics;
using System.Linq;

// Verifies the acceptance criterion of issue #1945: an aspect that targets several members that the compiler
// synthesized for a union orders them deterministically and does not throw.
//
// The comparer that orders the aspect instances of one layer orders them by the position of the primary declaration
// syntax of their target. Of the members that the compiler synthesizes for a union declaration, the Value property
// reports the union declaration as its primary syntax and the case constructors report none, so the case
// constructors are ordered by the branch that orders two declarations with no syntax. The union type reports the
// same syntax as the Value property, but the two are never compared, because the aspect instances of a type and the
// aspect instances of its members belong to two pipeline steps of different declaration depth.
//
// The pair that the generalised comparer orders and the previous one did not is therefore a record one, and it is
// pinned by Tests/Aspects/Records/AspectOnSynthesizedMembers.cs.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp15.Unions.AspectOnSynthesizedUnionMembers
{
    internal class Fabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            var unions = amender.SelectTypes().Where( t => t.IsUnion );

            unions.AddAspect<ReportAspect>();
            unions.SelectMany( t => t.Properties ).AddAspect<ReportAspect>();
            unions.SelectMany( t => t.Constructors ).AddAspect<ReportAspect>();
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

    public record Circle( double Radius );

    public record Rectangle( double Width, double Height );

    // <target>
    public union Shape( Circle, Rectangle );
}

// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @IncludePolyfill(IUnion,UnionAttribute)
// @RemoveOutputCode
#endif

using Metalama.Extensions.Multicast;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Extensions.Multicast.AspectTests.Union;

// Pins the behaviour of finding UT-14d of issue #1946: which instance constructors an assembly-level multicast aspect
// selects on a union declaration.
//
// The selection admits an implicitly declared constructor when it has no parameter, which is the parameterless
// constructor that the compiler adds to a type that declares none. Roslyn adds that constructor to a union as well,
// because every case constructor of a union takes one parameter, and materializing it produces code that the compiler
// rejects with CS9375: a constructor declared in a union must chain through a this(...) initializer. The union is
// therefore excluded from that part of the filter.
//
// The struct in the test carries no constructor of its own, so its implicit parameterless constructor is reported and
// shows that the filter still admits the constructor that it is meant to admit. The case constructors of the union are
// implicitly declared and take one parameter each, so they are not reported either.
//
// The expected output of this test is provisional. This library reproduces the multicasting of PostSharp, and
// PostSharp does not support the unions of C# 15, so there is no behaviour to compare the selection against. Issue
// #2031 asks for the comparison once PostSharp supports them, and this file is one of the two places to update if the
// selection changes, the other being section 13 of Metalama.Framework/docs/2027.0/DECISIONS.md.

[assembly: ReportConstructor]

namespace Metalama.Extensions.Multicast.AspectTests.Union
{
    internal sealed class ReportConstructorAttribute : MulticastAspect, IAspect<IConstructor>
    {
        private static readonly DiagnosticDefinition<IDeclaration> _selected =
            new( "MY001", Severity.Warning, "The multicast aspect selected {0}." );

        public ReportConstructorAttribute() : base( MulticastTargets.InstanceConstructor ) { }

        public void BuildAspect( IAspectBuilder<IConstructor> builder )
        {
            this.Implementation.BuildAspect( builder, b => b.Diagnostics.Report( _selected.WithArguments( b.Target ) ) );
        }

        // The default implementation of the interface member is a default interface implementation, which .NET
        // Framework does not support, so the method is declared here.
        public void BuildEligibility( IEligibilityBuilder<IConstructor> builder ) { }
    }

    public record Circle( double Radius );

    public record Rectangle( double Width, double Height );

    public struct OrdinaryStruct;

    // <target>
    public union Shape( Circle, Rectangle );
}

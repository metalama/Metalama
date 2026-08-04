// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.SelectTypesDerivedFromIntroducedType;

// SelectTypesDerivedFrom( INamedType ) converts the type it is given to a durable reference, so that a query built by
// a fabric does not pin the compilation (issue #1799). A type introduced by an aspect has no symbol and therefore no
// resolvable type identifier, so that conversion has to be skipped for it, which is safe because only an aspect can
// hold an introduced type and the queries of an aspect do not outlive the run. Converting it unconditionally makes
// this test fail with a SymbolNotFoundException while the query is being executed.

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect2), typeof(Aspect1) )]

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.SelectTypesDerivedFromIntroducedType
{
    internal class Aspect1 : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var introducedType = builder.IntroduceClass( "IntroducedType" ).Declaration;

            builder.Outbound.SelectTypesDerivedFrom( introducedType ).AddAspect<Aspect2>();
        }
    }

    internal class Aspect2 : TypeAspect
    {
        [Introduce]
        public void MethodOnIntroducedType() { }
    }

    // <target>
    [Aspect1]
    internal class TargetCode { }
}

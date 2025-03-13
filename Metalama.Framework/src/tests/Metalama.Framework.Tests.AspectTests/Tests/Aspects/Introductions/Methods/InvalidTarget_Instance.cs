// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InvalidTarget_Instance
{
    /*
     * Tests cases where method is introduced as instance into a type where instance members are not allowed.
     */

    public class ImplicitInstanceIntroductionAttribute : TypeAspect
    {
        public static DiagnosticDefinition<string> ManualAssert = new( "MANUAL_ASSERT", Severity.Warning, "{0}" );

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.Diagnostics.Report( ManualAssert.WithArguments( "Manually assert that 2 error is reported on this class." ) );
            builder.IntroduceMethod( nameof(Method_ImplicitlyInstance) );
        }

        [Template]
        public int Method_ImplicitlyInstance()
        {
            return meta.Proceed();
        }
    }

    public class ExplicitInstanceIntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod( nameof(Method_ExplicitlyInstance), scope: IntroductionScope.Instance );
        }

        [Template]
        public static int Method_ExplicitlyInstance()
        {
            return meta.Proceed();
        }
    }

    // <target>
    [ImplicitInstanceIntroduction]
    [ExplicitInstanceIntroduction]
    internal static class StaticTargetClass { }
}
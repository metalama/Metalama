using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Invalid_StaticVirtual
{
    /*
     * Tests cases where method is introduced as static virtual.
     */

    public class ImplicitlyStaticExplicitlyVirtualIntroductionAttribute : TypeAspect
    {
        public static DiagnosticDefinition<string> ManualAssert = new( "MANUAL_ASSERT", Severity.Warning, "{0}" );

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.Diagnostics.Report( ManualAssert.WithArguments( "Manually assert that 3 errors are reported on this class." ) );
            builder.IntroduceMethod( nameof(Method_ImplicitlyStaticExplicitlyVirtual), buildMethod: b => b.IsVirtual = true );
        }

        [Template]
        public static int Method_ImplicitlyStaticExplicitlyVirtual()
        {
            return meta.Proceed();
        }
    }

    public class ExplicitlyStaticImplicitlyVirtualIntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod( nameof(Method_ExplicitlyStaticImplicitlyVirtual), scope: IntroductionScope.Static );
        }

        [Template]
        public virtual int Method_ExplicitlyStaticImplicitlyVirtual()
        {
            return meta.Proceed();
        }
    }

    public class ExplicitlyStaticExplicitlyVirtualIntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod(
                nameof(Method_ExplicitlyStaticExplicitlyVirtual),
                scope: IntroductionScope.Static,
                buildMethod: b => b.IsVirtual = true );
        }

        [Template]
        public int Method_ExplicitlyStaticExplicitlyVirtual()
        {
            return meta.Proceed();
        }
    }

    // <target>
    [ImplicitlyStaticExplicitlyVirtualIntroduction]
    [ExplicitlyStaticImplicitlyVirtualIntroduction]
    [ExplicitlyStaticExplicitlyVirtualIntroduction]
    internal class TargetClass { }
}
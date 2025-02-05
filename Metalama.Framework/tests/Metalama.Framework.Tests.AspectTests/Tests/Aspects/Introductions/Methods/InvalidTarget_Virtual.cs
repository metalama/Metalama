using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InvalidTarget_Virtual
{
    /*
     * Tests cases where method is introduced as virtual into a type where virtual members are not allowed.
     */

    public class ImplicitlyVirtualIntroductionAttribute : TypeAspect
    {
        public static DiagnosticDefinition<string> ManualAssert = new( "MANUAL_ASSERT", Severity.Warning, "{0}" );

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.Diagnostics.Report( ManualAssert.WithArguments( "Manually assert that 3 errors are reported on this class." ) );
            builder.IntroduceMethod( nameof(Method_ImplicitlyVirtual) );
        }

        [Template]
        public virtual int Method_ImplicitlyVirtual()
        {
            return meta.Proceed();
        }
    }

    public class ExplicitlyVirtualIntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod( nameof(Method_ExplicitlyVirtual), buildMethod: b => b.IsVirtual = true );
        }

        [Template]
        public int Method_ExplicitlyVirtual()
        {
            return meta.Proceed();
        }
    }

    // <target>
    [ImplicitlyVirtualIntroduction]
    [ExplicitlyVirtualIntroduction]
    internal struct TargetStruct { }

    // <target>
    [ImplicitlyVirtualIntroduction]
    [ExplicitlyVirtualIntroduction]
    internal sealed class SealedTargetClass { }

    // <target>
    [ImplicitlyVirtualIntroduction]
    [ExplicitlyVirtualIntroduction]
    internal static class StaticTargetClass { }
}
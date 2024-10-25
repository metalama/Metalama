// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Aspect that, when added to a parameterless <c>void</c> method, causes this method to be invoked after each property setter
/// or method (but not property getters), except those annotated with <see cref="DoNotCheckInvariantsAttribute"/>. The target method
/// must check any relevant condition and throw throw <see cref="InvariantViolationException"/> in case of violation.
/// </summary>
/// <remarks>
/// <para>The aspect defines a protected method <c>VerifyInvariants</c> that calls all invariant methods defined in the object including
/// in its base classes.</para>
/// <para>
///  If the <see cref="ContractOptions.IsInvariantSuspensionSupported"/> contract option is defined, the aspect also introduces
/// the protected methods <c>SuspendInvariants</c>, which allows to suspend the verification of invariants,
/// and  <c>AreInvariantsSuspended</c>, which determines if the verification of invariants is currently suspended.
/// </para>
/// </remarks>
/// <seealso href="@invariants"/>
public sealed class InvariantAttribute : MethodAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        builder.MustNotBeStatic();
        builder.ReturnType().MustEqual( SpecialType.Void );
        builder.MustSatisfy( m => m.Parameters.Count == 0, o => $"{o} must not have any parameters" );
    }

    [Introduce( Accessibility = Accessibility.Protected, IsVirtual = true, WhenExists = OverrideStrategy.Override )]
#pragma warning disable CA1822
    internal void VerifyInvariants()
#pragma warning restore CA1822
    {
        meta.Proceed();

        var invariantMethod = (IMethod) meta.AspectInstance.TargetDeclaration.GetTarget( meta.Target.Compilation );

        invariantMethod.Invoke();
    }

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        if ( builder.Target.GetContractOptions().AreInvariantsEnabled == false )
        {
            builder.SkipAspect();

            return;
        }

        builder.Outbound.Select( x => x.DeclaringType ).RequireAspect<CheckInvariantsAspect>();
    }
}
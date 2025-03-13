// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Aspect that, when applied to a method or accessor, suspends the execution of invariants for the object while this method is
/// executing. All threads and execution contexts are affected. The feature must be enabled through the <see cref="ContractOptions.IsInvariantSuspensionSupported"/>
/// contract option.
/// </summary>
/// <seealso href="@invariants"/>
public sealed class SuspendInvariantsAttribute : OverrideMethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var contractOptions = builder.Target.DeclaringType.GetContractOptions();

        // Skip the aspect if invariants are not enabled.
        if ( contractOptions.AreInvariantsEnabled == false )
        {
            builder.SkipAspect();

            return;
        }

        // Report an error if invariant suspension is not supported for this type.
        var enableInvariantSuspensionSupport = contractOptions.IsInvariantSuspensionSupported == true;

        if ( !enableInvariantSuspensionSupport )
        {
            builder.Diagnostics.Report( ContractDiagnostics.SuspensionNotSupported.WithArguments( (builder.Target, builder.Target.DeclaringType) ) );

            return;
        }

        // Report a warning and skip if there is no invariant on this type.
        if ( !builder.Target.DeclaringType.Enhancements().HasAspect<CheckInvariantsAspect>() )
        {
            builder.Diagnostics.Report( ContractDiagnostics.SuspensionRedundant.WithArguments( (builder.Target, builder.Target.DeclaringType) ) );

            builder.SkipAspect();

            return;
        }

        base.BuildAspect( builder );
    }

    public override dynamic? OverrideMethod()
    {
        using ( meta.This.SuspendInvariants() )
        {
            return meta.Proceed();
        }
    }
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Implementation aspect for <see cref="InvariantAttribute"/>.
/// </summary>
[Inheritable]
internal sealed class CheckInvariantsAspect : IAspect<INamedType>
{
    public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var contractOptions = builder.Target.GetContractOptions();

        var enableInvariantSuspensionSupport = contractOptions.IsInvariantSuspensionSupported == true;

        // Override methods.
        var setters = builder.Target.Properties.Where( x => x.SetMethod != null )
            .Select( x => x.SetMethod! )
            .Concat( builder.Target.Indexers.Where( x => x.SetMethod != null ).Select( x => x.SetMethod! ) );

        var methods = builder.Target.Methods
            .Where(
                m => m is { IsReadOnly: false, Accessibility: Accessibility.Internal or Accessibility.ProtectedInternal or Accessibility.Public } &&
                     m.Name != nameof(InvariantAttribute.VerifyInvariants) && !m.Enhancements().HasAspect<InvariantAttribute>() )
            .Concat( setters );

        var skipInvariantsAttributeType = (INamedType) TypeFactory.GetType( typeof(DoNotCheckInvariantsAttribute) );

        var methodsToOverride = methods.Where(
            m => !m.Attributes.OfAttributeType( skipInvariantsAttributeType ).Any()
                 && m.IsAdviceEligible( AdviceKind.OverrideMethod ) );

        foreach ( var method in methodsToOverride )
        {
            builder.Advice.Override( method, nameof(OverrideMethod), args: new { enableInvariantSuspensionSupport } );
        }

        // Add support for dynamic suspension of invariants.

        if ( enableInvariantSuspensionSupport )
        {
            if ( !builder.Target.AllMethods.OfName( nameof(this.SuspendInvariants) ).Any() )
            {
                var counterField = builder.Advice.IntroduceField( builder.Target, nameof(this._invariantSuspensionCounter) ).Declaration;
                builder.Advice.IntroduceMethod( builder.Target, nameof(this.SuspendInvariants), args: new { counterField } );
                builder.Advice.IntroduceMethod( builder.Target, nameof(this.AreInvariantsSuspended), args: new { counterField } );
            }
        }
    }

    [Template]
    private readonly InvariantSuspensionCounter _invariantSuspensionCounter = new();

    [Template]
    protected SuspendInvariantsCookie SuspendInvariants( IField counterField )
    {
        ((InvariantSuspensionCounter) counterField.Value!).Increment();

        return new SuspendInvariantsCookie( (InvariantSuspensionCounter) counterField.Value! );
    }

    [Template]
    protected bool AreInvariantsSuspended( IField counterField )
    {
        return ((InvariantSuspensionCounter) counterField.Value!).AreInvariantsSuspended;
    }

    [Template]
    private static dynamic? OverrideMethod( [CompileTime] bool enableInvariantSuspensionSupport )
    {
        try
        {
            return meta.Proceed();
        }
        finally
        {
            if ( enableInvariantSuspensionSupport )
            {
                if ( !meta.This.AreInvariantsSuspended() )
                {
                    meta.This.VerifyInvariants();
                }
            }
            else
            {
                meta.This.VerifyInvariants();
            }
        }
    }
}
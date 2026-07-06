// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Issue1722.Primitives;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Issue1722.Aspects;

// The aspect lives in a DIFFERENT package than AspectUtilities and pulls it in as a (transitive) reference.
// It calls the cross-package extension method from eligibility code, which is exactly the scenario reported in
// issue #1722. (The failure is NOT specific to eligibility: calling AspectUtilities from the OverrideMethod
// template fails identically, reported as LAMA0041 "while executing the template" instead of LAMA0001 "while
// evaluating eligibility" - eligibility simply runs first in the pipeline.)
public sealed class SetContextAttribute : OverrideMethodAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            method => this.ProduceValidationFailureMessage( $"The method '{method}'", method ) == null,
            method => $"the return type must be a result object, or a task result object" );
    }

    private FormattableString? ProduceValidationFailureMessage( FormattableString description, IMethod method )
    {
        // Extension-method syntax on a type defined in the referenced Issue1722.Primitives package.
        // This is the call that throws FileNotFoundException for 'ml!Issue1722.Primitives...'.
        if ( !method.ReturnType.IsResultTask() )
        {
            return $"{description} must return a result object, or a task result object";
        }

        return null;
    }

    public override dynamic? OverrideMethod() => meta.Proceed();
}

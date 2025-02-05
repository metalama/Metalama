// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using System;

namespace Metalama.Framework.Engine.Pipeline;

internal sealed class EligibilityService : IEligibilityService
{
    private readonly AspectClassCollection _aspectClasses;

    public EligibilityService( AspectClassCollection aspectClasses )
    {
        this._aspectClasses = aspectClasses;
    }

    public bool IsEligible( Type aspectType, IDeclaration declaration, EligibleScenarios scenarios )
    {
        if ( !this._aspectClasses.Dictionary.TryGetValue( aspectType.FullName!, out var aspectClass ) )
        {
            throw new ArgumentOutOfRangeException( nameof(aspectType), $"The project does not contain an aspect of type '{aspectType.FullName}'." );
        }

        var eligibleScenarios = aspectClass.GetEligibility( declaration );

        return eligibleScenarios.IncludesAny( scenarios );
    }
}
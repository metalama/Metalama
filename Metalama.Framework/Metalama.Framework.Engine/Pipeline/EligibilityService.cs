// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
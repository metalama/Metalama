// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Eligibility;

internal interface IEligibilityService : IProjectService
{
    bool IsEligible( Type aspectType, IDeclaration declaration, EligibleScenarios scenarios );
}
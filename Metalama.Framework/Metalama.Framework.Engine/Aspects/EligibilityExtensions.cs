// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Engine.Aspects
{
    [PublicAPI]
    public static class EligibilityExtensions
    {
        public static bool IncludesAll( this EligibleScenarios scenarios, EligibleScenarios subset ) => (scenarios & subset) == subset;

        public static bool IncludesAny( this EligibleScenarios scenarios, EligibleScenarios subset ) => (scenarios & subset) != 0;
    }
}
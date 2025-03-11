// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
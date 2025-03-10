// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.IntegrationTests.Aspects.Applying.InvalidTarget
{
    // Intentionally not using TypeAspect so we have no AttributeUsage.
    public class IntroductionAttribute : Attribute, IAspect<INamedType>
    {
        public void BuildAspect( IAspectBuilder<INamedType> builder ) { }

        public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }
    }

    // <target>
    internal class TargetClass
    {
        [Introduction]
        private void Method() { }
    }
}
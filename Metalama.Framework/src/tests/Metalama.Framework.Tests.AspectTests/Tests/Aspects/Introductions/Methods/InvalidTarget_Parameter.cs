// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InvalidTarget_Parameter
{
    /*
     * Tests that an aspect with a declarative introduction cannot be applied to a parameter.
     * The eligibility rule of IntroduceAttribute reports LAMA0037 before the aspect is built.
     */

    public class IntroductionAttribute : ParameterAspect
    {
        [Introduce]
        public void IntroducedMethod() { }
    }

    // <target>
    internal class TargetClass
    {
        public void Method( [Introduction] int p ) { }
    }
}

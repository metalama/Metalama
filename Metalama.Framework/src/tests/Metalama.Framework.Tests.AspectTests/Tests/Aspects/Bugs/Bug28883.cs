// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Invokers;

// This checks that throw expressions in expression bodies work properly.
// Part of the fix was that the transformed run-time code for the aspect was incorrect, so we also cover it with tests.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28883
{
    public class TestAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                return meta.Target.FieldOrProperty.With( InvokerOptions.Final ).Value;
            }

            set
            {
                meta.Target.FieldOrProperty.With( InvokerOptions.Final ).Value = value;
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        [TestAttribute]
        private int Property { get; set; }
    }
}
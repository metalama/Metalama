// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif
#pragma warning disable CS0067, CS8602, CS8603, CS0169
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Diff.WarningLocation
{
    internal class IntroduceCloneAttribute : TypeAspect
    {
        [Introduce]
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    // <target>
    [IntroduceClone]
    internal partial class FirstClass
    {
        private int a;

        private string? b;
    }

    [IntroduceClone]
    internal partial class SecondClass
    {
        // CS8618 should be reported on this class because of this non-nullable field.
        private string nonNullableField;
    }
}

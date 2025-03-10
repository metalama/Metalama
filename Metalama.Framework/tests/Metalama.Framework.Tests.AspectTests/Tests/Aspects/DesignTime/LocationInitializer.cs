// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.DesignTime.LocationInitializer
{
    internal class IdAttribute : TypeAspect
    {
        // Initializers should NOT make it into the partial class.

        [Introduce]
        public Guid Property { get; } = Guid.NewGuid();

        [Introduce]
        public Guid Field = Guid.NewGuid();
    }

    // <target>
    [Id]
    internal partial class TargetCode { }
}
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
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.FileNameConflict
{
    // Tests that the pipeline handles types with the same full name.

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void Foo() { }
    }

    namespace X
    {
        [Introduction]
        partial class Y
        {
        }

        [Introduction]
        partial class Y<T>
        {
        }
    }

    partial class X<T>
    {
        [Introduction]
        partial class Y
        {
        }

        [Introduction]
        partial class Y<U>
        {
        }
    }
}
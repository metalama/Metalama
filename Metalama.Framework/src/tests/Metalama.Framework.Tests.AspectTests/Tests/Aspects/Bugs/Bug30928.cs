// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @KeepDisabledCode
#endif

using System.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30928
{
    public interface ISomeInterface
    {
    }

    // <target>
    public class SomeDisposable : ISomeInterface
    {   
#if TESTRUNNER
        void Foo()
        {
            Debug.Fail("");
        }
#endif

#if !TESTRUNNER
        void Bar()
        {
            Debug.Fail("");
        }
#endif
    }
}

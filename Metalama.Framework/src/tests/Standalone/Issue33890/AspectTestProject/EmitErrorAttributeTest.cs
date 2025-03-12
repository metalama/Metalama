// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using AspectLibraryProject;

namespace AspectTestProject.EmitErrorAttributeTests
{
    internal class EmitErrorAttributeTest
    {
        // The error should be emitted when running the test, but not when building the test suite.
        [EmitError]
        public static void MyMethod()
        {
            Console.WriteLine("Hello, world");
        }
    }
}
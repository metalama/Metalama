// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
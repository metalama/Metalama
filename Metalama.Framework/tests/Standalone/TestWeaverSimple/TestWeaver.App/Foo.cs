// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using Metalama.Open.Virtuosity;

#pragma warning disable CA1822 // Mark members as static

namespace TestWeaver.App
{
    [Virtualize]
    internal class Foo
    {
        public void Bar() { }
    }
}

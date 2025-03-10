// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.IfDirective
{
    public class SomeClass
    {
        void TestMain()
        {
#if METALAMA
            Console.WriteLine("This should be included.");
#else
            Console.WriteLine( "This should NOT be included." );
#endif

#if METALAMA
            Console.WriteLine("This should be included.");
#elif NOTHING
            Console.WriteLine( "This should NOT be included." );
#endif
            
        }
        
    }
}
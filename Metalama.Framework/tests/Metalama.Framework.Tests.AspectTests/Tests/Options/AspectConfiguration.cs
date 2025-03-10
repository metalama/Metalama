// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Doc.AspectConfiguration
{
    // Some target code.
    public class SomeClass
    {
        [Log]
        public void SomeMethod() { }
    }

    namespace ChildNamespace
    {
        public class SomeOtherClass
        {
            [Log]
            public void SomeMethod() { }
        }
    }
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.FieldAndPropertyInitializers
{
    internal class Aspect : TypeAspect
    {
        [Introduce]
        public string IntroducedField = meta.Target.Type.Name;

        [Introduce]
        public string IntroducedProperty { get; set; } = meta.Target.Type.Name;
    }

    [CompileTime]
    internal class CompileTimeClass
    {
        public int Field = 42;

        public string Property { get; set; } = "hello";

        public List<int> ListField = new List<int>();
    }
}

// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Patterns.Contracts.AspectTests;

// <target>
public class TestClass
{
    public TestClass( [NotNull] [NotEmpty] IReadOnlyCollection<int> list ) { }

    [return: NotNull]
    [return: NotEmpty]
    public IReadOnlyCollection<int> Foo( [NotNull] [NotEmpty] IReadOnlyCollection<int> list )
    {
        return list;
    }

    [NotNull]
    [NotEmpty]
    public IReadOnlyCollection<int> Property { get; set; }
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
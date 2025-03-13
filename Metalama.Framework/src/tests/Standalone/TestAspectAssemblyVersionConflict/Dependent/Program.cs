// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Middle1;
using Middle2;

namespace Dependent;

internal class Program
{
    static void Main(string[] args)
    {
        var test1 = new Test1();
        var test2 = new Test2();
    }
}

public class Test1 : BaseClass1
{
}

public class Test2 : BaseClass2
{
}
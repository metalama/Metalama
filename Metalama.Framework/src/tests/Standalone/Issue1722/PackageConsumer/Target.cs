// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
using System.Threading.Tasks;
using Issue1722.Aspects;

namespace Issue1722.PackageConsumer;

internal class Target
{
    [SetContext]
    private static Task<int> DoTheThing3(int a, int b) => Task.FromResult(a + b);
}

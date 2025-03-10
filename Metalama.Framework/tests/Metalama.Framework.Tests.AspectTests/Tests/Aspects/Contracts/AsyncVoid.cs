// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.AsyncVoid;

public sealed class NotNullAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException();
        }
    }
}

// <target>
public class Class1
{
    public Task Execute_Task( [NotNull] Action action ) => Task.CompletedTask;

    public ValueTask Execute_ValueTask( [NotNull] Action action ) => new( Task.CompletedTask );

    public async Task ExecuteAsync_Task( [NotNull] Action action ) => await Task.Yield();

    public async ValueTask ExecuteAsync_ValueTask( [NotNull] Action action ) => await Task.Yield();

    public async void ExecuteAsync_Void( [NotNull] Action action ) => await Task.Yield();
}
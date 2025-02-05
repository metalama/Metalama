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
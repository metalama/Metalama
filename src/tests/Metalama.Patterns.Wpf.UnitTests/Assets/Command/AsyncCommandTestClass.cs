// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.UnitTests.Assets.Command;

public partial class AsyncCommandTestClass
{
    public AsyncBarrier Barrier { get; } = new( 2 );

    [Command]
    private async Task CancellableAsync( CancellationToken cancellationToken )
    {
        await this.Barrier.SignalAndWait();

        cancellationToken.ThrowIfCancellationRequested();
    }

    [Command( CanExecuteProperty = nameof(CanExecuteCancellableWithCanExecute) )]
    private async Task CancellableWithCanExecuteAsync( CancellationToken cancellationToken )
    {
        await this.Barrier.SignalAndWait();

        cancellationToken.ThrowIfCancellationRequested();
    }

    [Command]
    private async Task NonCancellableAsync()
    {
        await this.Barrier.SignalAndWait();
    }

    [Command( CanExecuteProperty = nameof(CanExecuteCancellableWithCanExecute) )]
    private async Task NonCancellableWithCanExecuteAsync()
    {
        await this.Barrier.SignalAndWait();
    }

    public bool CanExecuteCancellableWithCanExecute { get; set; }
}
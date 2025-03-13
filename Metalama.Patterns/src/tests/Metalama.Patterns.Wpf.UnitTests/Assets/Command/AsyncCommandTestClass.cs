// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
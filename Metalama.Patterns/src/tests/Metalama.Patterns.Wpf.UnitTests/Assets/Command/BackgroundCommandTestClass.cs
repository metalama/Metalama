// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Wpf.UnitTests.Assets.Command;

public partial class BackgroundCommandTestClass
{
    public AsyncBarrier Barrier { get; } = new( 2 );

    [Command( Background = true )]
    private void Cancellable( CancellationToken cancellationToken )
    {
        this.Barrier.SignalAndWait().Wait( cancellationToken );

        cancellationToken.ThrowIfCancellationRequested();
    }

    [Command( CanExecuteProperty = nameof(CanExecuteCancellableWithCanExecute), Background = true )]
    private void CancellableWithCanExecute( CancellationToken cancellationToken )
    {
        this.Barrier.SignalAndWait().Wait( cancellationToken );

        cancellationToken.ThrowIfCancellationRequested();
    }

    [Command( Background = true )]
    private void NonCancellable()
    {
        this.Barrier.SignalAndWait().Wait();
    }

    [Command( CanExecuteProperty = nameof(CanExecuteCancellableWithCanExecute), Background = true )]
    private void NonCancellableWithCanExecute()
    {
        this.Barrier.SignalAndWait().Wait();
    }

    public bool CanExecuteCancellableWithCanExecute { get; set; }
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Wpf.UnitTests.Assets.Command;
using Xunit;

namespace Metalama.Patterns.Wpf.UnitTests.Command;

public class AsyncCommandTests
{
    public AsyncCommandTestClass Instance { get; } = new();

    [Fact]
    public async Task ExecuteCancellableAndCompleteAsync()
    {
        Assert.True( this.Instance.CancellableCommand.CanExecute );
        Assert.False( this.Instance.CancellableCommand.CanCancel );
        Assert.False( this.Instance.CancellableCommand.IsRunning );
        Assert.False( this.Instance.CancellableCommand.IsCancellationRequested );
        Assert.Null( this.Instance.CancellableCommand.ExecutionTask );

        this.Instance.CancellableCommand.Execute();

        Assert.False( this.Instance.CancellableCommand.CanExecute );
        Assert.True( this.Instance.CancellableCommand.CanCancel );
        Assert.True( this.Instance.CancellableCommand.IsRunning );
        Assert.False( this.Instance.CancellableCommand.IsCancellationRequested );
        Assert.NotNull( this.Instance.CancellableCommand.ExecutionTask );

        await this.Instance.Barrier.SignalAndWait();

        await this.Instance.CancellableCommand.ExecutionTask;

        Assert.True( this.Instance.CancellableCommand.CanExecute );
        Assert.False( this.Instance.CancellableCommand.CanCancel );
        Assert.False( this.Instance.CancellableCommand.IsRunning );
        Assert.False( this.Instance.CancellableCommand.IsCancellationRequested );
        Assert.NotNull( this.Instance.CancellableCommand.ExecutionTask );
    }

    [Fact]
    public async Task ExecuteNonCancellableAndCompleteAsync()
    {
        Assert.True( this.Instance.NonCancellableCommand.CanExecute );
        Assert.False( this.Instance.NonCancellableCommand.CanCancel );
        Assert.False( this.Instance.NonCancellableCommand.IsRunning );
        Assert.False( this.Instance.NonCancellableCommand.IsCancellationRequested );
        Assert.Null( this.Instance.NonCancellableCommand.ExecutionTask );

        this.Instance.NonCancellableCommand.Execute();

        Assert.False( this.Instance.NonCancellableCommand.CanExecute );
        Assert.False( this.Instance.NonCancellableCommand.CanCancel );
        Assert.True( this.Instance.NonCancellableCommand.IsRunning );
        Assert.False( this.Instance.NonCancellableCommand.IsCancellationRequested );
        Assert.NotNull( this.Instance.NonCancellableCommand.ExecutionTask );

        await this.Instance.Barrier.SignalAndWait();

        await this.Instance.NonCancellableCommand.ExecutionTask;

        Assert.True( this.Instance.NonCancellableCommand.CanExecute );
        Assert.False( this.Instance.NonCancellableCommand.CanCancel );
        Assert.False( this.Instance.NonCancellableCommand.IsRunning );
        Assert.False( this.Instance.NonCancellableCommand.IsCancellationRequested );
        Assert.NotNull( this.Instance.NonCancellableCommand.ExecutionTask );
    }

    [Fact]
    public async Task ExecuteAndCancelAsync()
    {
        Assert.True( this.Instance.CancellableCommand.CanExecute );
        Assert.False( this.Instance.CancellableCommand.CanCancel );
        Assert.False( this.Instance.CancellableCommand.IsRunning );
        Assert.False( this.Instance.CancellableCommand.IsCancellationRequested );
        Assert.Null( this.Instance.CancellableCommand.ExecutionTask );

        this.Instance.CancellableCommand.Execute();

        Assert.False( this.Instance.CancellableCommand.CanExecute );
        Assert.True( this.Instance.CancellableCommand.CanCancel );
        Assert.True( this.Instance.CancellableCommand.IsRunning );
        Assert.False( this.Instance.CancellableCommand.IsCancellationRequested );
        Assert.NotNull( this.Instance.CancellableCommand.ExecutionTask );

        this.Instance.CancellableCommand.Cancel();

        this.Instance.CancellableCommand.Cancel();

        Assert.False( this.Instance.CancellableCommand.CanExecute );
        Assert.True( this.Instance.CancellableCommand.CanCancel );
        Assert.True( this.Instance.CancellableCommand.IsRunning );
        Assert.True( this.Instance.CancellableCommand.IsCancellationRequested );

        await this.Instance.Barrier.SignalAndWait();

        await Assert.ThrowsAsync<OperationCanceledException>( () => this.Instance.CancellableCommand.ExecutionTask );

        Assert.True( this.Instance.CancellableCommand.CanExecute );
        Assert.False( this.Instance.CancellableCommand.CanCancel );
        Assert.False( this.Instance.CancellableCommand.IsRunning );
        Assert.False( this.Instance.CancellableCommand.IsCancellationRequested );
        Assert.NotNull( this.Instance.CancellableCommand.ExecutionTask );
    }

    [Fact]
    public async Task CannotExecuteAsync()
    {
        Assert.False( this.Instance.CancellableWithCanExecuteCommand.CanExecute );
        this.Instance.CanExecuteCancellableWithCanExecute = true;
        Assert.True( this.Instance.CancellableWithCanExecuteCommand.CanExecute );

        Assert.False( this.Instance.CancellableWithCanExecuteCommand.CanCancel );
        Assert.False( this.Instance.CancellableWithCanExecuteCommand.IsRunning );
        Assert.False( this.Instance.CancellableWithCanExecuteCommand.IsCancellationRequested );
        Assert.Null( this.Instance.CancellableWithCanExecuteCommand.ExecutionTask );

        this.Instance.CancellableWithCanExecuteCommand.Execute();

        Assert.False( this.Instance.CancellableWithCanExecuteCommand.CanExecute );
        Assert.True( this.Instance.CancellableWithCanExecuteCommand.CanCancel );
        Assert.True( this.Instance.CancellableWithCanExecuteCommand.IsRunning );
        Assert.False( this.Instance.CancellableWithCanExecuteCommand.IsCancellationRequested );
        Assert.NotNull( this.Instance.CancellableWithCanExecuteCommand.ExecutionTask );

        this.Instance.CancellableWithCanExecuteCommand.Cancel();

        this.Instance.CancellableWithCanExecuteCommand.Cancel();

        Assert.False( this.Instance.CancellableWithCanExecuteCommand.CanExecute );
        Assert.True( this.Instance.CancellableWithCanExecuteCommand.CanCancel );
        Assert.True( this.Instance.CancellableWithCanExecuteCommand.IsRunning );
        Assert.True( this.Instance.CancellableWithCanExecuteCommand.IsCancellationRequested );

        await this.Instance.Barrier.SignalAndWait();

        await Assert.ThrowsAsync<OperationCanceledException>( () => this.Instance.CancellableWithCanExecuteCommand.ExecutionTask );

        Assert.True( this.Instance.CancellableWithCanExecuteCommand.CanExecute );
        Assert.False( this.Instance.CancellableWithCanExecuteCommand.CanCancel );
        Assert.False( this.Instance.CancellableWithCanExecuteCommand.IsRunning );
        Assert.False( this.Instance.CancellableWithCanExecuteCommand.IsCancellationRequested );
        Assert.NotNull( this.Instance.CancellableWithCanExecuteCommand.ExecutionTask );
    }

    [Fact]
    public async Task ExecutedEventAsync()
    {
        var executedTokens = new List<DelegateCommandExecution>();
        this.Instance.CanExecuteCancellableWithCanExecute = true;
        this.Instance.CancellableWithCanExecuteCommand.Executed += token => executedTokens.Add( token );
        this.Instance.CancellableWithCanExecuteCommand.Execute();
        Assert.NotEmpty( executedTokens );
    }
}
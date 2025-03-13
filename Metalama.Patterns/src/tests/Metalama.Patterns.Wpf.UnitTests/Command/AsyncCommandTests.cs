// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Wpf.UnitTests.Assets.Command;
using Xunit;

namespace Metalama.Patterns.Wpf.UnitTests.Command;

public sealed class AsyncCommandTests
{
    private readonly AsyncCommandTestClass _instance = new();

    [Fact]
    public async Task ExecuteCancellableAndCompleteAsync()
    {
        Assert.True( this._instance.CancellableCommand.CanExecute );
        Assert.False( this._instance.CancellableCommand.CanCancel );
        Assert.False( this._instance.CancellableCommand.IsRunning );
        Assert.False( this._instance.CancellableCommand.IsCancellationRequested );
        Assert.Null( this._instance.CancellableCommand.ExecutionTask );

        this._instance.CancellableCommand.Execute();

        Assert.False( this._instance.CancellableCommand.CanExecute );
        Assert.True( this._instance.CancellableCommand.CanCancel );
        Assert.True( this._instance.CancellableCommand.IsRunning );
        Assert.False( this._instance.CancellableCommand.IsCancellationRequested );
        Assert.NotNull( this._instance.CancellableCommand.ExecutionTask );

        await this._instance.Barrier.SignalAndWait();

        await this._instance.CancellableCommand.ExecutionTask;

        Assert.True( this._instance.CancellableCommand.CanExecute );
        Assert.False( this._instance.CancellableCommand.CanCancel );
        Assert.False( this._instance.CancellableCommand.IsRunning );
        Assert.False( this._instance.CancellableCommand.IsCancellationRequested );
        Assert.NotNull( this._instance.CancellableCommand.ExecutionTask );
    }

    [Fact]
    public async Task ExecuteNonCancellableAndCompleteAsync()
    {
        Assert.True( this._instance.NonCancellableCommand.CanExecute );
        Assert.False( this._instance.NonCancellableCommand.CanCancel );
        Assert.False( this._instance.NonCancellableCommand.IsRunning );
        Assert.False( this._instance.NonCancellableCommand.IsCancellationRequested );
        Assert.Null( this._instance.NonCancellableCommand.ExecutionTask );

        this._instance.NonCancellableCommand.Execute();

        Assert.False( this._instance.NonCancellableCommand.CanExecute );
        Assert.False( this._instance.NonCancellableCommand.CanCancel );
        Assert.True( this._instance.NonCancellableCommand.IsRunning );
        Assert.False( this._instance.NonCancellableCommand.IsCancellationRequested );
        Assert.NotNull( this._instance.NonCancellableCommand.ExecutionTask );

        await this._instance.Barrier.SignalAndWait();

        await this._instance.NonCancellableCommand.ExecutionTask;

        Assert.True( this._instance.NonCancellableCommand.CanExecute );
        Assert.False( this._instance.NonCancellableCommand.CanCancel );
        Assert.False( this._instance.NonCancellableCommand.IsRunning );
        Assert.False( this._instance.NonCancellableCommand.IsCancellationRequested );
        Assert.NotNull( this._instance.NonCancellableCommand.ExecutionTask );
    }

    [Fact]
    public async Task ExecuteAndCancelAsync()
    {
        Assert.True( this._instance.CancellableCommand.CanExecute );
        Assert.False( this._instance.CancellableCommand.CanCancel );
        Assert.False( this._instance.CancellableCommand.IsRunning );
        Assert.False( this._instance.CancellableCommand.IsCancellationRequested );
        Assert.Null( this._instance.CancellableCommand.ExecutionTask );

        this._instance.CancellableCommand.Execute();

        Assert.False( this._instance.CancellableCommand.CanExecute );
        Assert.True( this._instance.CancellableCommand.CanCancel );
        Assert.True( this._instance.CancellableCommand.IsRunning );
        Assert.False( this._instance.CancellableCommand.IsCancellationRequested );
        Assert.NotNull( this._instance.CancellableCommand.ExecutionTask );

        this._instance.CancellableCommand.Cancel();

        this._instance.CancellableCommand.Cancel();

        Assert.False( this._instance.CancellableCommand.CanExecute );
        Assert.True( this._instance.CancellableCommand.CanCancel );
        Assert.True( this._instance.CancellableCommand.IsRunning );
        Assert.True( this._instance.CancellableCommand.IsCancellationRequested );

        await this._instance.Barrier.SignalAndWait();

        await Assert.ThrowsAsync<OperationCanceledException>( () => this._instance.CancellableCommand.ExecutionTask );

        Assert.True( this._instance.CancellableCommand.CanExecute );
        Assert.False( this._instance.CancellableCommand.CanCancel );
        Assert.False( this._instance.CancellableCommand.IsRunning );
        Assert.False( this._instance.CancellableCommand.IsCancellationRequested );
        Assert.NotNull( this._instance.CancellableCommand.ExecutionTask );
    }

    [Fact]
    public async Task CannotExecuteAsync()
    {
        Assert.False( this._instance.CancellableWithCanExecuteCommand.CanExecute );
        this._instance.CanExecuteCancellableWithCanExecute = true;
        Assert.True( this._instance.CancellableWithCanExecuteCommand.CanExecute );

        Assert.False( this._instance.CancellableWithCanExecuteCommand.CanCancel );
        Assert.False( this._instance.CancellableWithCanExecuteCommand.IsRunning );
        Assert.False( this._instance.CancellableWithCanExecuteCommand.IsCancellationRequested );
        Assert.Null( this._instance.CancellableWithCanExecuteCommand.ExecutionTask );

        this._instance.CancellableWithCanExecuteCommand.Execute();

        Assert.False( this._instance.CancellableWithCanExecuteCommand.CanExecute );
        Assert.True( this._instance.CancellableWithCanExecuteCommand.CanCancel );
        Assert.True( this._instance.CancellableWithCanExecuteCommand.IsRunning );
        Assert.False( this._instance.CancellableWithCanExecuteCommand.IsCancellationRequested );
        Assert.NotNull( this._instance.CancellableWithCanExecuteCommand.ExecutionTask );

        this._instance.CancellableWithCanExecuteCommand.Cancel();

        this._instance.CancellableWithCanExecuteCommand.Cancel();

        Assert.False( this._instance.CancellableWithCanExecuteCommand.CanExecute );
        Assert.True( this._instance.CancellableWithCanExecuteCommand.CanCancel );
        Assert.True( this._instance.CancellableWithCanExecuteCommand.IsRunning );
        Assert.True( this._instance.CancellableWithCanExecuteCommand.IsCancellationRequested );

        await this._instance.Barrier.SignalAndWait();

        await Assert.ThrowsAsync<OperationCanceledException>( () => this._instance.CancellableWithCanExecuteCommand.ExecutionTask );

        Assert.True( this._instance.CancellableWithCanExecuteCommand.CanExecute );
        Assert.False( this._instance.CancellableWithCanExecuteCommand.CanCancel );
        Assert.False( this._instance.CancellableWithCanExecuteCommand.IsRunning );
        Assert.False( this._instance.CancellableWithCanExecuteCommand.IsCancellationRequested );
        Assert.NotNull( this._instance.CancellableWithCanExecuteCommand.ExecutionTask );
    }

    [Fact]
    public async Task ExecutedEventAsync()
    {
        var executedTokens = new List<DelegateCommandExecution>();
        this._instance.CanExecuteCancellableWithCanExecute = true;
        this._instance.CancellableWithCanExecuteCommand.Executed += token => executedTokens.Add( token );
        this._instance.CancellableWithCanExecuteCommand.Execute();
        Assert.NotEmpty( executedTokens );
    }
}
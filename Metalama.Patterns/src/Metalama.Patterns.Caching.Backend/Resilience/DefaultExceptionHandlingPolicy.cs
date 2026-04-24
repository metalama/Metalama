// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace;
using Flashtrace.Messages;
using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// The default implementation of <see cref="IExceptionHandlingPolicy"/>. Logs and swallows all exceptions and try to recover from failed write operations
/// by invalidating the cache in the background.
/// </summary>
[PublicAPI]
public class DefaultExceptionHandlingPolicy : IExceptionHandlingPolicy
{
    /// <inheritdoc />
    public virtual ValueTask<RecoveryAction> OnExceptionAsync( ExceptionInfo exceptionInfo, CancellationToken cancellationToken )
    {
        var recoveryAction = exceptionInfo.OperationKind switch
        {
            OperationKind.CleanUp => RecoveryAction.Rethrow,
            OperationKind.Collect => RecoveryAction.Rethrow,
            OperationKind.ContainsDependency => RecoveryAction.Swallow,
            OperationKind.GetItem => RecoveryAction.Swallow,
            OperationKind.InvalidateDependency => RecoveryAction.InvalidateDependencyInBackground,
            OperationKind.SetItem => RecoveryAction.RemoveItemInBackground,
            OperationKind.RemoveItem => RecoveryAction.RemoveItemInBackground,
            _ => RecoveryAction.Swallow
        };

        var logLevel = recoveryAction switch
        {
            RecoveryAction.Rethrow => FlashtraceLevel.Error,
            RecoveryAction.InvalidateDependencyInBackground => FlashtraceLevel.Warning,
            RecoveryAction.RemoveItemInBackground => FlashtraceLevel.Warning,
            RecoveryAction.Swallow => FlashtraceLevel.Warning,

            // ReSharper disable once UnreachableSwitchArmDueToIntegerAnalysis
            _ => FlashtraceLevel.Error
        };

        var logSource = logLevel == FlashtraceLevel.Error ? exceptionInfo.Logger.Error : exceptionInfo.Logger.Warning;

        logSource.Write(
            FormattedMessageBuilder.Formatted(
                "An exception was thrown for operation {Kind} with key '{Key}'. Recovery action: {Recovery}.",
                exceptionInfo.OperationKind,
                exceptionInfo.Key,
                recoveryAction.ToDisplayString() ),
            exceptionInfo.Exception );

        return new ValueTask<RecoveryAction>( recoveryAction );
    }
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;

internal interface ICompileTimeCodeEditingStatusRpcApi : IRpcApi
{
    // TODO: Delete this method? 
    /// <summary>
    /// Notifies the analysis process that the user process is now ready to process notifications for a given project, which means that the analysis process will start
    /// calling <see cref="IRpcEventSender.RaiseEventAsync"/> for this project.
    /// </summary>
    Task RegisterCallbackAsync( ProjectKey projectKey, [UsedImplicitly] CancellationToken cancellationToken = default );

    /// <summary>
    /// Notifies that the user is done editing compile-time code, so the pipeline can be resumed.
    /// </summary>
    Task OnCompileTimeCodeEditingCompletedAsync( [UsedImplicitly] CancellationToken cancellationToken = default );

    /// <summary>
    /// Notifies that a user interface (not only the user process, but our VSX) is attached to the user-process services and
    /// listens to <see cref="IRpcEventSender.RaiseEventAsync"/> for the <see cref="CompileTimeEditingStatusChangedEventData"/> event, so that the pipeline does not report
    /// editing-in-progress situations as errors.
    /// </summary>
    Task OnUserInterfaceAttachedAsync( [UsedImplicitly] CancellationToken cancellationToken = default );
}
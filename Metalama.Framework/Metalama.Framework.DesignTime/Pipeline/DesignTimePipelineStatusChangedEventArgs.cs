// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.DesignTime.Pipeline;

internal sealed record DesignTimePipelineStatusChangedEventArgs(
    DesignTimeAspectPipeline Pipeline,
    DesignTimeAspectPipelineStatus OldStatus,
    DesignTimeAspectPipelineStatus NewStatus )
{
    public bool IsPausing => this.OldStatus != DesignTimeAspectPipelineStatus.Paused && this.NewStatus == DesignTimeAspectPipelineStatus.Paused;

    public bool IsResuming => this.OldStatus == DesignTimeAspectPipelineStatus.Paused && this.NewStatus == DesignTimeAspectPipelineStatus.Default;
}
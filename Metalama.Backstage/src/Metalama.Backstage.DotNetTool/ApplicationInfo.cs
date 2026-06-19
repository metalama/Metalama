// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;

namespace Metalama.Backstage.DotNetTool;

internal sealed class ApplicationInfo : ApplicationInfoBase
{
    public ApplicationInfo() : base( typeof(ApplicationInfo).Assembly ) { }

    public override string Name => typeof(ApplicationInfo).Assembly.GetName().Name!;

    // The `metalama` global tool does not report telemetry: it is not repo-scoped, and the context-scoped telemetry
    // model collects only from repository-scoped builds/analysis. Disabling it here makes the process-level gate fail,
    // so the tool never activates telemetry nor creates a device identifier. See #1701.
    public override bool IsTelemetryEnabled => false;
}
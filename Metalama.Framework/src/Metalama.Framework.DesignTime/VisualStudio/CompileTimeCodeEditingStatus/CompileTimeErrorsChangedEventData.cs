// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Newtonsoft.Json;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;

[JsonObject]
internal sealed class CompileTimeErrorsChangedEventData : RpcEventData
{
    public ProjectKey ProjectKey { get; }

    public ImmutableArray<DiagnosticData> Errors { get; }

    public CompileTimeErrorsChangedEventData( ProjectKey projectKey, ImmutableArray<DiagnosticData> errors )
    {
        this.ProjectKey = projectKey;
        this.Errors = errors;
    }

    public override string Category => nameof(ICompileTimeCodeEditingStatusRpcApi);
}
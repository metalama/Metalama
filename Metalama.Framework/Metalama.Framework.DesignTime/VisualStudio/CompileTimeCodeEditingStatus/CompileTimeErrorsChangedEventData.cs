// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Newtonsoft.Json;

namespace Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;

[JsonObject]
internal sealed class CompileTimeErrorsChangedEventData : RpcEventData
{
    public ProjectKey ProjectKey { get; }

    public IReadOnlyCollection<DiagnosticData> Errors { get; }

    public CompileTimeErrorsChangedEventData( ProjectKey projectKey, IReadOnlyCollection<DiagnosticData> errors )
    {
        this.ProjectKey = projectKey;
        this.Errors = errors;
    }

    public override string Category => nameof(ICompileTimeCodeEditingStatusRpcApi);
}
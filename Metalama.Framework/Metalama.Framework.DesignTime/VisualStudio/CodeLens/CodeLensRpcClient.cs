// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.VisualStudio.CodeLens;

internal sealed class CodeLensRpcClient : RpcClient<ICodeLensRpcApi>
{
    public CodeLensRpcClient( ClientEndpoint endpoint ) : base( endpoint ) { }
}
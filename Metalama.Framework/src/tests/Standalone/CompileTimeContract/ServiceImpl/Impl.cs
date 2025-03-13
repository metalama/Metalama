// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Contract;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;

namespace ServiceImpl
{
    [MetalamaPlugIn]
    public class Impl : IContract
    {
        public string? GetDocumentationCommentId( IDeclaration declaration )
            => declaration.GetSymbol().GetDocumentationCommentId();
    }
}
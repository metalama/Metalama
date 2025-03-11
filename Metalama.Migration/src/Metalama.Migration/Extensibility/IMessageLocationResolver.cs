// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, an <see cref="IDeclaration"/> is also a <see cref="IDiagnosticLocation"/>.
    /// </summary>
    public interface IMessageLocationResolver : IService
    {
        MessageLocation GetMessageLocation( object codeElement );
    }
}
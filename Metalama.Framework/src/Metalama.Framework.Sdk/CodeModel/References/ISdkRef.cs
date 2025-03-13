// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References
{
    internal interface ISdkRef : IRef
    {
        // This is a temporary method to extract the symbol from the reference, when there is any.
        // In the final implementation, this method should not be necessary.
        ISymbol? GetSymbol( Compilation compilation, bool ignoreAssemblyKey = false );
    }
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Framework.Engine.HierarchicalOptions;

internal sealed class HierarchicalOptionsInstance
{
    public HierarchicalOptionsInstance( IDeclaration declaration, IHierarchicalOptions options )
    {
        this.Declaration = declaration;
        this.Options = options;
    }

    public IDeclaration Declaration { get; }

    public IHierarchicalOptions Options { get; }
}
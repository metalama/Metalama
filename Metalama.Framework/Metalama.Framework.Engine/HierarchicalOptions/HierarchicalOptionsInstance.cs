// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
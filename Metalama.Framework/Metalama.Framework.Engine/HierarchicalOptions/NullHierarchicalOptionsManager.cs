// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;
using System;

namespace Metalama.Framework.Engine.HierarchicalOptions;

internal sealed class NullHierarchicalOptionsManager : IHierarchicalOptionsManager
{
    private NullHierarchicalOptionsManager() { }

    public static IHierarchicalOptionsManager Instance { get; } = new NullHierarchicalOptionsManager();

    public IHierarchicalOptions? GetOptions( IDeclaration declaration, Type optionsType ) => null;
}
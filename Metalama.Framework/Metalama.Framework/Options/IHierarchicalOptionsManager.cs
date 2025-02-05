// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Options;

/// <summary>
/// Provides read-only access to hierarchical options.
/// </summary>
[CompileTime]
public interface IHierarchicalOptionsManager
{
    IHierarchicalOptions? GetOptions( IDeclaration declaration, Type optionsType );
}
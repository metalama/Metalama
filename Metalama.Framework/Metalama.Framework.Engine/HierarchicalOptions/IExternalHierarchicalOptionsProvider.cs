// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;
using Metalama.Framework.Services;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.HierarchicalOptions;

/// <summary>
/// Provides <see cref="IHierarchicalOptions"/> defined in a referenced project or assembly.
/// </summary>
internal interface IExternalHierarchicalOptionsProvider : IProjectService
{
    IEnumerable<string> GetOptionTypes();

    bool TryGetOptions( IDeclaration declaration, string optionsType, [NotNullWhen( true )] out IHierarchicalOptions? options );
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
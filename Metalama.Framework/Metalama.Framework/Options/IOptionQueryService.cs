// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Options;

internal interface IOptionQueryService : IProjectService
{
    void SetOptions<TDeclaration, TOptions>( IQuery<TDeclaration> query, Func<TDeclaration, TOptions> func )
        where TDeclaration : class, IDeclaration
        where TOptions : class, IHierarchicalOptions, IHierarchicalOptions<TDeclaration>, new();

    void SetOptions<TDeclaration, TTag, TOptions>( ITaggedQuery<TDeclaration, TTag> query, Func<TDeclaration, TTag, TOptions> func )
        where TDeclaration : class, IDeclaration
        where TOptions : class, IHierarchicalOptions, IHierarchicalOptions<TDeclaration>, new();
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
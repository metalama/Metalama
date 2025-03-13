// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Fabrics;
using System;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Queries;

internal sealed class ChildQuery<TDeclaration, TTag> : Query<TDeclaration, TTag>
    where TDeclaration : class, IDeclaration
{
    internal ChildQuery(
        IRef<IDeclaration> containingDeclaration,
        IQueryOwner owner,
        CompilationModelVersion compilationModelVersion,
        Func<Func<TDeclaration, TTag, QueryExecutionContext, Task>, QueryExecutionContext, Task> addTargets )
        : base( owner.ServiceProvider, containingDeclaration, compilationModelVersion, addTargets )
    {
        this.Owner = owner;
    }

    public override IQueryOwner Owner { get; }
}
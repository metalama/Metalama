// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
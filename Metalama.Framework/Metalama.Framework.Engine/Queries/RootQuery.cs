// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Queries;

internal sealed class RootQuery<T> : Query<T, object?>
    where T : class, IDeclaration
{
    internal RootQuery(
        IRef<IDeclaration> containingDeclaration,
        IQueryOwner owner,
        CompilationModelVersion compilationModelVersion ) : base(
        owner.ServiceProvider,
        containingDeclaration,
        compilationModelVersion,
        ( action, context ) => action( (T) containingDeclaration.GetTarget( context.Compilation ), 0, context ) )
    {
        this.Owner = owner;
    }

    public override IQueryOwner Owner { get; }

    protected override bool ShouldCache => false;
}
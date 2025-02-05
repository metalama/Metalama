// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
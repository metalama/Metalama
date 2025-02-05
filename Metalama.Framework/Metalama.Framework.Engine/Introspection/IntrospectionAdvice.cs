// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionAdvice : IIntrospectionAdvice
{
    private readonly Advice _advice;
    private readonly AdviceResult _adviceResult;
    private readonly ICompilation _compilation;
    private readonly IntrospectionFactory _factory;

    // TODO: we should not store an ICompilation here because it causes several compilation versions to be mixed in the introspection output.

    public IntrospectionAdvice( Advice advice, AdviceResult adviceResult, ICompilation compilation, IntrospectionFactory factory )
    {
        this._advice = advice;
        this._adviceResult = adviceResult;
        this._compilation = compilation;
        this._factory = factory;
    }

    [Memo]
    public IIntrospectionAspectInstance AspectInstance => this._factory.GetIntrospectionAspectInstance( this._advice.AspectInstance );

    public AdviceKind AdviceKind => this._advice.AdviceKind;

    public IDeclaration TargetDeclaration => this._advice.TargetDeclaration.Translate( this._compilation );

    public string AspectLayerId => this._advice.AspectLayerId.ToString();

    [Memo]
    public ImmutableArray<IIntrospectionTransformation> Transformations
        => this._adviceResult.Transformations.Select( x => new IntrospectionTransformation( x, this._compilation, this ) )
            .ToImmutableArray<IIntrospectionTransformation>();

    public override string ToString()
        => $"{this._advice.AdviceKind} on '{this._advice.TargetDeclaration}' provided by '{this._advice.AspectInstance.AspectClass.ShortName}'";
}
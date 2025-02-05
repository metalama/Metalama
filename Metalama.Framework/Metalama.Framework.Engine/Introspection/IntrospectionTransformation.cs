// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.Introspection;

internal sealed class IntrospectionTransformation : IIntrospectionTransformation
{
    private readonly ITransformation _transformation;
    private readonly ICompilation _compilation;

    public IntrospectionTransformation( ITransformation transformation, ICompilation compilation, IIntrospectionAdvice advice )
    {
        this._transformation = transformation;
        this._compilation = compilation;
        this.Advice = advice;
    }

    public IntrospectionTransformationKind TransformationKind => this._transformation.TransformationKind;

    [Memo]
    public IDeclaration TargetDeclaration => this._transformation.TargetDeclaration.GetTarget( this._compilation );

    [Memo]
    public FormattableString Description => FormattableStringHelper.MapString( this._transformation.ToDisplayString().AssertNotNull(), this._compilation );

    [Memo]
    public IDeclaration? IntroducedDeclaration
        => this._transformation switch
        {
            IIntroduceDeclarationTransformation introduceDeclarationTransformation => introduceDeclarationTransformation.DeclarationBuilderData.ToRef()
                .GetTarget( this._compilation ),
            IIntroduceInterfaceTransformation introduceInterfaceTransformation => introduceInterfaceTransformation.TargetType.GetTarget( this._compilation ),
            IntroduceParameterTransformation introduceParameterTransformation => introduceParameterTransformation.Parameter.ToRef()
                .GetTarget( this._compilation ),
            _ => null
        };

    public IIntrospectionAdvice Advice { get; }

    public int Order => this._transformation.OrderWithinPipelineStepAndTypeAndAspectInstance;

    public int CompareTo( IIntrospectionTransformation? other )
        => TransformationLinkerOrderComparer.Instance.Compare( this._transformation, ((IntrospectionTransformation?) other)?._transformation );

    public override string ToString() => this.Description.ToString( MetalamaStringFormatter.Instance );
}
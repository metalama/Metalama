// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideIndexerAdvice : OverrideMemberAdvice<IIndexer, IIndexer>
{
    private readonly BoundTemplateMethod? _getTemplate;
    private readonly BoundTemplateMethod? _setTemplate;

    public OverrideIndexerAdvice(
        AdviceConstructorParameters<IIndexer> parameters,
        BoundTemplateMethod? getTemplate,
        BoundTemplateMethod? setTemplate )
        : base( parameters )
    {
        this._getTemplate = getTemplate.ExplicitlyImplementedOrNull();
        this._setTemplate = setTemplate.ExplicitlyImplementedOrNull();
    }

    public override AdviceKind AdviceKind => AdviceKind.OverrideFieldOrPropertyOrIndexer;

    protected override OverrideMemberAdviceResult<IIndexer> Implement( in AdviceImplementationContext context )
    {
        var targetDeclaration = this.TargetDeclaration;

        context.AddTransformation(
            new OverrideIndexerTransformation( this.AspectLayerInstance, targetDeclaration.ToFullRef(), this._getTemplate, this._setTemplate ) );

        return this.CreateSuccessResult( targetDeclaration );
    }
}
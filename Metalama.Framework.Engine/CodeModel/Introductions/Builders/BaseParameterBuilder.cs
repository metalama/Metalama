// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal abstract class BaseParameterBuilder : NamedDeclarationBuilder, IParameterBuilder, IParameterImpl
{
    public IntroducedRef<IParameter> Ref { get; }

    public abstract IType Type { get; set; }

    public abstract RefKind RefKind { get; set; }

    public abstract int Index { get; }

    public abstract TypedConstant? DefaultValue { get; set; }

    public abstract bool IsParams { get; set; }

    public abstract bool IsThis { get; set; }

    public abstract IHasParameters DeclaringMember { get; }

    public abstract ParameterInfo ToParameterInfo();

    public abstract bool IsReturnParameter { get; }

    IRef<IParameter> IParameter.ToRef() => this.Ref;

    public sealed override IDeclaration ContainingDeclaration => this.DeclaringMember;

    protected BaseParameterBuilder( CompilationModel compilation, AspectLayerInstance aspectLayerInstance ) : base( aspectLayerInstance )
    {
        this.Ref = new IntroducedRef<IParameter>( compilation.RefFactory );
    }

    bool IExpression.IsAssignable => true;

    public ref object? Value => ref RefHelper.Wrap( new SyntaxUserExpression( SyntaxFactory.IdentifierName( this.Name ), this.Type, true ) );

    public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        => new(
            new TypedExpressionSyntaxImpl(
                SyntaxFactory.IdentifierName( this.Name ),
                this.Type,
                ((SyntaxSerializationContext) syntaxGenerationContext).CompilationModel,
                true ) );

    protected override void EnsureReferenceInitialized()
    {
        this.Ref.BuilderData = new ParameterBuilderData( this, this.ContainingDeclaration.ToFullRef() );
    }

    public ParameterBuilderData BuilderData => (ParameterBuilderData) this.Ref.BuilderData;

    public override bool IsDesignTimeObservable => true;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;
}
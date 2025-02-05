// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// A transformation that appends an argument to the initializer call of a constructor.
/// </summary>
internal sealed class IntroduceConstructorInitializerArgumentTransformation : BaseSyntaxTreeTransformation, IMemberLevelTransformation
{
    private readonly IFullRef<IConstructor> _constructor;

    IFullRef<IMember> IMemberLevelTransformation.TargetMember => this._constructor;

    public int ParameterIndex { get; }

    private ExpressionSyntax Value { get; }

    public IntroduceConstructorInitializerArgumentTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IConstructor> constructor,
        int parameterIndex,
        ExpressionSyntax value ) : base( aspectLayerInstance, constructor )
    {
        this._constructor = constructor;
        this.ParameterIndex = parameterIndex;
        this.Value = value;
    }

    public ArgumentSyntax ToSyntax()
        => SyntaxFactory.Argument( this.Value ).WithAdditionalAnnotations( this.AspectInstance.AspectClass.GeneratedCodeAnnotation );

    public override IFullRef<IDeclaration> TargetDeclaration => this._constructor;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertConstructorInitializerArgument;

    public override FormattableString ToDisplayString() => $"Introduce an argument to the initializer of constructor '{this._constructor}'.";
}
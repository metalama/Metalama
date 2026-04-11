// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceParameterTransformation : BaseSyntaxTreeTransformation, IMemberLevelTransformation
{
    public IFullRef<IMember> TargetMember => this.Parameter.ContainingDeclaration.As<IMember>();

    public ParameterBuilderData Parameter { get; }

    /// <summary>
    /// Gets a value indicating whether, on a record primary constructor target, this introduced parameter should
    /// participate in the record's value shape (property, <c>Deconstruct</c>, <c>Equals</c>/<c>GetHashCode</c>/<c>ToString</c>).
    /// When <c>false</c> (default), the parameter lives as a constructor parameter only; the linker strips the primary
    /// header and emits a body-declared constructor without a matching positional property. Ignored for non-record targets.
    /// </summary>
    public bool MaterializeOnRecord { get; }

    public IntroduceParameterTransformation(
        AspectLayerInstance aspectLayerInstance,
        ParameterBuilderData parameter,
        bool materializeOnRecord = false ) : base(
        aspectLayerInstance,
        parameter.ContainingDeclaration )
    {
        this.Parameter = parameter;
        this.MaterializeOnRecord = materializeOnRecord;
    }

    public ParameterSyntax ToSyntax( SyntaxGenerationContext syntaxGenerationContext, CompilationModel compilation )
    {
        // We only add parameters to source declarations. For introduced declarations, the IntroductionTransformation already adds
        // the parameters.
        Invariant.Assert( this.TargetMember is not IIntroducedRef );

        var syntax = SyntaxFactory.Parameter(
            syntaxGenerationContext.SyntaxGenerator.AttributesForDeclaration( this.Parameter.ToRef(), compilation ),
            default,
            syntaxGenerationContext.SyntaxGenerator.TypeSyntax( this.Parameter.Type )
                .WithOptionalTrailingTrivia( SyntaxFactory.ElasticSpace, syntaxGenerationContext.Options ),
            SyntaxFactoryEx.SafeIdentifier( this.Parameter.Name.AssertNotNull() ),
            null );

        if ( this.Parameter.DefaultValue != null )
        {
            syntax = syntax.WithDefault(
                SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.Token(
                        new SyntaxTriviaList( SyntaxFactory.ElasticSpace ),
                        SyntaxKind.EqualsToken,
                        new SyntaxTriviaList( SyntaxFactory.ElasticSpace ) ),
                    syntaxGenerationContext.SyntaxGenerator.TypedConstant( this.Parameter.DefaultValue.Value, this.TargetMember.RefFactory ) ) );
        }

        return syntax;
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this.TargetMember;

    public override TransformationObservability Observability => TransformationObservability.Always;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.IntroduceParameter;

    public override FormattableString ToDisplayString()
    {
        var containingDeclarationDefinition = this.Parameter.ContainingDeclaration.Definition;

        return
            $"Introduce parameter '{this.Parameter.Name}' into {containingDeclarationDefinition.DeclarationKind.ToDisplayString()} '{containingDeclarationDefinition}'.";
    }
}
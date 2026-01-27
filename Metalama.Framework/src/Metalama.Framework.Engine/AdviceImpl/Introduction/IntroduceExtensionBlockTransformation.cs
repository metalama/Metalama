// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Transformation for introducing extension blocks into static classes.
/// </summary>
internal sealed class IntroduceExtensionBlockTransformation : IntroduceDeclarationTransformation<ExtensionBlockBuilderData>
{
    public IntroduceExtensionBlockTransformation(
        AspectLayerInstance aspectLayerInstance,
        ExtensionBlockBuilderData introducedDeclaration )
        : base( aspectLayerInstance, introducedDeclaration ) { }

    public override TransformationObservability Observability => TransformationObservability.Always;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var extensionBlock = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );
        var receiverParam = extensionBlock.ReceiverParameter;
        var receiverTypeSyntax = context.SyntaxGenerator.TypeSyntax( receiverParam.Type );

        // Build the receiver parameter syntax
        var parameterSyntax = Parameter(
            AdviceSyntaxGenerator.GetAttributeLists( receiverParam, context ),
            ModifierHelper.GetRefKindModifiers( receiverParam.RefKind ),
            receiverTypeSyntax,
            this.BuilderData.IsStaticExtension ? default : SyntaxFactoryEx.SafeIdentifier( receiverParam.Name ),
            null );

        // Build type parameter list if any
        var typeParameterList = this.BuilderData.TypeParameters.IsEmpty
            ? null
            : TypeParameterList( SeparatedList( this.BuilderData.TypeParameters.Select( tp => TypeParameter( SyntaxFactoryEx.SafeIdentifier( tp.Name ) ) ) ) );

        // Get constraint clauses
        var constraintClauses = context.SyntaxGenerator.ConstraintClauses( extensionBlock );

        // Generate: extension (ReceiverType) { } or extension (ReceiverType paramName) { }
        // or extension<T> (ReceiverType) { } for generic extension blocks
        var extensionSyntax = ExtensionBlockDeclaration(
            AdviceSyntaxGenerator.GetAttributeLists( extensionBlock, context ),
            default, // Modifiers (extension blocks don't have modifiers).
            Token( SyntaxKind.ExtensionKeyword ),
            typeParameterList,
            ParameterList( SingletonSeparatedList( parameterSyntax ) ),
            constraintClauses,
            Token( SyntaxKind.OpenBraceToken ),
            List<MemberDeclarationSyntax>(), // Empty - members added by Linker.
            Token( SyntaxKind.CloseBraceToken ),
            default );

        return
        [
            new InjectedMember(
                this,
                extensionSyntax.NormalizeWhitespaceIfNecessary( context.SyntaxGenerationContext ),
                this.AspectLayerId,
                InjectedMemberSemantic.Introduction,
                this.BuilderData.ToRef().As<IDeclaration>() )
        ];
    }
}
#endif
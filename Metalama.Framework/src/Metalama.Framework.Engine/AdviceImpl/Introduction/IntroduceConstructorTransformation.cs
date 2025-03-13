// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroduceConstructorTransformation
    : IntroduceMemberTransformation<ConstructorBuilderData>, IReplaceMemberTransformation, IInsertStatementTransformation
{
    public IntroduceConstructorTransformation( AspectLayerInstance aspectLayerInstance, ConstructorBuilderData introducedDeclaration ) : base(
        aspectLayerInstance,
        introducedDeclaration )
    {
        Invariant.Assert( !introducedDeclaration.IsStatic );

        this.ReplacedMember = introducedDeclaration.ReplacedImplicitConstructor;
    }

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        if ( this.BuilderData.IsImplicitlyDeclared )
        {
            return [];
        }

        // TODO: We must generate the code based on our _initial_ compilation because the last compilation may already contain introduced
        // parameters, but these parameters will be added by the linker. We would have duplicates by adding them here.
        // However, if we resolve to the initial compilation, we may get the replaced (implicit) constructor instead of the new one.
        var constructorBuilder = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        Invariant.Assert( !constructorBuilder.IsRecordCopyConstructor() );

        var statements = Array.Empty<StatementSyntax>();

        var syntaxSerializationContext = new SyntaxSerializationContext(
            context.FinalCompilation,
            context.SyntaxGenerationContext,
            constructorBuilder.DeclaringType );

        var arguments =
            ArgumentList(
                SeparatedList(
                    this.BuilderData.InitializerArguments.SelectAsArray(
                        a =>
                            Argument(
                                a.ParameterName != null
                                    ? NameColon( IdentifierName( a.ParameterName ) )
                                    : null,
                                default,
                                a.Expression.ToExpressionSyntax( syntaxSerializationContext ) ) ) ) );

        var initializer =
            constructorBuilder.InitializerKind switch
            {
                ConstructorInitializerKind.None => null,
                ConstructorInitializerKind.Base =>
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        arguments ),
                ConstructorInitializerKind.This =>
                    ConstructorInitializer(
                        SyntaxKind.ThisConstructorInitializer,
                        arguments ),
                var i => throw new AssertionFailedException( $"Unsupported initializer kind: {i}" )
            };

        var syntax =
            ConstructorDeclaration(
                AdviceSyntaxGenerator.GetAttributeLists( constructorBuilder, context ),
                constructorBuilder.GetSyntaxModifierList(),
                Identifier( constructorBuilder.DeclaringType.Name ),
                context.SyntaxGenerator.ParameterList( constructorBuilder, context.FinalCompilation ),
                initializer,
                context.SyntaxGenerationContext.SyntaxGenerator.FormattedBlock( statements )
                    .WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation ),
                null );

        return
        [
            new InjectedMember(
                this,
                syntax,
                this.AspectLayerId,
                InjectedMemberSemantic.Introduction,
                this.BuilderData.ToRef() )
        ];
    }

    public IFullRef<IMember>? ReplacedMember { get; }

    public override InsertPosition InsertPosition => this.ReplacedMember?.ToInsertPosition() ?? this.BuilderData.InsertPosition;

    public override TransformationObservability Observability
        => this.ReplacedMember == null && !this.BuilderData.IsImplicitlyDeclared
            ? TransformationObservability.Always
            : TransformationObservability.CompileTimeOnly;

    public IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context )
    {
        var constructorBuilder = this.BuilderData.ToRef().GetTarget( context.FinalCompilation );

        // See https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11#auto-default-struct.
        if ( constructorBuilder.DeclaringType.TypeKind is TypeKind.Struct or TypeKind.RecordStruct &&
             context.SyntaxGenerationContext.RequiresStructFieldInitialization )
        {
            return
            [
                // this = default;
                new InsertedStatement(
                    ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                ThisExpression(),
                                LiteralExpression( SyntaxKind.DefaultLiteralExpression ) ) )
                        .WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation ),
                    constructorBuilder,
                    this,
                    InsertedStatementKind.Initializer )
            ];
        }

        return Array.Empty<InsertedStatement>();
    }

    IFullRef<IMember> IInsertStatementTransformation.TargetMember => this.BuilderData.ToRef();
}
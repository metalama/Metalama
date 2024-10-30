// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.AdviceImpl;

internal class AdviceSyntaxGenerator
{
    public static SyntaxList<AttributeListSyntax> GetAttributeLists(
        IDeclaration declaration,
        MemberInjectionContext context,
        SyntaxKind attributeTargetSyntaxKind = SyntaxKind.None )
    {
        var attributes = context.SyntaxGenerator.AttributesForDeclaration(
            declaration.ToFullRef(),
            context.FinalCompilation,
            attributeTargetSyntaxKind );

        if ( declaration is IMethod method )
        {
            attributes = attributes.AddRange(
                context.SyntaxGenerator.AttributesForDeclaration(
                    method.ReturnParameter.ToFullRef(),
                    context.FinalCompilation,
                    SyntaxKind.ReturnKeyword ) );

            if ( method.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.PropertySet )
            {
                attributes = attributes.AddRange(
                    context.SyntaxGenerator.AttributesForDeclaration(
                        method.Parameters[0].ToFullRef(),
                        context.FinalCompilation,
                        SyntaxKind.ParamKeyword ) );
            }
        }
        else if ( declaration is IProperty { IsAutoPropertyOrField: true } )
        {
            // TODO: field-level attributes
        }

        return attributes;
    }

    // TODO: This is temporary overload (see the callsite for reason).
    public static SyntaxList<AttributeListSyntax> GetAttributeLists(
        IFullRef<IDeclaration> declarationRef,
        MemberInjectionContext context,
        SyntaxKind attributeTargetSyntaxKind = SyntaxKind.None )
    {
        var attributes = context.SyntaxGenerator.AttributesForDeclaration(
            declarationRef,
            context.FinalCompilation,
            attributeTargetSyntaxKind );

        return attributes;
    }

    private static bool TryExpandInitializerTemplate<T>(
        T member,
        AspectLayerInstance aspectLayerInstance,
        MemberInjectionContext context,
        TemplateMember<T> initializerTemplate,
        [NotNullWhen( true )] out BlockSyntax? expression )
        where T : class, IMember
    {
        var metaApi = MetaApi.ForInitializer(
            member,
            new MetaApiProperties(
                aspectLayerInstance.InitialCompilation,
                context.DiagnosticSink,
                initializerTemplate.AsMemberOrNamedType(),
                aspectLayerInstance.AspectLayerId,
                context.SyntaxGenerationContext,
                aspectLayerInstance.AspectInstance,
                context.ServiceProvider,
                MetaApiStaticity.Default ) );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            member,
            initializerTemplate.TemplateProvider,
            aspectLayerInstance.AspectLayerId );

        var templateDriver = initializerTemplate.Driver;

        return templateDriver.TryExpandDeclaration( expansionContext, [], out expression );
    }

    public static bool GetInitializerExpressionOrMethod<T>(
        T member,
        AspectLayerInstance aspectLayerInstance,
        MemberInjectionContext context,
        IType targetType,
        IExpression? initializerExpression,
        TemplateMember<T>? initializerTemplate,
        out ExpressionSyntax? initializerExpressionSyntax,
        out MethodDeclarationSyntax? initializerMethodSyntax )
        where T : class, IMember
    {
        if ( context is null )
        {
            throw new ArgumentNullException( nameof(context) );
        }

        if ( targetType is null )
        {
            throw new ArgumentNullException( nameof(targetType) );
        }

        if ( context.SyntaxGenerationContext.IsPartial && (initializerExpression != null || initializerTemplate != null) )
        {
            // At design time when generating the partial code for source generators, we do not expand templates.
            // This may cause warnings in the constructor (because some fields will not be initialized)
            // but we will add that later. The main point is that we should not execute the template here.

            initializerMethodSyntax = null;
            initializerExpressionSyntax = null;

            return true;
        }

        if ( initializerExpression != null )
        {
            // TODO: Error about the expression type?
            initializerMethodSyntax = null;

            try
            {
                initializerExpressionSyntax =
                    initializerExpression.ToExpressionSyntax(
                        new SyntaxSerializationContext( context.FinalCompilation, context.SyntaxGenerationContext, member.DeclaringType ),
                        targetType );
            }
            catch ( Exception ex )
            {
                context.DiagnosticSink.Report( GeneralDiagnosticDescriptors.CantGetMemberInitializer.CreateRoslynDiagnostic( null, (member, ex.Message) ) );

                initializerExpressionSyntax = null;

                return false;
            }

            return true;
        }
        else if ( initializerTemplate != null )
        {
            if ( !TryExpandInitializerTemplate( member, aspectLayerInstance, context, initializerTemplate, out var initializerBlock ) )
            {
                // Template expansion error.
                initializerMethodSyntax = null;
                initializerExpressionSyntax = null;

                return false;
            }

            // If the initializer block contains only a single return statement, 
            if ( initializerBlock.Statements is [ReturnStatementSyntax { Expression: not null } returnStatement] )
            {
                initializerMethodSyntax = null;
                initializerExpressionSyntax = returnStatement.Expression;

                return true;
            }

            var initializerName = context.InjectionNameProvider.GetInitializerName( member.DeclaringType, aspectLayerInstance.AspectLayerId, member );

            initializerExpressionSyntax = InvocationExpression( IdentifierName( initializerName ) );

            initializerMethodSyntax =
                MethodDeclaration(
                    List<AttributeListSyntax>(),
                    TokenList(
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ),
                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) ),
                    context.SyntaxGenerator.TypeSyntax( targetType )
                        .WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
                    null,
                    Identifier( initializerName ),
                    null,
                    ParameterList(),
                    List<TypeParameterConstraintClauseSyntax>(),
                    initializerBlock,
                    null );

            return true;
        }
        else
        {
            initializerMethodSyntax = null;
            initializerExpressionSyntax = null;

            return true;
        }
    }
}
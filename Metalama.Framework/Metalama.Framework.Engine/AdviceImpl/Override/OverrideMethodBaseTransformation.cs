// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Metalama.Framework.Code.MethodKind;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal abstract class OverrideMethodBaseTransformation : OverrideMemberTransformation
{
    protected IFullRef<IMethod> OverriddenMethod { get; }

    protected OverrideMethodBaseTransformation( AspectLayerInstance aspectLayerInstance, IFullRef<IMethod> targetMethod )
        : base( aspectLayerInstance, targetMethod )
    {
        this.OverriddenMethod = targetMethod;
    }

    public override IFullRef<IMember> OverriddenDeclaration => this.OverriddenMethod;

    protected SyntaxUserExpression CreateProceedExpression( MemberInjectionContext context, TemplateKind templateKind )
        => ProceedHelper.CreateProceedDynamicExpression(
            context.SyntaxGenerationContext,
            this.CreateInvocationExpression( context ),
            templateKind,
            this.OverriddenMethod.GetTarget( context.FinalCompilation ) );

    protected InjectedMember[] GetInjectedMembersImpl( MemberInjectionContext context, BlockSyntax newMethodBody, bool isAsyncTemplate )
    {
        TypeSyntax? returnType = null;

        var overriddenDeclaration = this.OverriddenMethod.GetTarget( this.InitialCompilation );

        var modifiers = overriddenDeclaration
            .GetSyntaxModifierList( ModifierCategories.Static | ModifierCategories.Async | ModifierCategories.Unsafe )
            .Insert( 0, SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

        if ( !overriddenDeclaration.IsAsync )
        {
            if ( isAsyncTemplate )
            {
                // If the template is async but the overridden declaration is not, we have to add an async modifier.
                modifiers = modifiers.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.AsyncKeyword ) );
            }
        }
        else
        {
            if ( !isAsyncTemplate )
            {
                // If the template is not async but the overridden declaration is, we have to remove the async modifier.
                modifiers = TokenList( modifiers.Where( m => !m.IsKind( SyntaxKind.AsyncKeyword ) ) );
            }

            // If the template is async and the target declaration is `async void`, and regardless of the async flag the template, we have to change the type to ValueTask, otherwise
            // it is not awaitable

            if ( overriddenDeclaration.ReturnType.Equals( SpecialType.Void ) )
            {
                returnType = context.SyntaxGenerator.TypeSyntax( overriddenDeclaration.GetCompilationContext().ReflectionMapper.GetTypeSymbol( typeof(ValueTask) ) );
            }
        }

        returnType ??= context.SyntaxGenerator.TypeSyntax( overriddenDeclaration.ReturnType );

        var introducedMethod = MethodDeclaration(
            List<AttributeListSyntax>(),
            modifiers,
            returnType.WithOptionalTrailingTrivia( ElasticSpace, context.SyntaxGenerationContext.Options ),
            null,
            Identifier(
                context.InjectionNameProvider.GetOverrideName(
                    overriddenDeclaration.DeclaringType,
                    this.AspectLayerId,
                    overriddenDeclaration ) ),
            context.SyntaxGenerator.TypeParameterList( overriddenDeclaration, context.FinalCompilation ),
            context.SyntaxGenerator.ParameterList( overriddenDeclaration, context.FinalCompilation, true ),
            context.SyntaxGenerator.ConstraintClauses( overriddenDeclaration ),
            newMethodBody,
            null );

        return
        [
            new InjectedMember(
                this,
                introducedMethod,
                this.AspectLayerId,
                InjectedMemberSemantic.Override,
                overriddenDeclaration.ToFullRef() )
        ];
    }

    private ExpressionSyntax CreateInvocationExpression( MemberInjectionContext context )
    {
        var overriddenDeclaration = this.OverriddenMethod.GetTarget( context.FinalCompilation );

        return overriddenDeclaration.MethodKind switch
        {
            MethodKind.Default or MethodKind.ExplicitInterfaceImplementation =>
                InvocationExpression(
                    this.CreateMemberAccessExpression( AspectReferenceTargetKind.Self, context ),
                    ArgumentList(
                        SeparatedList(
                            overriddenDeclaration.Parameters.SelectAsReadOnlyList(
                                p => Argument( null, p.RefKind.InvocationRefKindToken(), IdentifierName( p.Name ) ) ) ) ) ),
            MethodKind.Finalizer =>
                context.AspectReferenceSyntaxProvider.GetFinalizerReference( this.AspectLayerId ),
            MethodKind.Operator =>
                context.AspectReferenceSyntaxProvider.GetOperatorReference(
                    this.AspectLayerId,
                    (IMethod) this.TargetDeclaration.GetTarget( context.FinalCompilation ),
                    context.SyntaxGenerator ),
            _ => throw new AssertionFailedException( $"Unsupported method kind: {overriddenDeclaration} is {overriddenDeclaration.MethodKind}." )
        };
    }
}
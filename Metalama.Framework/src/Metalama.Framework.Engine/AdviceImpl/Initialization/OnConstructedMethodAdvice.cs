// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Accessibility = Metalama.Framework.Code.Accessibility;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Implements <see cref="InitializerKind.AfterLastInstanceConstructor"/>: introduces (or reuses) a
/// <c>public virtual void OnConstructed(InitializationContext context = default)</c> method on the
/// target type, injects the template body into it, and emits a trailing <c>this.OnConstructed(context)</c>
/// call at the end of every non-<c>:this(...)</c> instance constructor (after pulling an
/// <c>InitializationContext</c> parameter through the constructor chain).
/// </summary>
internal sealed class OnConstructedMethodAdvice : Advice<AddInitializerAdviceResult>
{
    private const string _methodName = "OnConstructed";

    // Default name given to the InitializationContext parameter when this advice introduces a new
    // OnConstructed method or a new constructor parameter. When the target constructor or method
    // already has an InitializationContext parameter, that parameter's existing name is preserved.
    private const string _defaultContextParameterName = "context";

    private readonly TemplateMember<IMethod> _template;
    private readonly IObjectReader? _templateArguments;

    private new INamedType TargetDeclaration => (INamedType) base.TargetDeclaration;

    public OnConstructedMethodAdvice(
        in AdviceConstructorParameters<INamedType> parameters,
        TemplateMember<IMethod> template,
        IObjectReader? templateArguments,
        IEnumerable<IField>? slotFields )
        : base( parameters )
    {
        this._template = template;
        this._templateArguments = templateArguments;

        // slotFields is accepted by the API but the slot-coordination code path is not yet implemented.
        // Phase 3 will emit `if (!context.IsHandledBy(Slot))` guards around the epilogue call.
        _ = slotFields;
    }

    protected override AddInitializerAdviceResult Implement( AdviceImplementationContext context )
    {
        var targetType = this.TargetDeclaration.ToRef().GetTarget( context.MutableCompilation );
        var factory = targetType.Compilation.Factory;
        var initContextType = factory.GetTypeByReflectionType( typeof(InitializationContext) );

        // Records are rejected — same rule as BeforeInstanceConstructor, because the compiler-generated
        // copy constructor cannot be modified.
        if ( targetType.IsRecord )
        {
            return this.CreateFailedResult(
                AdviceDiagnosticDescriptors.CannotAddInitializerToRecord.CreateRoslynDiagnostic(
                    targetType.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, targetType),
                    this ) );
        }

        // Phase 1 gate: base type already has OnConstructed. Phase 3 will remove this restriction and
        // implement proper slot coordination across inheritance.
        if ( BaseTypeHasOnConstructed( targetType, initContextType ) )
        {
            return this.CreateFailedResult(
                AdviceDiagnosticDescriptors.CannotAddOnConstructedToDerivedType.CreateRoslynDiagnostic(
                    targetType.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, targetType),
                    this ) );
        }

        // Step 1: introduce (or, if the user wrote one, resolve) OnConstructed on this type.
        var onConstructedMethod = this.IntroduceOrResolveOnConstructed( targetType, initContextType, context );

        // Step 2: bind and inject the template body into OnConstructed.
        var boundTemplate = this._template.ForInitializer( onConstructedMethod, this._templateArguments );

        context.AddTransformation(
            new InsertTemplateStatementsTransformation(
                this.AspectLayerInstance,
                targetType.ToRef(),
                onConstructedMethod.ToFullRef(),
                boundTemplate ) );

        // Step 3: for each non-`:this(...)` instance constructor, ensure the `context` parameter
        // and emit the epilogue call `this.OnConstructed(context);`.
        foreach ( var sourceCtor in targetType.Constructors
                     .Where( c => c.InitializerKind != ConstructorInitializerKind.This
                                  && !c.IsRecordCopyConstructor() )
                     .ToList() )
        {
            var (targetConstructor, contextParameterName) = InitializationContextParameterHelper.EnsureContextParameter(
                sourceCtor,
                initContextType,
                context,
                this.AspectLayerInstance,
                _defaultContextParameterName );

            // Early `return;` statements in the constructor body are rewritten to
            // `goto __metalama_epilogue;` by ConstructorEpilogueRewriter (invoked from
            // LinkerInjectionStep.Rewriter.ReplaceBlock) so the epilogue still fires.
            context.AddTransformation(
                new InsertSyntaxStatementsTransformation(
                    this.AspectLayerInstance,
                    targetType.ToRef(),
                    targetConstructor.ToFullRef(),
                    _ => BuildOnConstructedCallStatement( contextParameterName ),
                    InsertedStatementKind.InitializerEpilogue ) );
        }

        return new AddInitializerAdviceResult( AdviceOutcome.Success, this.AdviceFactory );
    }

    private IMethod IntroduceOrResolveOnConstructed(
        INamedType targetType,
        IType initContextType,
        AdviceImplementationContext context )
    {
        // If the user already declared a matching `OnConstructed(InitializationContext)` method on the target,
        // reuse it.
        var existing = targetType.Methods
            .OfName( _methodName )
            .SingleOrDefault(
                m => m.Parameters.Count == 1
                     && m.Parameters[0].Type.Equals( initContextType ) );

        if ( existing != null )
        {
            return existing;
        }

        var factory = targetType.Compilation.Factory;

        var builder = new MethodBuilder( this.AspectLayerInstance, targetType, _methodName )
        {
            ReturnType = factory.GetSpecialType( Code.SpecialType.Void ),
            Accessibility = Accessibility.Public,
            IsVirtual = !targetType.IsSealed && targetType.TypeKind != Code.TypeKind.Struct
        };

        builder.AddParameter( _defaultContextParameterName, initContextType, defaultValue: TypedConstant.Default( initContextType ) );

        builder.Freeze();

        context.AddTransformation(
            new IntroduceMethodTransformation( this.AspectLayerInstance, builder.BuilderData ) );

        return builder;
    }

    private static bool BaseTypeHasOnConstructed( INamedType targetType, IType initContextType )
    {
        for ( var baseType = targetType.BaseType; baseType != null; baseType = baseType.BaseType )
        {
            var baseMethod = baseType.Methods
                .OfName( _methodName )
                .FirstOrDefault(
                    m => m.Parameters.Count == 1
                         && m.Parameters[0].Type.Equals( initContextType ) );

            if ( baseMethod != null )
            {
                return true;
            }
        }

        return false;
    }

    private static StatementSyntax BuildOnConstructedCallStatement( string contextParameterName )
        => ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName( _methodName ) ),
                ArgumentList(
                    SingletonSeparatedList(
                        Argument( SyntaxFactoryEx.SafeIdentifierName( contextParameterName ) ) ) ) ) );

    public override AdviceKind AdviceKind => AdviceKind.AddInitializer;

    protected override AddInitializerAdviceResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceOutcome.Error, this.AdviceFactory, diagnostics );
}

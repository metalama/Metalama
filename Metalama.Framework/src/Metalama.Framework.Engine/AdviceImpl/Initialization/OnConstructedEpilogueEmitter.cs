// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.RunTime;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Emits the per-constructor portion of <c>InitializerKind.AfterLastInstanceConstructor</c>:
/// for each non-<c>:this(...)</c> instance constructor of the target type, ensures the
/// <see cref="InitializationContext"/> parameter, emits the epilogue
/// <c>if (!context.IsHandled(InitializationSlot.OnConstructed)) this.OnConstructed(context);</c>,
/// and rewrites the <c>:base(...)</c> argument to <c>context.Descend(InitializationSlot.OnConstructed)</c>
/// when the base constructor has the same parameter.
/// </summary>
/// <remarks>
/// Shared between the in-project path (<see cref="OnConstructedMethodAdvice"/>) and the cross-project
/// path (<see cref="OnConstructedEpilogueAdvice"/>, invoked from
/// <see cref="AddConstructorEpilogueTransitiveAspect"/> in dependent projects).
/// </remarks>
internal static class OnConstructedEpilogueEmitter
{
    // Default name given to the InitializationContext parameter when this helper introduces a new
    // constructor parameter. When the target constructor already has an InitializationContext parameter,
    // that parameter's existing name is preserved.
    private const string _defaultContextParameterName = "context";

    /// <summary>
    /// Emits the epilogue and the descend rewrite for every eligible constructor of
    /// <paramref name="targetType"/>.
    /// </summary>
    /// <param name="targetType">The type whose constructors are processed.</param>
    /// <param name="initContextType">The <see cref="InitializationContext"/> type.</param>
    /// <param name="aspectLayerInstance">The aspect layer that owns the emitted transformations.</param>
    /// <param name="context">The advice implementation context used to add transformations.</param>
    /// <param name="registerPullFallback">When <c>true</c> (in-project path), missing parameters are
    /// introduced and the recursive parameter pull is triggered. When <c>false</c> (cross-project path),
    /// missing parameters indicate a contract violation: the pull aspect should have run first via
    /// <see cref="PullConstructorParameterTransitiveAspect"/>; the constructor is silently skipped.</param>
    public static void EmitForType(
        INamedType targetType,
        IType initContextType,
        AspectLayerInstance aspectLayerInstance,
        AdviceImplementationContext context,
        bool registerPullFallback )
    {
        var guardEpilogue = !targetType.IsSealed && targetType.TypeKind != Code.TypeKind.Struct;

        foreach ( var sourceCtor in targetType.Constructors
                     .Where( c => c.InitializerKind != ConstructorInitializerKind.This
                                  && !c.IsRecordCopyConstructor() )
                     .ToList() )
        {
            var ensured = EnsureOrFindContextParameter( sourceCtor, initContextType, aspectLayerInstance, context, registerPullFallback );

            if ( ensured == null )
            {
                // Cross-project path: parameter is unexpectedly absent. Skip.
                continue;
            }

            var (targetConstructor, contextParameterName) = ensured.Value;

            // Early `return;` statements in the constructor body are rewritten to
            // `goto __metalama_epilogue;` by ConstructorEpilogueRewriter (invoked from
            // LinkerInjectionStep.Rewriter.ReplaceBlock) so the epilogue still fires.
            // Uses an aggregatable transformation so that multiple peer aspects produce a single
            // deduplicated epilogue call per constructor.
            context.AddTransformation(
                new OnConstructedEpilogueTransformation(
                    aspectLayerInstance,
                    targetType.ToRef(),
                    targetConstructor.ToFullRef(),
                    contextParameterName,
                    guardEpilogue ) );

            // If the :base(...) target has an InitializationContext parameter, replace the pulled
            // argument with `context.Descend(OnConstructed)` so the base constructor's epilogue skips.
            if ( targetConstructor.InitializerKind != ConstructorInitializerKind.This )
            {
                EmitDescendOverrideForBaseInitializer(
                    targetConstructor,
                    initContextType,
                    contextParameterName,
                    aspectLayerInstance,
                    context );
            }
        }
    }

    /// <summary>
    /// Resolves the <see cref="InitializationContext"/> parameter on <paramref name="sourceConstructor"/>.
    /// If a parameter of that type already exists (introduced by another aspect, by another peer, or
    /// hand-written), its existing name is returned and no new parameter is introduced. Otherwise, when
    /// <paramref name="registerPullFallback"/> is <c>true</c>, an implicit constructor is materialized as
    /// needed, a new parameter is introduced with <c>default(InitializationContext)</c> as its default
    /// value, and the parameter is pulled recursively through <c>:this(...)</c> / <c>:base(...)</c> chains
    /// (and across project boundaries via <see cref="PullConstructorParameterTransitiveAspect"/>).
    /// </summary>
    /// <returns>
    /// The (possibly materialized) constructor and the name of the <see cref="InitializationContext"/>
    /// parameter, or <c>null</c> if no parameter could be resolved (only happens in the cross-project path).
    /// </returns>
    private static (IConstructor Constructor, string ContextParameterName)? EnsureOrFindContextParameter(
        IConstructor sourceConstructor,
        IType initializationContextType,
        AspectLayerInstance aspectLayerInstance,
        AdviceImplementationContext context,
        bool registerPullFallback )
    {
        // 1. Look for an existing parameter of type InitializationContext on this constructor.
        // This path satisfies the subtle constraint: even if the parameter was added by another aspect
        // or by the user, the epilogue must still be added using that existing parameter's name.
        var existing = sourceConstructor.Parameters.FirstOrDefault(
            p => p.Type.Equals( initializationContextType ) );

        if ( existing != null )
        {
            return (sourceConstructor, existing.Name);
        }

        if ( !registerPullFallback )
        {
            // Cross-project path: PullConstructorParameterTransitiveAspect runs before the epilogue
            // aspect, so a missing parameter here means the pull was not registered for this base. The
            // contract is "the epilogue aspect consumes whatever parameter is present"; without one,
            // we cannot synthesize a meaningful epilogue.
            return null;
        }

        // 2. Materialize implicit constructor to an explicit one if needed.
        var constructor = sourceConstructor;

        if ( constructor.IsImplicitInstanceConstructor() )
        {
            var constructorBuilder = new ConstructorBuilder( aspectLayerInstance, constructor );
            constructorBuilder.Freeze();
            context.AddTransformation( constructorBuilder.CreateTransformation() );
            constructor = constructorBuilder;
        }

        // 3. Introduce the parameter. For record primaries, the linker's primary-constructor
        // materialization path (driven by LateTypeLevelTransformations.RemovePrimaryConstructor)
        // strips the primary from the record header and emits an explicit body-declared ctor.
        // Because the pull strategy below sets materializeOnRecord: false, the linker also filters
        // the parameter out of the property/Deconstruct/assignment emission sites.
        var parameterBuilder = new ParameterBuilder(
            constructor,
            constructor.Parameters.Count,
            _defaultContextParameterName,
            initializationContextType,
            RefKind.None,
            aspectLayerInstance ) { DefaultValue = TypedConstant.Default( initializationContextType ) };

        if ( constructor.CanBeChainedFromOutsideAssembly() )
        {
            parameterBuilder.AddAttribute( AttributeConstruction.Create( typeof(AspectGeneratedAttribute) ) );
        }

        parameterBuilder.Freeze();

        context.AddTransformation(
            new IntroduceParameterTransformation(
                aspectLayerInstance,
                parameterBuilder.BuilderData,
                materializeOnRecord: false ) );

        // 4. Recursively pull into constructors that chain to this one. Passing an
        //    IntroduceParameterPullStrategy enables cross-project propagation via
        //    PullConstructorParameterTransitiveAspect registered inside PullConstructorParameterAdviceImpl.
        //    The default value is serialized as text; a typed `default(T)` is used so that
        //    TypedConstant.TryConvertFromExpression recognizes it as a DefaultExpressionSyntax
        //    (rather than as a DefaultLiteralExpression whose token value is the string "default").
        //    `reuseExistingParameterOfCompatibleType: true` opts into reusing any existing
        //    InitializationContext parameter on a chained constructor (added by hand or by another
        //    aspect) instead of introducing a duplicate. InitializationContext is a unique marker
        //    type, so a "match by type" rule is unambiguous here.
        var defaultValueText = $"default(global::{typeof(InitializationContext).FullName})";

        var pullStrategy = new IntroduceParameterPullStrategy(
            _defaultContextParameterName,
            initializationContextType.ToRef(),
            defaultValueText,
            reuseExistingParameterOfCompatibleType: true );

        var pullImpl = new PullConstructorParameterAdviceImpl(
            context,
            pullStrategy,
            aspectLayerInstance,
            onlyProcessDerivedTypes: false );

        pullImpl.PullConstructorParameterRecursive( parameterBuilder );

        return (constructor, _defaultContextParameterName);
    }

    /// <summary>
    /// Locates the <see cref="InitializationContext"/> parameter on the base constructor reached by
    /// <paramref name="derivedConstructor"/>'s <c>:base(...)</c> initializer and, if found, emits an
    /// <c>IsOverride=true</c> <see cref="IntroduceConstructorInitializerArgumentTransformation"/>
    /// that replaces the pulled argument with <c>context.Descend(OnConstructed)</c>.
    /// </summary>
    private static void EmitDescendOverrideForBaseInitializer(
        IConstructor derivedConstructor,
        IType initContextType,
        string contextParameterName,
        AspectLayerInstance aspectLayerInstance,
        AdviceImplementationContext context )
    {
        var baseConstructor = ((IConstructorImpl) derivedConstructor).GetBaseConstructor()?.Definition;

        if ( baseConstructor == null )
        {
            return;
        }

        var baseContextParameter = baseConstructor.Parameters.FirstOrDefault( p => p.Type.Equals( initContextType ) );

        if ( baseContextParameter == null )
        {
            // The base constructor has no InitializationContext parameter — no pulled arg to override.
            // This happens when the base type does not have the aspect applied but inherits an
            // OnConstructed from an even deeper ancestor (or hand-authored one on a type whose
            // constructors don't take a context). Nothing to do here.
            return;
        }

        var requiresParameterName = baseConstructor.Parameters.Any(
            p => p.DefaultValue != null && p.Index < baseContextParameter.Index );

        context.AddTransformation(
            new IntroduceConstructorInitializerArgumentTransformation(
                aspectLayerInstance,
                derivedConstructor.ToFullRef(),
                baseContextParameter.Index,
                baseContextParameter.Name,
                BuildDescendExpression( contextParameterName ),
                requiresParameterName,
                isOverride: true ) );
    }

    /// <summary>
    /// Builds <c>context.Descend(global::...InitializationSlot.OnConstructed)</c>, used as the argument
    /// override for the <c>:base(...)</c> initializer of a derived constructor so the base layer's epilogue
    /// skips running.
    /// </summary>
    private static ExpressionSyntax BuildDescendExpression( string contextParameterName )
        => InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactoryEx.SafeIdentifierName( contextParameterName ),
                IdentifierName( "Descend" ) ),
            ArgumentList(
                SingletonSeparatedList(
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactoryEx.CreateFullyQualifiedName( typeof(InitializationSlot).FullName! ),
                            IdentifierName( nameof(InitializationSlot.OnConstructed) ) ) ) ) ) );
}

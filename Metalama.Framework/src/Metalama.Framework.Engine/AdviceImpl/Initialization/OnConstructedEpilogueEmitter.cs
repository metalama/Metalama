// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Comparers;
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
/// when the chained constructor has the same parameter. <c>:this(...)</c> constructors are processed in a
/// second pass (epilogue + descend rewrite of the <c>:this(...)</c> argument) only when the epilogue is
/// guarded — i.e. on non-sealed, non-struct types; for sealed types and structs, the inner constructor's
/// unconditional <c>OnConstructed</c> call is sufficient and Pass 2 is skipped.
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
    /// <param name="overloadingStrategy">The strategy that decides which constructors that receive the
    /// <see cref="InitializationContext"/> parameter also receive a forwarding constructor. It is used only when
    /// <paramref name="registerPullFallback"/> is <c>true</c>, because the cross-project path introduces no parameter.</param>
    /// <param name="owningAdvice">The advice that reports the diagnostics of the forwarding constructors.</param>
    public static void EmitForType(
        INamedType targetType,
        IType initContextType,
        AspectLayerInstance aspectLayerInstance,
        AdviceImplementationContext context,
        bool registerPullFallback,
        IConstructorOverloadingStrategy? overloadingStrategy,
        Advice owningAdvice )
    {
        var guardEpilogue = !targetType.IsSealed && targetType.TypeKind != TypeKind.Struct;

        var allConstructors = targetType.Constructors
            .Where( c => !c.IsRecordCopyConstructor() )
            .ToList();

        // Pass 1: Process non-:this(...) constructors. This introduces the InitializationContext
        // parameter, triggers the recursive pull (which propagates the parameter to :this(...)
        // constructors), emits the epilogue, and emits the descend override for :base(...).
        foreach ( var sourceCtor in allConstructors.Where( c => c.InitializerKind != ConstructorInitializerKind.This ) )
        {
            var ensured = EnsureOrFindContextParameter(
                sourceCtor,
                initContextType,
                aspectLayerInstance,
                context,
                registerPullFallback,
                overloadingStrategy,
                owningAdvice );

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

            // If the chained initializer's target has an InitializationContext parameter, replace the
            // pulled argument with `context.Descend(OnConstructed)` so the chained constructor's epilogue
            // skips. This applies to both :base(...) here and :this(...) in Pass 2 below — the helper
            // dispatches on derivedConstructor.InitializerKind.
            EmitDescendOverrideForChainedInitializer(
                targetConstructor,
                initContextType,
                contextParameterName,
                aspectLayerInstance,
                context );
        }

        // Pass 2: Process :this(...) constructors. The InitializationContext parameter was already
        // pulled to these constructors by Pass 1's recursive pull; we now emit the epilogue and
        // the descend override for the :this(...) initializer argument.
        // This is only needed when guardEpilogue is true (non-sealed, non-struct types): the guard
        // prevents double invocation when both the :this(...) constructor and its target have the
        // epilogue. For sealed types and structs, there is no guard, so the inner constructor's
        // unconditional OnConstructed call is sufficient.
        if ( !guardEpilogue )
        {
            return;
        }

        foreach ( var sourceCtor in allConstructors.Where( c => c.InitializerKind == ConstructorInitializerKind.This ) )
        {
            // A forwarding constructor created by an earlier parameter pull has the pre-mutation signature and no
            // InitializationContext parameter. It passes `default` to the mutated constructor, which runs the epilogue,
            // so the forwarder itself must not receive an epilogue.
            if ( sourceCtor.Origin.Kind == DeclarationOriginKind.Aspect
                 && !sourceCtor.Parameters.Any( p => p.Type.Equals( initContextType ) ) )
            {
                continue;
            }

            // The parameter was already introduced by the pull in Pass 1 but the constructor
            // object captured in allConstructors may not reflect it yet. Try to find an existing
            // parameter first; if not found, use the default name that the pull strategy used.
            var existing = sourceCtor.Parameters.FirstOrDefault( p => p.Type.Equals( initContextType ) );
            var contextParameterName = existing?.Name ?? _defaultContextParameterName;

            context.AddTransformation(
                new OnConstructedEpilogueTransformation(
                    aspectLayerInstance,
                    targetType.ToRef(),
                    sourceCtor.ToFullRef(),
                    contextParameterName,
                    guardEpilogue ) );

            EmitDescendOverrideForChainedInitializer(
                sourceCtor,
                initContextType,
                contextParameterName,
                aspectLayerInstance,
                context );
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
        bool registerPullFallback,
        IConstructorOverloadingStrategy? overloadingStrategy,
        Advice owningAdvice )
    {
        // 1. Look for an existing parameter of type InitializationContext on this constructor.
        // This path satisfies the subtle constraint: even if the parameter was added by another aspect
        // or by the user, the epilogue must still be added using that existing parameter's name.
        var existing = sourceConstructor.Parameters.FirstOrDefault( p => p.Type.Equals( initializationContextType ) );

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

        // 3. Create the pull strategy and the forwarding helper. The pull strategy is used in step 5 to pull the
        //    parameter into chained constructors, and by the forwarding helper to obtain the value that a
        //    forwarder passes for the parameter.
        //    The default value is serialized as text; a typed `default(T)` is used so that
        //    TypedConstant.TryConvertFromExpression recognizes it as a DefaultExpressionSyntax
        //    (rather than as a DefaultLiteralExpression whose token value is the string "default").
        //    `reuseExistingParameterOfCompatibleType: true` opts into reusing any existing
        //    InitializationContext parameter on a chained constructor (added by hand or by another
        //    aspect) instead of introducing a duplicate. InitializationContext is a unique marker
        //    type, so a "match by type" rule is unambiguous here.
        //    `requireParameterWhenForwarded: true` introduces the parameter without a default value on every
        //    constructor that receives a forwarder, so that the forwarder is the only constructor that can be
        //    called without a context. The strategy is serialized with the transitive pull aspect, so the same
        //    rule applies to derived types in dependent projects.
        var defaultValueText = $"default(global::{typeof(InitializationContext).FullName})";

        var pullStrategy = new IntroduceParameterPullStrategy(
            _defaultContextParameterName,
            initializationContextType.ToDurableRef(),
            defaultValueText,
            reuseExistingParameterOfCompatibleType: true,
            requireParameterWhenForwarded: true );

        var forwardingHelper = new ForwardingConstructorHelper(
            context,
            aspectLayerInstance,
            overloadingStrategy,
            pullStrategy,
            owningAdvice );

        // 4. Introduce the parameter. For record primaries, the linker's primary-constructor
        // materialization path (driven by LateTypeLevelTransformations.RemovePrimaryConstructor)
        // strips the primary from the record header and emits an explicit body-declared ctor.
        // Because the pull strategy sets materializeOnRecord: false, the linker also filters
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

        // The forwarding decision is made before the parameter is frozen, because a forwarded constructor
        // receives the parameter without a default value.
        var forwardingPlan = forwardingHelper.PlanForwarder( constructor, parameterBuilder );

        if ( forwardingPlan.HasForwarder )
        {
            parameterBuilder.DefaultValue = null;
        }

        parameterBuilder.Freeze();

        context.AddTransformation(
            new IntroduceParameterTransformation(
                aspectLayerInstance,
                parameterBuilder.BuilderData,
                materializeOnRecord: false ) );

        forwardingHelper.ApplyForwarder( forwardingPlan, constructor, parameterBuilder );

        // 5. Recursively pull into constructors that chain to this one. Passing an
        //    IntroduceParameterPullStrategy enables cross-project propagation via
        //    PullConstructorParameterTransitiveAspect registered inside PullConstructorParameterAdviceImpl.
        //    Passing the forwarding helper gives forwarders to the chained and derived constructors, and serializes
        //    the overloading strategy into the transitive aspect.
        var pullImpl = new PullConstructorParameterAdviceImpl(
            context,
            pullStrategy,
            aspectLayerInstance,
            onlyProcessDerivedTypes: false,
            forwardingHelper );

        pullImpl.PullConstructorParameterRecursive( parameterBuilder );

        return (constructor, _defaultContextParameterName);
    }

    /// <summary>
    /// Locates the <see cref="InitializationContext"/> parameter on the constructor reached by
    /// <paramref name="derivedConstructor"/>'s <c>:base(...)</c> or <c>:this(...)</c> initializer
    /// and, if found, emits an <c>IsOverride=true</c>
    /// <see cref="IntroduceConstructorInitializerArgumentTransformation"/> that replaces the pulled
    /// argument with <c>context.Descend(OnConstructed)</c>.
    /// </summary>
    private static void EmitDescendOverrideForChainedInitializer(
        IConstructor derivedConstructor,
        IType initContextType,
        string contextParameterName,
        AspectLayerInstance aspectLayerInstance,
        AdviceImplementationContext context )
    {
        var chainedConstructor = ((IConstructorImpl) derivedConstructor).GetBaseConstructor()?.Definition;

        if ( chainedConstructor == null )
        {
            return;
        }

        var chainedContextParameter = chainedConstructor.Parameters.FirstOrDefault( p => p.Type.Equals( initContextType ) );

        if ( chainedContextParameter == null
             && FindConstructorForwardedBy( chainedConstructor, initContextType ) is { } forwardedConstructor )
        {
            // The initializer is bound to a forwarding constructor, typically `: base()` bound to the parameterless
            // forwarder of a base type. The parameter pull has rewritten the call to target the forwarded constructor
            // (see PullConstructorParameterAdviceImpl.IsChainedCall), so the argument to override is the
            // InitializationContext parameter of the forwarded constructor.
            chainedConstructor = forwardedConstructor;
            chainedContextParameter = forwardedConstructor.Parameters.First( p => p.Type.Equals( initContextType ) );
        }

        int parameterIndex;
        string parameterName;

        if ( chainedContextParameter != null )
        {
            parameterIndex = chainedContextParameter.Index;
            parameterName = chainedContextParameter.Name;
        }
        else if ( derivedConstructor.InitializerKind == ConstructorInitializerKind.This )
        {
            // For :this(...) chains within the same type, the chained constructor's
            // InitializationContext parameter was introduced by the parameter pull in
            // Pass 1 and appended at the end. Since it isn't visible in the source model
            // yet, compute the index from the original parameter count.
            parameterIndex = chainedConstructor.Parameters.Count;
            parameterName = _defaultContextParameterName;
        }
        else
        {
            // The chained constructor has no InitializationContext parameter — no pulled arg to override.
            // This happens when the base type does not have the aspect applied but inherits an
            // OnConstructed from an even deeper ancestor (or hand-authored one on a type whose
            // constructors don't take a context). Nothing to do here.
            return;
        }

        var requiresParameterName = chainedConstructor.Parameters.Any( p => p.DefaultValue != null && p.Index < parameterIndex );

        context.AddTransformation(
            new IntroduceConstructorInitializerArgumentTransformation(
                aspectLayerInstance,
                derivedConstructor.ToFullRef(),
                parameterIndex,
                parameterName,
                BuildDescendExpression( contextParameterName ),
                requiresParameterName,
                isOverride: true ) );
    }

    /// <summary>
    /// Returns the constructor of the same type that <paramref name="forwarder"/> forwards to, when
    /// <paramref name="forwarder"/> is a forwarding constructor that keeps the signature of a constructor that received
    /// an <see cref="InitializationContext"/> parameter. Returns <c>null</c> otherwise.
    /// </summary>
    /// <remarks>
    /// The forwarded constructor is the constructor whose parameters, without the <see cref="InitializationContext"/>
    /// parameter, have the same types and the same ref kinds as the parameters of <paramref name="forwarder"/>. This
    /// rule is unambiguous because <see cref="InitializationContext"/> is used only as a marker type, and C# does not
    /// allow two constructors with the same signature. The rule does not depend on the <see cref="AspectGeneratedAttribute"/>,
    /// which is added only to the parameters of constructors that can be chained from another assembly.
    /// </remarks>
    private static IConstructor? FindConstructorForwardedBy( IConstructor forwarder, IType initContextType )
    {
        foreach ( var candidate in forwarder.DeclaringType.Constructors )
        {
            var candidateParameters = candidate.Parameters.Where( p => !p.Type.Equals( initContextType ) ).ToList();

            if ( candidateParameters.Count != forwarder.Parameters.Count || candidateParameters.Count == candidate.Parameters.Count )
            {
                continue;
            }

            var match = true;

            for ( var i = 0; i < candidateParameters.Count; i++ )
            {
                if ( !SignatureTypeComparer.Instance.Equals( candidateParameters[i].Type, forwarder.Parameters[i].Type )
                     || candidateParameters[i].RefKind != forwarder.Parameters[i].RefKind )
                {
                    match = false;

                    break;
                }
            }

            if ( match )
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Builds <c>context.Descend(global::...InitializationSlot.OnConstructed)</c>, used as the argument
    /// override for the <c>:base(...)</c> or <c>:this(...)</c> initializer so the chained constructor's
    /// epilogue skips running.
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
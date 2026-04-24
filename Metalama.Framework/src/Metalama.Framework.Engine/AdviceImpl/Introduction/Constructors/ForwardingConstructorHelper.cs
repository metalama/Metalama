// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

/// <summary>
/// Helper that implements the "forwarding constructor" side-effect of <c>IntroduceParameter</c>.
/// When the current advice mutates a constructor and the caller-supplied <see cref="IConstructorOverloadingStrategy"/>
/// asks for a forwarder, this helper creates (or extends) a compile-time stub that keeps the pre-mutation
/// signature of the constructor callable and forwards a strategy-supplied expression as the value of the
/// newly introduced parameter.
/// </summary>
internal sealed class ForwardingConstructorHelper
{
    private readonly AdviceImplementationContext _context;
    private readonly AspectLayerInstance _aspectLayerInstance;
    private readonly IPullStrategy? _pullStrategy;
    private readonly Advice _owningAdvice;

    public ForwardingConstructorHelper(
        AdviceImplementationContext context,
        AspectLayerInstance aspectLayerInstance,
        IConstructorOverloadingStrategy? overloadingStrategy,
        IPullStrategy? pullStrategy,
        Advice owningAdvice )
    {
        this._context = context;
        this._aspectLayerInstance = aspectLayerInstance;
        this.OverloadingStrategy = overloadingStrategy;
        this._pullStrategy = pullStrategy;
        this._owningAdvice = owningAdvice;
    }

    public IConstructorOverloadingStrategy? OverloadingStrategy { get; }

    public bool IsEnabled => this.OverloadingStrategy is not null;

    /// <summary>
    /// Invoked after <paramref name="mutatedConstructor"/> has received <paramref name="introducedParameter"/>.
    /// If the overloading strategy asks for a forwarder, this method creates or extends one.
    /// </summary>
    public void ApplyForwarderIfNeeded( IConstructor mutatedConstructor, IParameter introducedParameter )
    {
        if ( !this.IsEnabled || this._pullStrategy is null )
        {
            return;
        }

        // Compute pre-mutation parameters: the ones originating from source (aspect-introduced parameters
        // are excluded).
        var preMutationParams = mutatedConstructor.Parameters
            .Where( p => p.Origin.Kind == DeclarationOriginKind.Source )
            .ToList();

        // An earlier aspect may have created a forwarder matching the pre-mutation signature. If so, it
        // must receive the newly introduced parameter regardless of whether the current overloading
        // strategy would create a forwarder itself — otherwise its :this(...) call would no longer satisfy
        // the mutated constructor's required parameters.
        var existingForwarder = FindExistingForwarder( mutatedConstructor, preMutationParams );

        if ( existingForwarder is not null )
        {
            this.AppendForwarderArgument( existingForwarder, introducedParameter );

            return;
        }

        var action = this.GetConstructorOverloadingAction( mutatedConstructor, introducedParameter );

        if ( action.Kind == ConstructorOverloadingActionKind.None )
        {
            return;
        }

        this.CreateForwarder( mutatedConstructor, preMutationParams, introducedParameter, action );
    }

    private ConstructorOverloadingAction GetConstructorOverloadingAction( IConstructor mutatedConstructor, IParameter introducedParameter )
    {
        using ( UserCodeExecutionContext.WithContext(
                   this._context.ServiceProvider,
                   this._context.MutableCompilation,
                   UserCodeDescription.Create( "executing GetConstructorOverloadingAction" ) ) )
        {
            return this.OverloadingStrategy!.GetConstructorOverloadingAction( mutatedConstructor, introducedParameter );
        }
    }

    private static IConstructor? FindExistingForwarder( IConstructor mutatedConstructor, IReadOnlyList<IParameter> preMutationParams )
    {
        // A forwarder's signature is exactly the pre-mutation signature of the mutated constructor.
        // C# guarantees no other source constructor can share that signature (duplicate-signature rule),
        // so a same-type ctor that is not the mutated one and whose parameter types/refkinds match
        // preMutationParams can only be the forwarder. The mutated constructor has a strictly larger
        // parameter count, so it is rejected by the count comparison.
        foreach ( var ctor in mutatedConstructor.DeclaringType.Constructors )
        {
            if ( ctor.Parameters.Count != preMutationParams.Count )
            {
                continue;
            }

            var match = true;

            for ( var i = 0; i < preMutationParams.Count; i++ )
            {
                if ( !SignatureTypeComparer.Instance.Equals( ctor.Parameters[i].Type, preMutationParams[i].Type )
                     || ctor.Parameters[i].RefKind != preMutationParams[i].RefKind )
                {
                    match = false;

                    break;
                }
            }

            if ( match && !ctor.Equals( mutatedConstructor ) )
            {
                return ctor;
            }
        }

        return null;
    }

    private void CreateForwarder(
        IConstructor mutatedConstructor,
        IReadOnlyList<IParameter> preMutationParams,
        IParameter introducedParameter,
        ConstructorOverloadingAction action )
    {
        var forwarderBuilder = new ConstructorBuilder( this._aspectLayerInstance, mutatedConstructor.DeclaringType )
        {
            Accessibility = mutatedConstructor.Accessibility,
            IsStatic = false,
            InitializerKind = ConstructorInitializerKind.This,

            // The original source constructor is still physically present in the user's source at design time,
            // so the source generator must not emit the forwarding constructor (it would collide). The forwarder
            // is only needed at compile time, when the source constructor has been mutated with the introduced parameter.
            IsDesignTimeObservableOverride = false
        };

        // When the strategy asks for [Obsolete], emit it before copying source attributes so we can skip a
        // source [Obsolete] below (strategy wins).
        if ( action.Kind == ConstructorOverloadingActionKind.ForwardAndMarkObsolete )
        {
            var obsoleteArguments = action.ObsoleteMessage is null && !action.ObsoleteIsError
                ? Array.Empty<object?>()
                : new object?[] { action.ObsoleteMessage, action.ObsoleteIsError };

            forwarderBuilder.AddAttribute( AttributeConstruction.Create( typeof(ObsoleteAttribute), obsoleteArguments ) );
        }

        // Copy source custom attributes from the mutated constructor (e.g. [Obsolete],
        // [EditorBrowsable], [Conditional]) so callers of the forwarding constructor see the same metadata
        // that would have been visible on the pre-mutation source constructor. When the strategy
        // is ForwardAndMarkObsolete, skip any source [Obsolete] — the strategy's directive
        // wins and emitting both would produce a duplicate-attribute compile error.
        forwarderBuilder.AddAttributes(
            mutatedConstructor.Attributes
                .Where(
                    a => a.Origin.Kind == DeclarationOriginKind.Source
                         && !(action.Kind == ConstructorOverloadingActionKind.ForwardAndMarkObsolete
                              && a.Type.FullName == "System.ObsoleteAttribute") ) );

        // Copy the pre-mutation parameter list (source parameters only).
        foreach ( var sourceParameter in preMutationParams )
        {
            forwarderBuilder.AddParameter(
                sourceParameter.Name,
                sourceParameter.Type,
                sourceParameter.RefKind,
                sourceParameter.DefaultValue );
        }

        // Forward the pre-mutation parameters by name to the :this(...) call.
        foreach ( var sourceParameter in preMutationParams )
        {
            forwarderBuilder.AddInitializerArgument(
                new SyntaxUserExpression(
                    SyntaxFactoryEx.SafeIdentifierName( sourceParameter.Name ),
                    sourceParameter.Type ),
                sourceParameter.Name );
        }

        // Forward any aspect-introduced parameters that predate the current advice.
        // These are already present on the mutated constructor's parameter list.
        // The newly introduced parameter is excluded from this loop because the
        // advice transformation that adds it has not been flushed yet — we forward
        // it explicitly afterwards.
        foreach ( var aspectParameter in mutatedConstructor.Parameters
                     .Where( p => p.Origin.Kind == DeclarationOriginKind.Aspect && p.Name != introducedParameter.Name ) )
        {
            var priorForwardingExpression = this.GetForwardingExpression( aspectParameter, forwarderBuilder );

            if ( priorForwardingExpression is null )
            {
                return;
            }

            forwarderBuilder.AddInitializerArgument( priorForwardingExpression, aspectParameter.Name );
        }

        // Forward the newly introduced parameter.
        var forwardingExpression = this.GetForwardingExpression( introducedParameter, forwarderBuilder );

        if ( forwardingExpression is null )
        {
            return;
        }

        forwarderBuilder.AddInitializerArgument( forwardingExpression, introducedParameter.Name );

        forwarderBuilder.Freeze();
        this._context.AddTransformation( forwarderBuilder.CreateTransformation() );
    }

    private void AppendForwarderArgument( IConstructor existingForwarder, IParameter introducedParameter )
    {
        // Ask the strategy for the forwarding expression for the newly introduced parameter.
        var forwardingExpression = this.GetForwardingExpression( introducedParameter, existingForwarder );

        if ( forwardingExpression is null )
        {
            return;
        }

        // Convert the expression to a syntax node for the argument. Use the declaring type's primary
        // declaration syntax for the generation context — the forwarder itself is a builder with no syntax.
        var syntaxGenerationOptions = this._context.ServiceProvider.GetRequiredService<SyntaxGenerationOptions>();

        var forwarderSyntaxGenerationContext = this._context.MutableCompilation.CompilationContext.GetSyntaxGenerationContext(
            syntaxGenerationOptions,
            existingForwarder.DeclaringType.GetPrimaryDeclarationSyntax().AssertNotNull() );

        var expressionSyntax = forwardingExpression.ToExpressionSyntax(
            new SyntaxSerializationContext(
                this._context.MutableCompilation,
                forwarderSyntaxGenerationContext,
                null,
                existingForwarder.DeclaringType ) );

        this._context.AddTransformation(
            new IntroduceConstructorInitializerArgumentTransformation(
                this._aspectLayerInstance,
                existingForwarder.ToFullRef(),
                introducedParameter.Index,
                introducedParameter.Name,
                expressionSyntax,
                requiresParameterName: true ) );
    }

    private IExpression? GetForwardingExpression( IParameter introducedParameter, IConstructor forwarderTarget )
    {
        PullAction pullAction;

        using ( UserCodeExecutionContext.WithContext(
                   this._context.ServiceProvider,
                   this._context.MutableCompilation,
                   UserCodeDescription.Create( "evaluating the pull strategy for a forwarding constructor" ) ) )
        {
            pullAction = this._pullStrategy!.GetPullAction( introducedParameter, forwarderTarget );
        }

        switch ( pullAction.Kind )
        {
            case PullActionKind.UseExpression:
                return pullAction.Expression;

            case PullActionKind.AppendParameterAndPull:
                // A forwarder has a fixed signature (the pre-mutation signature), so we cannot append a
                // new parameter to it. Prefer the strategy-supplied ForwarderExpression, then the
                // parameter's declared default, and fall back to default(T)! so the forwarder always
                // compiles — the null-forgiving operator silences nullable warnings for reference types
                // and is a no-op for value types.
                if ( pullAction.ForwarderExpression != null )
                {
                    return pullAction.ForwarderExpression;
                }

                if ( pullAction.ParameterDefaultValue != null )
                {
                    return pullAction.ParameterDefaultValue;
                }

                return this.CreateDefaultForwarderExpression( introducedParameter, forwarderTarget );

            default:
                // DoNotPull, ReplaceParameterTypeAndPull are invalid for forwarders.
                this._context.Diagnostics.Report(
                    AdviceDiagnosticDescriptors.InvalidPullActionForForwardingConstructor.CreateRoslynDiagnostic(
                        forwarderTarget.GetDiagnosticLocation(),
                        (this._owningAdvice.AspectInstance.AspectClass.ShortName, introducedParameter, forwarderTarget,
                         pullAction.Kind.ToString()),
                        this._owningAdvice ) );

                return null;
        }
    }

    private IExpression CreateDefaultForwarderExpression( IParameter introducedParameter, IConstructor forwarderTarget )
    {
        var syntaxGenerationOptions = this._context.ServiceProvider.GetRequiredService<SyntaxGenerationOptions>();

        var syntaxGenerationContext = this._context.MutableCompilation.CompilationContext.GetSyntaxGenerationContext(
            syntaxGenerationOptions,
            forwarderTarget.DeclaringType.GetPrimaryDeclarationSyntax().AssertNotNull() );

        var defaultSyntax = syntaxGenerationContext.SyntaxGenerator.DefaultExpression( introducedParameter.Type );

        // Append the null-forgiving operator only when the parameter type may fire a nullable warning
        // on a default value: excluded for value types and for types already known to be nullable
        // (where null is a legal value). For unconstrained generic parameters we keep the operator
        // defensively because nullability cannot be determined statically.
        if ( introducedParameter.Type.IsReferenceType != false && introducedParameter.Type.IsNullable != true )
        {
            defaultSyntax = PostfixUnaryExpression( SyntaxKind.SuppressNullableWarningExpression, defaultSyntax );
        }

        return new SyntaxUserExpression( defaultSyntax, introducedParameter.Type );
    }
}
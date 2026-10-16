// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Source;
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
internal sealed partial class ForwardingConstructorHelper
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
    /// Gets a value indicating whether a parameter introduced on a constructor that receives a forwarder must be
    /// introduced without a default value. The value comes from
    /// <see cref="IntroduceParameterPullStrategy.RequireParameterWhenForwarded"/>.
    /// </summary>
    public bool RequireParameterWhenForwarded => this._pullStrategy is IntroduceParameterPullStrategy { RequireParameterWhenForwarded: true };

    /// <summary>
    /// Invoked after <paramref name="mutatedConstructor"/> has received <paramref name="introducedParameter"/>.
    /// If the overloading strategy asks for a forwarder, this method creates or extends one.
    /// </summary>
    public void ApplyForwarderIfNeeded( IConstructor mutatedConstructor, IParameter introducedParameter )
        => this.ApplyForwarder( this.PlanForwarder( mutatedConstructor, introducedParameter ), mutatedConstructor, introducedParameter );

    /// <summary>
    /// Determines whether <paramref name="mutatedConstructor"/> will receive a forwarder when it receives
    /// <paramref name="introducedParameter"/>, without adding any transformation.
    /// </summary>
    /// <remarks>
    /// Call this method before the <see cref="ParameterBuilder"/> of <paramref name="introducedParameter"/> is frozen
    /// when the default value of the parameter depends on the result (see <see cref="RequireParameterWhenForwarded"/>),
    /// then pass the result to <see cref="ApplyForwarder"/>. The overloading strategy is evaluated only once.
    /// </remarks>
    public ForwardingPlan PlanForwarder( IConstructor mutatedConstructor, IParameter introducedParameter )
    {
        if ( !this.IsEnabled || this._pullStrategy is null )
        {
            return default;
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

        // The new parameter is appended after the existing parameters. When one of them is optional, the new parameter
        // cannot be required (CS1737), and a new forwarder with the pre-mutation signature would make the calls that omit
        // the optional arguments ambiguous (CS0121). When the parameter is meant to be required on forwarded constructors,
        // such a constructor therefore receives no new forwarder and keeps an optional parameter.
        var hasOptionalParameter = mutatedConstructor.Parameters.Any( p => p.DefaultValue != null );

        if ( existingForwarder is not null )
        {
            return new ForwardingPlan( existingForwarder, default, preMutationParams, hasOptionalParameter );
        }

        if ( hasOptionalParameter && this.RequireParameterWhenForwarded )
        {
            return new ForwardingPlan( null, default, preMutationParams, hasOptionalParameter );
        }

        var action = this.GetConstructorOverloadingAction( mutatedConstructor, introducedParameter );

        return new ForwardingPlan( null, action, preMutationParams, hasOptionalParameter );
    }

    /// <summary>
    /// Creates or extends the forwarder described by <paramref name="plan"/>, which was returned by
    /// <see cref="PlanForwarder"/> for the same constructor and parameter.
    /// </summary>
    public void ApplyForwarder( in ForwardingPlan plan, IConstructor mutatedConstructor, IParameter introducedParameter )
    {
        if ( plan.ExistingForwarder is not null )
        {
            this.AppendForwarderArgument( plan.ExistingForwarder, introducedParameter );
        }
        else if ( plan.Action.Kind != ConstructorOverloadingActionKind.None )
        {
            this.CreateForwarder( mutatedConstructor, plan.PreMutationParameters!, introducedParameter, plan.Action );
        }
    }

    private ConstructorOverloadingAction GetConstructorOverloadingAction( IConstructor mutatedConstructor, IParameter introducedParameter )
    {
        // A constructor that replaces an implicit constructor is an aspect-generated declaration, but it stands for
        // the implicit source constructor. Give the source constructor to the strategy, so that strategies that only
        // forward source constructors also preserve an implicit parameterless constructor.
        // Structs are excluded: a struct always has a parameterless constructor, which initializes the instance to its
        // default value, so the implicit constructor of a struct does not need a forwarder. An explicit parameterless
        // struct constructor would also change the meaning of `new S()` and require C# 10.
        var logicalConstructor =
            mutatedConstructor.DeclaringType.TypeKind != TypeKind.Struct
            && GetReplacedImplicitConstructor( mutatedConstructor ) is { } replacedImplicitConstructor
                ? replacedImplicitConstructor
                : mutatedConstructor;

        using ( UserCodeExecutionContext.WithContext(
                   this._context.ServiceProvider,
                   this._context.MutableCompilation,
                   UserCodeDescription.Create( "executing GetConstructorOverloadingAction" ) ) )
        {
            return this.OverloadingStrategy!.GetConstructorOverloadingAction( logicalConstructor, introducedParameter );
        }
    }

    /// <summary>
    /// Returns the implicit source constructor that <paramref name="constructor"/> replaces, or <c>null</c> when
    /// <paramref name="constructor"/> does not replace an implicit constructor.
    /// </summary>
    /// <remarks>
    /// An implicit constructor is replaced by a <see cref="ConstructorBuilder"/> when an advice must add code or parameters
    /// to it. When an earlier advice has replaced it (for instance a <c>BeforeInstanceConstructor</c> initializer), the
    /// current advice receives an <see cref="IntroducedConstructor"/> instead, whose builder data references the implicit
    /// constructor. The reference resolves to the replacement, so the implicit constructor is created from its symbol.
    /// </remarks>
    private static IConstructor? GetReplacedImplicitConstructor( IConstructor constructor )
    {
        switch ( constructor )
        {
            case ConstructorBuilder { ReplacedImplicitConstructor: { } replaced }:
                return replaced is SourceConstructor ? replaced : GetReplacedImplicitConstructor( replaced );

            case IntroducedConstructor { BuilderData: ConstructorBuilderData { ReplacedImplicitConstructor: { } replacedRef } } introduced:
                return replacedRef is ISymbolRef { Symbol: Microsoft.CodeAnalysis.IMethodSymbol implicitConstructorSymbol }
                    ? new SourceConstructor( implicitConstructorSymbol, introduced.GetCompilationModel(), null )
                    : null;

            default:
                return null;
        }
    }

    private static IConstructor? FindExistingForwarder( IConstructor mutatedConstructor, IReadOnlyList<IParameter> preMutationParams )
    {
        // A forwarder's signature is exactly the pre-mutation signature of the mutated constructor.
        // C# guarantees no other source constructor can share that signature (duplicate-signature rule),
        // so a same-type ctor that is not the mutated one and whose parameter types/refkinds match
        // preMutationParams can only be the forwarder. The mutated constructor has a strictly larger
        // parameter count, so it is rejected by the count comparison.
        // A forwarder is never implicitly declared. When the mutated constructor replaces an implicit constructor,
        // the implicit constructor is still in the collection of the current compilation and has the
        // pre-mutation signature, but it is the mutated constructor itself and not a forwarder.
        foreach ( var ctor in mutatedConstructor.DeclaringType.Constructors )
        {
            if ( ctor.Parameters.Count != preMutationParams.Count || ctor.IsImplicitlyDeclared )
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

            // An explicit source constructor is still physically present in the user's source at design time,
            // so the source generator must not emit the forwarding constructor (it would collide). The forwarder
            // is only needed at compile time, when the source constructor has been mutated with the introduced parameter.
            // An implicit constructor is different: the design-time source generator declares the mutated constructor
            // in a partial type, so the compiler no longer declares the implicit constructor. The forwarder must then
            // be visible at design time, otherwise the parameterless constructor would disappear from the design-time
            // compilation. A null value keeps the default behavior of introduced constructors.
            IsDesignTimeObservableOverride = GetReplacedImplicitConstructor( mutatedConstructor ) != null ? null : false
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
        // The pull strategy of the current advice is designed for the parameter that this advice introduces. Another
        // advice may have introduced the earlier parameter with a different type, for instance a dependency next to an
        // InitializationContext. The expression of the current strategy is used only when its type is convertible to the
        // type of the earlier parameter. Otherwise, an optional parameter receives no argument, so that its own default
        // value applies, and a required parameter receives the default value of its type.
        foreach ( var aspectParameter in mutatedConstructor.Parameters
                     .Where( p => p.Origin.Kind == DeclarationOriginKind.Aspect && p.Name != introducedParameter.Name ) )
        {
            var priorForwardingExpression = this.GetForwardingExpression( aspectParameter, forwarderBuilder );

            if ( priorForwardingExpression is null )
            {
                return;
            }

            if ( !priorForwardingExpression.Type.IsConvertibleTo( aspectParameter.Type ) )
            {
                if ( aspectParameter.DefaultValue != null )
                {
                    continue;
                }

                priorForwardingExpression = this.CreateDefaultForwarderExpression( aspectParameter, forwarderBuilder );
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
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed class PullConstructorParameterAdviceImpl
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly AspectLayerInstance _aspectLayerInstance;
    private readonly bool _onlyProcessDerivedTypes;
    private readonly AdviceImplementationContext _context;
    private readonly ForwardingConstructorHelper? _forwardingHelper;

    public PullConstructorParameterAdviceImpl(
        AdviceImplementationContext context,
        IPullStrategy? pullStrategy,
        AspectLayerInstance aspectLayerInstance,
        bool onlyProcessDerivedTypes,
        ForwardingConstructorHelper? forwardingHelper = null )
    {
        this._context = context;
        this._pullStrategy = pullStrategy;
        this._aspectLayerInstance = aspectLayerInstance;
        this._onlyProcessDerivedTypes = onlyProcessDerivedTypes;
        this._forwardingHelper = forwardingHelper;
    }

    private ProjectServiceProvider ServiceProvider => this._context.ServiceProvider;

    private CompilationModel Compilation => this._context.MutableCompilation;

    private void AddTransformation( ITransformation transformation ) => this._context.AddTransformation( transformation );

    /// <summary>
    /// Returns a parameter name that does not collide with any existing parameter in <paramref name="member"/>.
    /// If <paramref name="desiredName"/> is free, it is returned unchanged; otherwise a numeric suffix is appended.
    /// </summary>
    internal static string GetUniqueParameterName( IHasParameters member, string desiredName )
    {
        var existingNames = new HashSet<string>( member.Parameters.SelectAsArray( p => p.Name ) );

        if ( !existingNames.Contains( desiredName ) )
        {
            return desiredName;
        }

        for ( var i = 1; ; i++ )
        {
            var candidate = desiredName + i;

            if ( !existingNames.Contains( candidate ) )
            {
                return candidate;
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="ParameterBuilder"/> that replaces an existing introduced parameter's type,
    /// copies all custom attributes from the existing parameter, emits a <see cref="ReplaceParameterTransformation"/>,
    /// and returns the frozen builder.
    /// </summary>
    internal static ParameterBuilder CreateReplacementParameter(
        AdviceImplementationContext context,
        AspectLayerInstance aspectLayerInstance,
        IConstructor constructor,
        IParameter existingParameter,
        IType newType,
        TypedConstant? defaultValue,
        Action<ParameterBuilder>? buildAction = null )
    {
        var builder = new ParameterBuilder(
            constructor,
            existingParameter.Index,
            existingParameter.Name,
            newType,
            existingParameter.RefKind,
            aspectLayerInstance ) { DefaultValue = defaultValue };

        // Copy all custom attributes from the existing parameter.
        builder.AddAttributes( existingParameter.Attributes );

        buildAction?.Invoke( builder );

        builder.Freeze();

        context.AddTransformation(
            new ReplaceParameterTransformation(
                aspectLayerInstance,
                builder.BuilderData,
                existingParameter.Index ) );

        return builder;
    }

    /// <summary>
    /// Determines whether <paramref name="child"/>'s <c>: base(...)</c>/<c>: this(...)</c> call targets <paramref name="target"/>,
    /// either directly or through a forwarding constructor. When the base constructor lives in a
    /// referenced project, Roslyn may resolve the chain call to a forwarding constructor emitted into the dependency's IL (arity match)
    /// rather than the mutated constructor; we "see through" such forwarders by matching their parameter prefix against
    /// <paramref name="target"/>'s parameters, since the framework only ever appends new parameters to mutated constructors.
    /// </summary>
    private static bool IsChainedCall( IConstructor child, IConstructor target )
    {
        var resolved = ((IConstructorImpl) child).GetBaseConstructor()?.Definition;

        if ( resolved is null )
        {
            return false;
        }

        if ( resolved.Equals( target ) )
        {
            return true;
        }

        // When the recursive pull is processing a derived type whose base constructor was
        // just materialized in the current compilation (typical of multi-level cross-project pull:
        // Grandchild -> Child -> external BaseClass), `target` is the in-memory ConstructorBuilder
        // that replaced an implicit constructor on the same type, while `resolved` is still the
        // original implicit constructor that Roslyn resolved Grandchild's `:base()` against.
        // Treat the builder and its replaced constructor as the same logical constructor.
        if ( target is ConstructorBuilder { ReplacedImplicitConstructor: { } replacedImplicit }
             && resolved.Equals( replacedImplicit ) )
        {
            return true;
        }

        // Roslyn's SemanticModel resolves `: base(id)` (or `: this(id)`) to whichever
        // ctor matches the source arity in IL. In cross-project scenarios that means
        // it resolves to the forwarding ctor emitted by the aspect in
        // project A, not to the mutated ctor. Treat that as a match for `target` when
        // the resolved ctor is a forwarding constructor in the same
        // declaring type whose parameters are a type+refkind prefix of `target`.
        if ( !resolved.IsSourceCompatibilityConstructor() )
        {
            return false;
        }

        if ( !SignatureTypeComparer.Instance.Equals( resolved.DeclaringType, target.DeclaringType ) )
        {
            return false;
        }

        if ( resolved.Parameters.Count >= target.Parameters.Count )
        {
            return false;
        }

        for ( var i = 0; i < resolved.Parameters.Count; i++ )
        {
            if ( !SignatureTypeComparer.Instance.Equals( resolved.Parameters[i].Type, target.Parameters[i].Type )
                 || resolved.Parameters[i].RefKind != target.Parameters[i].RefKind )
            {
                return false;
            }
        }

        return true;
    }

    public void PullConstructorParameterRecursive( IParameter baseParameter )
    {
        var baseConstructor = (IConstructor) baseParameter.DeclaringMember.AssertNotNull();
        var syntaxGenerationOptions = this.ServiceProvider.GetRequiredService<SyntaxGenerationOptions>();

        if ( !this._onlyProcessDerivedTypes )
        {
            // Process the current type.
            ProcessType(
                baseConstructor.DeclaringType.Constructors
                    .Where( c => c.InitializerKind == ConstructorInitializerKind.This ) );

            // Register a transitive aspect for the current type.
            if ( this._pullStrategy is not null && this._pullStrategy is not LegacyPullStrategy && baseConstructor.CanBeChainedFromOutsideAssembly() )
            {
                var transitiveAspect = new PullConstructorParameterTransitiveAspect(
                    this._pullStrategy,
                    baseParameter.ToRef(),
                    this._context.AspectOrder,
                    this._forwardingHelper?.OverloadingStrategy );

                this._context.AddTransitiveAspect(
                    new TransitiveAspectInstance(
                        transitiveAspect,
                        baseParameter.DeclaringMember.DeclaringType.ToRef(),
                        baseParameter.DeclaringMember.DeclaringType.Depth,
                        (IAspectClassImpl) this._context.AspectClassResolver.GetAspectClass( typeof(PullConstructorParameterTransitiveAspect) ),
                        this._aspectLayerInstance.AspectInstance.AspectState,
                        this._aspectLayerInstance.AspectInstance.PredecessorDegree + 1,
                        baseParameter.GetPrimarySyntaxTree() ) );
            }
        }

        // Process derived types.
        foreach ( var derivedType in this.Compilation.GetDerivedTypes( baseConstructor.DeclaringType, DerivedTypesOptions.DirectOnly ) )
        {
            ProcessType( derivedType.Constructors.Where( c => c.InitializerKind != ConstructorInitializerKind.This && !c.IsRecordCopyConstructor() ) );
        }

        return;

        void ProcessType( IEnumerable<IConstructor> potentialConstructors )
        {
            var chainedConstructors =
                potentialConstructors.Where( c => IsChainedCall( c, baseConstructor ) );

            // Process all of these constructors.
            foreach ( var chainedConstructor in chainedConstructors )
            {
                var chainedSyntaxGenerationContext = this.Compilation.CompilationContext.GetSyntaxGenerationContext(
                    syntaxGenerationOptions,
                    chainedConstructor.GetPrimaryDeclarationSyntax()
                    ?? chainedConstructor.DeclaringType.GetPrimaryDeclarationSyntax().AssertNotNull() );

                PullAction pullParameterAction;

                if ( this._pullStrategy != null )
                {
                    using ( UserCodeExecutionContext.WithContext(
                               this.ServiceProvider,
                               this.Compilation,
                               UserCodeDescription.Create( "evaluating the pull strategy for {0}", this ) ) )
                    {
                        // Ask the IPullStrategy what to do.
                        pullParameterAction = this._pullStrategy.GetPullAction( baseParameter, chainedConstructor );
                    }
                }
                else
                {
                    pullParameterAction = PullAction.None;
                }

                // If we have an implicit constructor, make it explicit.
                var initializedChainedConstructor = chainedConstructor;

                if ( chainedConstructor.IsImplicitInstanceConstructor() )
                {
                    var derivedConstructorBuilder = new ConstructorBuilder( this._aspectLayerInstance, chainedConstructor );

                    derivedConstructorBuilder.Freeze();
                    this.AddTransformation( derivedConstructorBuilder.CreateTransformation() );
                    initializedChainedConstructor = derivedConstructorBuilder;
                }

                // Execute the strategy.
                ExpressionSyntax parameterValue;

                switch ( pullParameterAction.Kind )
                {
                    case PullActionKind.DoNotPull:
                        // We do not add a new argument and rely on the optional value.
                        continue;

                    case PullActionKind.UseExpression:
                        // If the chaining constructor already has an aspect-introduced parameter matching
                        // the base parameter, forward that parameter instead of using the pull expression.
                        // This handles the case where IntroduceParameter was called on the chaining constructor
                        // before the base constructor (reverse processing order).
                        var existingIntroducedParam = chainedConstructor.Parameters.FirstOrDefault(
                            p => p.Name == baseParameter.Name && p.Origin.Kind == DeclarationOriginKind.Aspect );

                        if ( existingIntroducedParam != null )
                        {
                            parameterValue = SyntaxFactoryEx.SafeIdentifierName( existingIntroducedParam.Name );
                        }
                        else
                        {
                            parameterValue =
                                pullParameterAction.Expression.AssertNotNull()
                                    .ToExpressionSyntax(
                                        new SyntaxSerializationContext(
                                            this.Compilation,
                                            chainedSyntaxGenerationContext,
                                            null,
                                            chainedConstructor.DeclaringType ) );
                        }

                        break;

                    case PullActionKind.ReplaceParameterTypeAndPull:
                    {
                        // Replace the type of an existing introduced parameter with a more specific type.
                        var existingParam = pullParameterAction.ExistingParameter.AssertNotNull();
                        var newType = pullParameterAction.ParameterType.AssertNotNull();

                        var replaceParameterBuilder = CreateReplacementParameter(
                            this._context,
                            this._aspectLayerInstance,
                            initializedChainedConstructor,
                            existingParam,
                            newType,
                            TypedConstant.Default( newType ) );

                        // Use the existing parameter's name as the base call argument.
                        parameterValue = SyntaxFactoryEx.SafeIdentifierName( existingParam.Name );

                        // Recursively pull using a virtual parameter with the NEW type so that
                        // downstream GetPullAction calls see the updated type for matching.
                        this.PullConstructorParameterRecursive( replaceParameterBuilder );

                        break;
                    }

                    case PullActionKind.AppendParameterAndPull:
                        // Create a new parameter, deduplicating the name if it collides with a source-defined parameter.
                        var pullParamName = GetUniqueParameterName(
                            initializedChainedConstructor,
                            pullParameterAction.ParameterName.AssertNotNull() );

                        parameterValue = SyntaxFactoryEx.SafeIdentifierName( pullParamName );

                        TypedConstant? constant = null;

                        if ( pullParameterAction.ParameterDefaultValue != null
                             && !TypedConstant.TryConvertFromExpression( pullParameterAction.ParameterDefaultValue, out constant ) )
                        {
                            throw new AssertionFailedException( $"Cannot convert '{pullParameterAction.ParameterDefaultValue}' into a constant." );
                        }

                        var recursiveParameterBuilder = new ParameterBuilder(
                            initializedChainedConstructor,
                            initializedChainedConstructor.Parameters.Count,
                            pullParamName,
                            pullParameterAction.ParameterType.AssertNotNull(),
                            RefKind.None,
                            this._aspectLayerInstance ) { DefaultValue = constant };

                        recursiveParameterBuilder.AddAttributes( pullParameterAction.ParameterAttributes );

                        if ( initializedChainedConstructor.CanBeChainedFromOutsideAssembly() )
                        {
                            recursiveParameterBuilder.AddAttribute( AttributeConstruction.Create( typeof(AspectGeneratedAttribute) ) );
                        }

                        recursiveParameterBuilder.Freeze();

                        this.AddTransformation(
                            new IntroduceParameterTransformation(
                                this._aspectLayerInstance,
                                recursiveParameterBuilder.BuilderData,
                                pullParameterAction.MaterializeOnRecord ) );

                        var recursiveParameter = recursiveParameterBuilder;

                        // Generate a forwarding constructor preserving the chained constructor's original signature if requested.
                        this._forwardingHelper?.ApplyForwarderIfNeeded( initializedChainedConstructor, recursiveParameter );

                        // Process all constructors calling this constructor.
                        this.PullConstructorParameterRecursive( recursiveParameter );

                        break;

                    default:
                        throw new AssertionFailedException( $"Invalid value for PullActionKind: {pullParameterAction.Kind}." );
                }

                // Determine if we should qualify the argument with the parameter name.
                // We do this every time there is an optional parameter before the current parameter.
                var requiresParameterName = baseConstructor.Parameters.Any( p => p.DefaultValue != null && p.Index < baseParameter.Index );

                // Append (or override) the argument to the call to the current constructor.
                // For ReplaceParameterTypeAndPull, the original pull already added an argument at this
                // index; we override it because the expression type may no longer be compatible after
                // the target parameter's type was replaced with a more specific one.
                var isOverride = pullParameterAction.Kind == PullActionKind.ReplaceParameterTypeAndPull;

                this.AddTransformation(
                    new IntroduceConstructorInitializerArgumentTransformation(
                        this._aspectLayerInstance,
                        initializedChainedConstructor.ToFullRef(),
                        baseParameter.Index,
                        baseParameter.Name,
                        parameterValue,
                        requiresParameterName,
                        isOverride ) );
            }
        }
    }
}
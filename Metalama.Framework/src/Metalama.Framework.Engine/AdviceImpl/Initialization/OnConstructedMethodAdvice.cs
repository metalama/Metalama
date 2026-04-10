// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.RunTime;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Accessibility = Metalama.Framework.Code.Accessibility;
using RefKind = Metalama.Framework.Code.RefKind;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Implements <see cref="InitializerKind.AfterLastInstanceConstructor"/>: introduces (or reuses) a
/// <c>public virtual void OnConstructed(InitializationContext context = default)</c> method on the
/// target type, injects the template body into it, and emits a trailing <c>this.OnConstructed(context)</c>
/// call at the end of every non-<c>:this(...)</c> instance constructor (after pulling an
/// <c>InitializationContext</c> parameter through the constructor chain).
/// </summary>
/// <remarks>
/// On non-sealed, non-struct targets the epilogue call is guarded by
/// <c>if (!context.IsHandled(InitializationSlot.OnConstructed))</c> so that base
/// constructors skip the call when a derived layer will handle it. In a hierarchy where the aspect
/// is applied to both base and derived, the derived layer is emitted as an <c>override</c> that
/// chains to <c>base.OnConstructed</c>, and its <c>:base(...)</c> call passes
/// <c>context.Descend(OnConstructed)</c> (replacing the <c>context</c> argument that the
/// base layer's pull would otherwise have appended).
/// </remarks>
internal sealed class OnConstructedMethodAdvice : Advice<AddInitializerAdviceResult>
{
    private const string _methodName = "OnConstructed";

    // Default name given to the InitializationContext parameter when this advice introduces a new
    // OnConstructed method or a new constructor parameter. When the target constructor or method
    // already has an InitializationContext parameter, that parameter's existing name is preserved.
    private const string _defaultContextParameterName = "context";

    private readonly TemplateMember<IMethod> _template;
    private readonly IObjectReader? _templateArguments;
    private readonly IEnumerable<IField>? _slotFields;

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
        this._slotFields = slotFields;
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

        // Look up an inherited OnConstructed — if present, the introduced method becomes an override.
        var baseOnConstructed = FindBaseOnConstructed( targetType, initContextType );

        // If a base type exposes OnConstructed(InitializationContext), it MUST also expose a
        // constructor accepting InitializationContext so that derived `:base(...)` calls can
        // pass `context.Descend(InitializationSlot.OnConstructed)` and the base constructor can
        // skip its own OnConstructed call. Otherwise the base will always call OnConstructed
        // unconditionally and the derived override will run it a second time.
        if ( baseOnConstructed != null )
        {
            var declaringType = baseOnConstructed.DeclaringType;
            var hasContextConstructor = declaringType.Constructors.Any(
                c => c.Parameters.Any( p => p.Type.Equals( initContextType ) ) );

            if ( !hasContextConstructor )
            {
                return this.CreateFailedResult(
                    AdviceDiagnosticDescriptors.OnConstructedBaseWithoutContextConstructor.CreateRoslynDiagnostic(
                        targetType.GetDiagnosticLocation(),
                        (this.AspectInstance.AspectClass.ShortName, targetType, declaringType),
                        this ) );
            }
        }

        // Step 1: introduce (or, if the user wrote one, resolve) OnConstructed on this type.
        var onConstructedMethod = this.IntroduceOrResolveOnConstructed( targetType, initContextType, baseOnConstructed, context );

        // Step 2: bind and inject the template body into OnConstructed.
        var boundTemplate = this._template.ForInitializer( onConstructedMethod, this._templateArguments );

        context.AddTransformation(
            new InsertTemplateStatementsTransformation(
                this.AspectLayerInstance,
                targetType.ToRef(),
                onConstructedMethod.ToFullRef(),
                boundTemplate ) );

        // Step 2b: if we are introducing an override, prepend `base.OnConstructed(context.Descend(userSlots))`
        // — or plain `base.OnConstructed(context)` when there are no user slots, since Descend(default) is a no-op
        // for OnConstructed (it only normalizes Intent, which OnConstructed does not consult).
        // Uses an aggregatable transformation so that multiple peer aspects applied to the same type produce a
        // single combined base call `base.OnConstructed(context.Descend(slotA | slotB))`.
        if ( baseOnConstructed != null && onConstructedMethod.IsOverride )
        {
            var prologueContextName = onConstructedMethod.Parameters[0].Name;
            var slotFieldsList = this._slotFields?.ToList();

            context.AddTransformation(
                new OnConstructedBaseCallTransformation(
                    this.AspectLayerInstance,
                    targetType.ToRef(),
                    onConstructedMethod.ToFullRef(),
                    prologueContextName,
                    slotFieldsList ) );
        }

        // Step 3: for each non-`:this(...)` instance constructor, ensure the `context` parameter
        // and emit the epilogue call `this.OnConstructed(context);` (guarded on non-sealed/non-struct).
        var guardEpilogue = !targetType.IsSealed && targetType.TypeKind != Code.TypeKind.Struct;

        foreach ( var sourceCtor in targetType.Constructors
                     .Where( c => c.InitializerKind != ConstructorInitializerKind.This
                                  && !c.IsRecordCopyConstructor() )
                     .ToList() )
        {
            var (targetConstructor, contextParameterName) = this.EnsureContextParameter( sourceCtor, initContextType, context );

            // Early `return;` statements in the constructor body are rewritten to
            // `goto __metalama_epilogue;` by ConstructorEpilogueRewriter (invoked from
            // LinkerInjectionStep.Rewriter.ReplaceBlock) so the epilogue still fires.
            // Uses an aggregatable transformation so that multiple peer aspects produce a single
            // deduplicated epilogue call per constructor.
            context.AddTransformation(
                new OnConstructedEpilogueTransformation(
                    this.AspectLayerInstance,
                    targetType.ToRef(),
                    targetConstructor.ToFullRef(),
                    contextParameterName,
                    guardEpilogue ) );

            // If base has OnConstructed, the derived `:base(...)` call must pass
            // `context.Descend(OnConstructed)` so the base constructor's epilogue skips.
            // Base's pull already appended a plain `context` argument; replace it with an
            // IsOverride=true transformation at the same parameter index.
            if ( baseOnConstructed != null
                 && targetConstructor.InitializerKind != ConstructorInitializerKind.This )
            {
                this.EmitDescendOverrideForBaseInitializer(
                    targetConstructor,
                    initContextType,
                    contextParameterName,
                    context );
            }
        }

        return new AddInitializerAdviceResult( AdviceOutcome.Success, this.AdviceFactory );
    }

    private IMethod IntroduceOrResolveOnConstructed(
        INamedType targetType,
        IType initContextType,
        IMethod? baseOnConstructed,
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
            ReturnType = factory.GetSpecialType( Code.SpecialType.Void )
        };

        if ( baseOnConstructed != null )
        {
            // Override the inherited OnConstructed method so the derived layer chains to base.
            builder.IsOverride = true;
            builder.OverriddenMethod = baseOnConstructed;

            // An override must match the accessibility of the method it overrides.
            builder.Accessibility = baseOnConstructed.Accessibility;
        }
        else
        {
            // New method — virtual unless the type is sealed or a struct. Choose the minimum accessibility
            // required: private on sealed types and structs (no derived class can ever see it), protected
            // otherwise (so derived classes can override or call it).
            var isSealedLike = targetType.IsSealed || targetType.TypeKind == Code.TypeKind.Struct;
            builder.IsVirtual = !isSealedLike;
            builder.Accessibility = isSealedLike ? Accessibility.Private : Accessibility.Protected;
        }

        builder.AddParameter( _defaultContextParameterName, initContextType, defaultValue: TypedConstant.Default( initContextType ) );

        builder.Freeze();

        context.AddTransformation(
            new IntroduceMethodTransformation( this.AspectLayerInstance, builder.BuilderData ) );

        return builder;
    }

    /// <summary>
    /// Ensures that <paramref name="sourceConstructor"/> has an <c>InitializationContext</c> parameter.
    /// If an existing parameter of the given type is found, its name is returned. Otherwise an implicit
    /// constructor is materialized as needed, a new parameter is introduced with
    /// <c>default(InitializationContext)</c> as its default value, and the parameter is pulled recursively
    /// through <c>:this(...)</c> / <c>:base(...)</c> chains (and across project boundaries via the
    /// transitive aspect mechanism registered inside <see cref="PullConstructorParameterAdviceImpl"/>).
    /// </summary>
    /// <returns>
    /// The (possibly materialized) constructor and the name of the <c>InitializationContext</c> parameter.
    /// </returns>
    private (IConstructor Constructor, string ContextParameterName) EnsureContextParameter(
        IConstructor sourceConstructor,
        IType initializationContextType,
        AdviceImplementationContext context )
    {
        // 1. Look for an existing parameter of type InitializationContext on this constructor.
        var existing = sourceConstructor.Parameters.FirstOrDefault(
            p => p.Type.Equals( initializationContextType ) );

        if ( existing != null )
        {
            return (sourceConstructor, existing.Name);
        }

        // 2. Materialize implicit constructor to an explicit one if needed.
        var constructor = sourceConstructor;

        if ( constructor.IsImplicitInstanceConstructor() )
        {
            var constructorBuilder = new ConstructorBuilder( this.AspectLayerInstance, constructor );
            constructorBuilder.Freeze();
            context.AddTransformation( constructorBuilder.CreateTransformation() );
            constructor = constructorBuilder;
        }

        // 3. Introduce the parameter.
        var parameterBuilder = new ParameterBuilder(
            constructor,
            constructor.Parameters.Count,
            _defaultContextParameterName,
            initializationContextType,
            RefKind.None,
            this.AspectLayerInstance ) { DefaultValue = TypedConstant.Default( initializationContextType ) };

        if ( constructor.CanBeChainedFromOutsideAssembly() )
        {
            parameterBuilder.AddAttribute( AttributeConstruction.Create( typeof(AspectGeneratedAttribute) ) );
        }

        parameterBuilder.Freeze();

        context.AddTransformation(
            new IntroduceParameterTransformation( this.AspectLayerInstance, parameterBuilder.BuilderData ) );

        // 4. Recursively pull into constructors that chain to this one. Passing an
        //    IntroduceParameterPullStrategy enables cross-project propagation via
        //    PullConstructorParameterTransitiveAspect registered inside PullConstructorParameterAdviceImpl.
        //    The default value is serialized as text; a typed `default(T)` is used so that
        //    TypedConstant.TryConvertFromExpression recognizes it as a DefaultExpressionSyntax
        //    (rather than as a DefaultLiteralExpression whose token value is the string "default").
        var defaultValueText = $"default(global::{typeof(InitializationContext).FullName})";
        var pullStrategy = new IntroduceParameterPullStrategy( _defaultContextParameterName, initializationContextType.ToRef(), defaultValueText );

        var pullImpl = new PullConstructorParameterAdviceImpl(
            context,
            pullStrategy,
            this.AspectLayerInstance,
            onlyProcessDerivedTypes: false );

        pullImpl.PullConstructorParameterRecursive( parameterBuilder );

        return (constructor, _defaultContextParameterName);
    }

    /// <summary>
    /// Walks base types looking for a virtual/override <c>OnConstructed(InitializationContext)</c>
    /// method that a derived layer can override. Returns the base method if found; otherwise null.
    /// </summary>
    private static IMethod? FindBaseOnConstructed( INamedType targetType, IType initContextType )
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
                // Only overridable methods can be chained from a derived layer.
                return baseMethod.IsVirtual || baseMethod.IsOverride ? baseMethod : null;
            }
        }

        return null;
    }

    /// <summary>
    /// Locates the <see cref="InitializationContext"/> parameter on the base constructor reached by
    /// <paramref name="derivedConstructor"/>'s <c>:base(...)</c> initializer and, if found, emits an
    /// <c>IsOverride=true</c> <see cref="IntroduceConstructorInitializerArgumentTransformation"/>
    /// that replaces the pulled argument with <c>context.Descend(OnConstructed)</c>.
    /// </summary>
    private void EmitDescendOverrideForBaseInitializer(
        IConstructor derivedConstructor,
        IType initContextType,
        string contextParameterName,
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
                this.AspectLayerInstance,
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

    public override AdviceKind AdviceKind => AdviceKind.AddInitializer;

    protected override AddInitializerAdviceResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceOutcome.Error, this.AdviceFactory, diagnostics );
}

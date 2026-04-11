// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Accessibility = Metalama.Framework.Code.Accessibility;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Implements <see cref="InitializerKind.AfterLastInstanceConstructor"/>: introduces (or reuses) an
/// <c>OnConstructed(InitializationContext context = default)</c> method on the target type, injects
/// the template body into it, and emits a trailing <c>this.OnConstructed(context)</c> call at the end
/// of every non-<c>:this(...)</c> instance constructor (after pulling an <c>InitializationContext</c>
/// parameter through the constructor chain). The introduced method is <c>private</c> on sealed types
/// and structs, <c>protected virtual</c> on unsealed classes, and an <c>override</c> matching the base
/// accessibility when a base type already exposes <c>OnConstructed</c>.
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
    // OnConstructed method. When the target type already has a matching method, that method's existing
    // parameter name is preserved.
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
        // Shared with the cross-project epilogue advice (registerPullFallback: false there).
        OnConstructedEpilogueEmitter.EmitForType(
            targetType,
            initContextType,
            this.AspectLayerInstance,
            context,
            registerPullFallback: true );

        // Step 4: cross-project propagation of the epilogue obligation. The transitive aspect runs
        // in dependent projects regardless of whether the user aspect is [Inheritable], so derived
        // classes in dependent projects also get the epilogue and the :base(context.Descend(...))
        // rewrite. It runs after PullConstructorParameterTransitiveAspect (system-layer ordering),
        // and is independent from it: even if the pull is a no-op (because the constructor already
        // has an InitializationContext parameter from another source), this aspect still emits
        // the epilogue.
        if ( !targetType.IsSealed
             && targetType.TypeKind != Code.TypeKind.Struct
             && targetType.IsAccessibleFromOutsideAssembly() )
        {
            var transitiveAspect = new AddConstructorEpilogueTransitiveAspect( context.AspectOrder );

            context.AddTransitiveAspect(
                new TransitiveAspectInstance(
                    transitiveAspect,
                    targetType.ToRef(),
                    targetType.Depth,
                    (IAspectClassImpl) context.AspectClassResolver.GetAspectClass( typeof(AddConstructorEpilogueTransitiveAspect) ),
                    this.AspectLayerInstance.AspectInstance.AspectState,
                    this.AspectLayerInstance.AspectInstance.PredecessorDegree + 1,
                    targetType.GetPrimarySyntaxTree() ) );
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

    public override AdviceKind AdviceKind => AdviceKind.AddInitializer;

    protected override AddInitializerAdviceResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceOutcome.Error, this.AdviceFactory, diagnostics );
}

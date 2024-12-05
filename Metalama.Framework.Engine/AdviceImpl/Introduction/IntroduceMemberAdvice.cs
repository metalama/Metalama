// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal abstract class IntroduceMemberAdvice<TTemplate, TIntroduced, TBuilder> : IntroduceDeclarationAdvice<TIntroduced, TBuilder>
    where TTemplate : class, IMember
    where TIntroduced : class, IMember
    where TBuilder : MemberBuilder, TIntroduced
{
    private readonly IntroductionScope _scope;

    private readonly INamedType? _explicitlyImplementedInterfaceType;

    protected new INamedType TargetDeclaration => (INamedType) base.TargetDeclaration;

    protected string MemberName { get; }

    protected TemplateMember<TTemplate>? Template { get; }

    protected OverrideStrategy OverrideStrategy { get; }

    protected IntroduceMemberAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        string? explicitName,
        TemplateMember<TTemplate>? template,
        IntroductionScope scope,
        OverrideStrategy overrideStrategy,
        Action<TBuilder>? buildAction,
        INamedType? explicitlyImplementedInterfaceType )
        : base( parameters, buildAction )
    {
        var templateAttribute = (ITemplateAttribute?) template?.AdviceAttribute;
        var templateAttributeProperties = templateAttribute?.Properties;

        this.MemberName = explicitName ?? templateAttributeProperties?.Name
            ?? template?.Symbol.Name ?? throw new ArgumentNullException( nameof(explicitName) );

        this.Template = template;

        if ( scope != IntroductionScope.Default )
        {
            this._scope = scope;
        }
        else if ( templateAttribute is IntroduceAttribute introduceAttribute )
        {
            this._scope = introduceAttribute.Scope;
        }

        if ( this._scope == IntroductionScope.Target )
        {
            this._scope = parameters.AspectLayerInstance.AspectInstance.TargetDeclaration.GetTarget( parameters.AspectLayerInstance.InitialCompilation )
                .GetClosestMemberOrNamedType()
                ?.IsStatic == false
                ? IntroductionScope.Instance
                : IntroductionScope.Static;
        }

        this.OverrideStrategy = overrideStrategy;
        this._explicitlyImplementedInterfaceType = explicitlyImplementedInterfaceType;
    }

    protected virtual void InitializeBuilderCore(
        TBuilder builder,
        TemplateAttributeProperties? templateAttributeProperties,
        in AdviceImplementationContext context ) { }

    protected override void InitializeBuilder( TBuilder builder, in AdviceImplementationContext context )
    {
        var templateAttribute = (ITemplateAttribute?) this.Template?.AdviceAttribute;
        var templateAttributeProperties = templateAttribute?.Properties;
        var templateDeclaration = this.Template?.GetDeclaration( this.SourceCompilation );

        var isInterfaceMember = this.TargetDeclaration.TypeKind is TypeKind.Interface;
        var isAbstractTypeMember = this.TargetDeclaration.IsAbstract;

        // Extern templates have to be used with members without bodies (abstract, partial, extern).
        var isTemplateWithoutBody = this.Template?.TemplateClassMember.TemplateInfo.HasNoBody == true;

        var isExplicitlyAbstractOrPartialOrExtern = 
            templateAttributeProperties?.IsAbstract == true
            || templateAttributeProperties?.IsPartial == true
            || templateAttributeProperties?.IsExtern == true;

        // Without a template, interface members start as public, other type members as private.
        builder.Accessibility =
            this.Template?.Accessibility
            ?? Accessibility.Private;

        // In abstract context, extern members are implicitly abstract for convenience, otherwise one of the other
        // values has to be specified.
        var isImplicitlyAbstract =
            isTemplateWithoutBody
            && !isExplicitlyAbstractOrPartialOrExtern
            && builder.Accessibility != Accessibility.Private
            && (isInterfaceMember || isAbstractTypeMember);

        builder.IsSealed = 
            templateAttributeProperties?.IsSealed 
            ?? templateDeclaration?.IsSealed 
            ?? false;

        // Non-private extern template implicitly denotes an abstract member of an interface or abstract class.
        builder.IsAbstract =
            isAbstractTypeMember && (templateAttributeProperties?.IsAbstract == true || isImplicitlyAbstract);

        builder.IsPartial = 
            isTemplateWithoutBody 
            && templateAttributeProperties?.IsPartial == true;

        builder.IsExtern =
            isTemplateWithoutBody
            && templateAttributeProperties?.IsExtern == true;

        // All abstract members are automatically virtual.
        // Interface members that do not have templates are by default virtual.
        builder.IsVirtual =
            builder.IsAbstract
            || (templateAttributeProperties?.IsVirtual
                ?? templateDeclaration?.IsVirtual
                ?? (isInterfaceMember && builder.Accessibility != Accessibility.Private));

        // Handle the introduction scope.
        // By default, interface members are static because the scope is default and there is no template.
        builder.IsStatic = this._scope switch
        {
            IntroductionScope.Default => templateDeclaration is { IsStatic: true },
            IntroductionScope.Instance => false,
            IntroductionScope.Static => true,
            _ => throw new AssertionFailedException( $"Unexpected IntroductionScope: {this._scope}." )
        };

        if ( this.Template != null )
        {
            CopyTemplateAttributes( templateDeclaration!, builder, context.ServiceProvider );
        }

        this.InitializeBuilderCore( builder, templateAttributeProperties, in context );
    }

    protected override void CompleteBuilder( TBuilder builder )
    {
        base.CompleteBuilder( builder );

        SetBuilderExplicitInterfaceImplementation( builder, this._explicitlyImplementedInterfaceType );
    }

    protected override void ValidateBuilder( TBuilder builder, IDiagnosticAdder diagnosticAdder )
    {
        base.ValidateBuilder( builder, diagnosticAdder );

        var targetDeclaration = this.TargetDeclaration;

        // Check that static member is not virtual.
        if ( builder is { IsStatic: true, IsVirtual: true, DeclaringType.TypeKind: not TypeKind.Interface } )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceStaticVirtualMember.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder),
                    this ) );
        }

        // Check that static member is not sealed.
        if ( builder is { IsStatic: true, IsSealed: true } )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceStaticSealedMember.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder),
                    this ) );
        }

        // Check that instance member is not introduced to a static type.
        if ( targetDeclaration.IsStatic && !builder.IsStatic )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceInstanceMember.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration),
                    this ) );
        }

        // Check that abstract member is not introduced to a non-abstract type.
        if ( builder.IsAbstract && !targetDeclaration.IsAbstract )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceAbstractMemberToNonAbstractType.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration),
                    this ) );
        }

        // Check that partial member is not introduced to a non-partial type.
        if ( builder.IsPartial && !targetDeclaration.IsPartial )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroducePartialMemberToNonPartialType.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration),
                    this ) );
        }

        // Check that template without body is not used OverrideStrategy.Override.
        if ( builder.IsAbstract
             && this.OverrideStrategy is OverrideStrategy.Override or OverrideStrategy.New )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceAbstractMemberWithOverrideStrategy.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder, this.OverrideStrategy),
                    this ) );
        }

        // Check that virtual member is not introduced to a sealed type or a struct.
        if ( targetDeclaration is { IsSealed: true } or { DeclaringType.TypeKind: TypeKind.Struct or TypeKind.RecordStruct }
             && builder.IsVirtual )
        {
            diagnosticAdder.Report(
                AdviceDiagnosticDescriptors.CannotIntroduceVirtualToTargetType.CreateRoslynDiagnostic(
                    targetDeclaration.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder, targetDeclaration),
                    this ) );
        }
    }

    protected static void CopyTemplateAttributes( IDeclaration declaration, IDeclarationBuilder builder, in ProjectServiceProvider serviceProvider )
    {
        var classificationService = serviceProvider.Global.GetRequiredService<AttributeClassificationService>();

        foreach ( var codeElementAttribute in declaration.Attributes )
        {
            if ( classificationService.MustCopyTemplateAttribute( codeElementAttribute ) )
            {
                builder.AddAttribute( codeElementAttribute.ToAttributeConstruction() );
            }
        }
    }

    private static void SetBuilderExplicitInterfaceImplementation( TBuilder builder, INamedType? explicitlyImplementedInterfaceType )
    {
        if ( explicitlyImplementedInterfaceType == null )
        {
            return;
        }

        switch ( builder )
        {
            case MethodBuilder methodBuilder:
                if ( explicitlyImplementedInterfaceType.Methods.OfExactSignature( methodBuilder ) is { } interfaceMethod )
                {
                    methodBuilder.SetExplicitInterfaceImplementation( interfaceMethod );

                    return;
                }

                break;

            case PropertyBuilder propertyBuilder:
                if ( explicitlyImplementedInterfaceType.Properties.OfName( propertyBuilder.Name ).SingleOrDefault() is { } interfaceProperty )
                {
                    propertyBuilder.SetExplicitInterfaceImplementation( interfaceProperty );

                    return;
                }

                break;

            case EventBuilder eventBuilder:
                if ( explicitlyImplementedInterfaceType.Events.OfName( eventBuilder.Name ).SingleOrDefault() is { } interfaceEvent )
                {
                    eventBuilder.SetExplicitInterfaceImplementation( interfaceEvent );

                    return;
                }

                break;

            case IndexerBuilder indexerBuilder:
                if ( explicitlyImplementedInterfaceType.Indexers.OfExactSignature( indexerBuilder ) is { } interfaceIndexer )
                {
                    indexerBuilder.SetExplicitInterfaceImplementation( interfaceIndexer );

                    return;
                }

                break;
        }

        throw new InvalidOperationException(
            MetalamaStringFormatter.Format(
                $"The member '{builder}' can't be used to explicitly implement the interface '{explicitlyImplementedInterfaceType}', because it doesn't match any member of the interface." ) );
    }

    protected IntroductionAdviceResult<TIntroduced> CreateSuccessResult( AdviceOutcome outcome, TBuilder introducedMember )
    {
        return new IntroductionAdviceResult<TIntroduced>( this.AdviceKind, outcome, introducedMember.ToRef().As<TIntroduced>(), null );
    }

    public override string ToString() => $"Introduce {typeof(TIntroduced)} '{this.MemberName}' into '{this.TargetDeclaration}'";
}
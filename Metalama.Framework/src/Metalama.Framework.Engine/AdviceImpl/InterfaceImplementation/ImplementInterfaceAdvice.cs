// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Accessibility = Metalama.Framework.Code.Accessibility;

namespace Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;

internal sealed partial class ImplementInterfaceAdvice : Advice<ImplementInterfaceAdviceResult>
{
    private readonly List<InterfaceSpecification> _interfaceSpecifications;
    private readonly INamedType _interfaceType;
    private readonly OverrideStrategy _overrideStrategy;
    private readonly IObjectReader _tags;
    private readonly IAdviceFactoryImpl _adviceFactory;
    private readonly TemplateProvider _templateProvider;

    private new INamedType TargetDeclaration => (INamedType) base.TargetDeclaration;

    public ImplementInterfaceAdvice(
        AdviceConstructorParameters<INamedType> parameters,
        INamedType interfaceType,
        OverrideStrategy overrideStrategy,
        IObjectReader tags,
        IAdviceFactoryImpl adviceFactory,
        TemplateProvider templateProvider )
        : base( parameters )
    {
        this._interfaceType = interfaceType;
        this._overrideStrategy = overrideStrategy;
        this._interfaceSpecifications = [];
        this._tags = tags;
        this._adviceFactory = adviceFactory;
        this._templateProvider = templateProvider;
    }

    public override AdviceKind AdviceKind => AdviceKind.ImplementInterface;

    private void Initialize( in AdviceImplementationContext context )
    {
        var interfaceType = this._interfaceType.ForCompilation( context.MutableCompilation );
        var contextCopy = context;

        switch ( this._overrideStrategy )
        {
            case OverrideStrategy.Fail:
            case OverrideStrategy.Ignore:
            case OverrideStrategy.Override:
                break;

            default:
                context.Diagnostics.Report(
                    AdviceDiagnosticDescriptors.InterfaceUnsupportedOverrideStrategy.CreateRoslynDiagnostic(
                        this.GetDiagnosticLocation(),
                        (this.AspectInstance.AspectClass.ShortName, InterfaceType: interfaceType,
                         this.TargetDeclaration,
                         this._overrideStrategy),
                        this ) );

                break;
        }

        if ( interfaceType is { IsGeneric: true, IsCanonicalGenericInstance: true } )
        {
            context.Diagnostics.Report(
                AdviceDiagnosticDescriptors.CannotImplementCanonicalGenericInstanceOfGenericInterface.CreateRoslynDiagnostic(
                    this.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, InterfaceType: interfaceType, this.TargetDeclaration),
                    this ) );

            // No other diagnostics should be reported after this.
            return;
        }

        // When initializing, it is not known which types the target type is implementing.
        // Therefore, a specification for all interfaces should be prepared and only diagnostics related advice parameters and aspect class
        // should be reported.

        var templateReflectionContext =
            this.TemplateInstance.TemplateClass.GetTemplateReflectionContext( this.SourceCompilation.CompilationContext );

        var templateClassType = templateReflectionContext.GetCompilationModel( this.SourceCompilation )
            .Factory.GetTypeByReflectionName( this.TemplateInstance.TemplateClass.FullName );

        if ( this.TargetDeclaration is { TypeKind: Code.TypeKind.Interface } )
        {
            // For interfaces, do not map members or expand base interfaces.
            this._interfaceSpecifications.Add( new InterfaceSpecification( interfaceType.ToRef(), [] ) );
        }
        else
        {
            // Prepare all interface types that need to be introduced.
            var interfacesToIntroduce =
                new[] { (InterfaceType: interfaceType, IsTopLevel: true) }
                    .Concat( interfaceType.AllImplementedInterfaces.SelectAsImmutableArray( i => (InterfaceType: i, IsTopLevel: false) ) )
                    .ToDictionary( x => x.InterfaceType, x => x.IsTopLevel, this.SourceCompilation.Comparers.Default );

            // No explicit member specification was given, we have to detect introduced members corresponding to all interface members.
            foreach ( var pair in interfacesToIntroduce )
            {
                var introducedInterface = pair.Key;
                List<MemberSpecification> memberSpecifications = [];

                void TryAddMember<T>( T interfaceMember, Func<T, TemplateMember<T>?> getAspectInterfaceMember, Func<T, bool> membersMatch )
                    where T : class, IMember
                {
                    var memberTemplate = getAspectInterfaceMember( interfaceMember );

                    if ( memberTemplate == null )
                    {
                        // Do nothing. Interface members can (and should) be specified using [Introduce] or [ExplicitInterfaceMember] now.
                    }
                    else
                    {
                        var memberTemplateDeclaration = memberTemplate.GetDeclaration( this.SourceCompilation );

                        if ( !membersMatch( memberTemplateDeclaration ) )
                        {
                            contextCopy.Diagnostics.Report(
                                AdviceDiagnosticDescriptors.DeclarativeInterfaceMemberDoesNotMatch.CreateRoslynDiagnostic(
                                    this.GetDiagnosticLocation(),
                                    (this.AspectInstance.AspectClass.ShortName, this.TargetDeclaration,
                                     InterfaceType: interfaceType,
                                     memberTemplateDeclaration,
                                     interfaceMember),
                                    this ) );
                        }
                        else
                        {
                            var memberSpecification = new MemberSpecification( interfaceMember.ToRef(), null, memberTemplate.As<IMember>() );

                            var isPublic = memberTemplate.Accessibility == Accessibility.Public;

                            if ( interfaceMember is IPropertyOrIndexer property )
                            {
                                if ( property.GetMethod != null )
                                {
                                    isPublic &= memberTemplate.GetAccessorAccessibility == Accessibility.Public;
                                }

                                if ( property.SetMethod != null )
                                {
                                    isPublic &= memberTemplate.SetAccessorAccessibility == Accessibility.Public;
                                }
                            }

                            if ( !memberSpecification.IsExplicit && !isPublic )
                            {
                                contextCopy.Diagnostics.Report(
                                    AdviceDiagnosticDescriptors.ImplicitInterfaceImplementationHasToBePublic.CreateRoslynDiagnostic(
                                        this.GetDiagnosticLocation(),
                                        (this.AspectInstance.AspectClass.ShortName, interfaceType, memberTemplateDeclaration),
                                        this ) );
                            }
                            else
                            {
                                memberSpecifications.Add( memberSpecification );
                            }
                        }
                    }
                }

                foreach ( var interfaceMethod in introducedInterface.Methods )
                {
                    TryAddMember(
                        interfaceMethod,
                        GetAspectInterfaceMethod,
                        templateMethod => SignatureTypeComparer.Instance.Equals(
                                              interfaceMethod.ReturnParameter.Type,
                                              templateMethod.ReturnParameter.Type )
                                          && interfaceMethod.ReturnParameter.RefKind == templateMethod.ReturnParameter.RefKind );
                }

                // Indexers are ignored, because there are no indexer templates.

                foreach ( var interfaceProperty in introducedInterface.Properties )
                {
                    TryAddMember(
                        interfaceProperty,
                        GetAspectInterfaceProperty,
                        templateProperty => this.SourceCompilation.Comparers.Default.Equals( interfaceProperty.Type, templateProperty.Type )
                                            && interfaceProperty.RefKind == templateProperty.RefKind );
                }

                foreach ( var interfaceEvent in introducedInterface.Events )
                {
                    TryAddMember(
                        interfaceEvent,
                        GetAspectInterfaceEvent,
                        templateEvent => this.SourceCompilation.Comparers.Default.Equals( interfaceEvent.Type, templateEvent.Type ) );
                }

                this._interfaceSpecifications.Add( new InterfaceSpecification( introducedInterface.ToRef(), memberSpecifications ) );
            }

            TemplateMember<IMethod>? GetAspectInterfaceMethod( IMethod interfaceMethod )
            {
                var method = templateClassType.AllMethods.OfName( interfaceMethod.Name ).SingleOrDefault( m => m.SignatureEquals( interfaceMethod ) );

                if ( method != null && TryGetInterfaceMemberTemplate( method, out var classMember ) )
                {
                    return TemplateMemberFactory.Create( method, classMember, this._templateProvider, this._tags );
                }

                return null;
            }

            TemplateMember<IProperty>? GetAspectInterfaceProperty( IProperty interfaceProperty )
            {
                var property = templateClassType.AllProperties.OfName( interfaceProperty.Name ).SingleOrDefault( p => p.SignatureEquals( interfaceProperty ) );

                if ( property != null && TryGetInterfaceMemberTemplate( property, out var classMember ) )
                {
                    return TemplateMemberFactory.Create( property, classMember, this._templateProvider, this._tags );
                }

                return null;
            }

            TemplateMember<IEvent>? GetAspectInterfaceEvent( IEvent interfaceEvent )
            {
                var @event = templateClassType.AllEvents.OfName( interfaceEvent.Name ).SingleOrDefault( e => e.SignatureEquals( interfaceEvent ) );

                if ( @event != null && TryGetInterfaceMemberTemplate( @event, out var classMember ) )
                {
                    return TemplateMemberFactory.Create( @event, classMember, this._templateProvider, this._tags );
                }

                return null;
            }

            bool TryGetInterfaceMemberTemplate(
                IMember member,
                [NotNullWhen( true )] out TemplateClassMember? templateClassMember )
            {
                return this.TemplateInstance.TemplateClass.TryGetInterfaceMember(
                    member.GetSymbol().AssertSymbolNotNull(),
                    out templateClassMember );
            }
        }
    }

    protected override ImplementInterfaceAdviceResult Implement( in AdviceImplementationContext context )
    {
        var contextCopy = context;
        var serviceProvider = context.ServiceProvider;
        var compilation = context.MutableCompilation;

        this.Initialize( context );
        context.ThrowIfAnyError();

        // Adding interfaces may run into three problems:
        //      1) Target type already implements the interface.
        //      2) Target type already implements an ancestor of the interface.

        var targetType = this.TargetDeclaration.ForCompilation( context.MutableCompilation );
        var diagnostics = new DiagnosticBag();
        var implementedInterfaces = new List<ImplementationResult>();
        var implementedInterfaceMembers = new List<MemberImplementationResult>();

        foreach ( var interfaceSpecification in this._interfaceSpecifications )
        {
            // Validate that the interface must be introduced to the specific target.
            bool skipInterfaceBaseList;
            var forceIgnore = false;

            var interfaceType = interfaceSpecification.InterfaceType.GetTarget( context.MutableCompilation );

            if ( targetType.AllImplementedInterfaces.Any( t => t.Equals( interfaceType ) ) )
            {
                switch ( this._overrideStrategy )
                {
                    case OverrideStrategy.Ignore:
                        implementedInterfaces.Add( new ImplementationResult( interfaceType, InterfaceImplementationOutcome.Ignore ) );
                        skipInterfaceBaseList = true;

                        // The interface is implemented, so we ignore its members, as if they had InterfaceMemberOverrideStrategy.Ignore.
                        forceIgnore = true;

                        break;

                    case OverrideStrategy.Fail:
                        diagnostics.Report(
                            AdviceDiagnosticDescriptors.InterfaceIsAlreadyImplemented.CreateRoslynDiagnostic(
                                targetType.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, InterfaceType: interfaceType, targetType),
                                this ) );

                        continue;

                    case OverrideStrategy.Override:
                        implementedInterfaces.Add(
                            new ImplementationResult(
                                interfaceType,
                                InterfaceImplementationOutcome.Implement,
                                this.TargetDeclaration,
                                this._adviceFactory ) );

                        skipInterfaceBaseList = true;

                        break;

                    default:
                        throw new AssertionFailedException( $"Unexpected OverrideStrategy: {this._overrideStrategy}." );
                }
            }
            else
            {
                implementedInterfaces.Add(
                    new ImplementationResult(
                        interfaceType,
                        InterfaceImplementationOutcome.Implement,
                        this.TargetDeclaration,
                        this._adviceFactory ) );

                skipInterfaceBaseList = false;
            }

            var replaceDefaultConstructorAdded = false;

            void AddTransformationNoDuplicates( ITransformation transformation )
            {
                if ( transformation is IntroduceConstructorTransformation )
                {
                    if ( !replaceDefaultConstructorAdded )
                    {
                        replaceDefaultConstructorAdded = true;
                    }
                    else
                    {
                        return;
                    }
                }

                contextCopy.AddTransformation( transformation );
            }

            var interfaceMemberMap = new Dictionary<IMember, IMember>( context.MutableCompilation.Comparers.Default );

            // Map members only for non-interfaces.
            if ( this.TargetDeclaration is not { TypeKind: Code.TypeKind.Interface } )
            {
                foreach ( var memberSpec in interfaceSpecification.MemberSpecifications )
                {
                    // Collect implemented interface members and add non-observable transformations.
                    var templateAttributeProperties = (memberSpec.Template?.AdviceAttribute as ITemplateAttribute)?.Properties;

                    var interfaceMember = memberSpec.InterfaceMember.GetTarget( context.MutableCompilation );

                    switch ( interfaceMember )
                    {
                        case IMethod interfaceMethod:
                            var existingMethod = targetType.AllMethods.OfName( interfaceMethod.Name ).SingleOrDefault( m => m.SignatureEquals( interfaceMethod ) );
                            var templateMethod = memberSpec.Template?.As<IMethod>();
                            var templateMethodDeclaration = templateMethod?.GetDeclaration( context.MutableCompilation );
                            var redirectionTargetMethod = memberSpec.TargetMember?.As<IMethod>().GetTarget( context.MutableCompilation );

                            if ( existingMethod != null && !memberSpec.IsExplicit )
                            {
#pragma warning disable CS0618 // Type is obsolete
                                switch ( memberSpec.OverrideStrategy )
                                {
                                    case { } when forceIgnore:
                                    case InterfaceMemberOverrideStrategy.Ignore:

                                        implementedInterfaceMembers.Add(
                                            new MemberImplementationResult(
                                                compilation,
                                                interfaceMember,
                                                InterfaceMemberImplementationOutcome.UseExisting,
                                                existingMethod ) );

                                        continue;

                                    case InterfaceMemberOverrideStrategy.Default when this._overrideStrategy != OverrideStrategy.Override:
                                    case InterfaceMemberOverrideStrategy.Fail:
                                        diagnostics.Report(
                                            AdviceDiagnosticDescriptors.ImplicitInterfaceMemberAlreadyExists.CreateRoslynDiagnostic(
                                                targetType.GetDiagnosticLocation(),
                                                (this.AspectInstance.AspectClass.ShortName, interfaceMethod, targetType, existingMethod),
                                                this ) );

                                        continue;

                                    case InterfaceMemberOverrideStrategy.Default when this._overrideStrategy == OverrideStrategy.Override:
                                        if ( existingMethod.Accessibility != Accessibility.Public )
                                        {
                                            diagnostics.Report(
                                                AdviceDiagnosticDescriptors.CannotOverrideNonPublicInterfaceMember.CreateRoslynDiagnostic(
                                                    targetType.GetDiagnosticLocation(),
                                                    (this.AspectInstance.AspectClass.ShortName, existingMethod),
                                                    this ) );

                                            continue;
                                        }

                                        if ( existingMethod.DeclaringType.Equals( targetType ) )
                                        {
                                            AddTransformationNoDuplicates(
                                                new OverrideMethodTransformation(
                                                    this.AspectLayerInstance,
                                                    existingMethod.ToFullRef(),
                                                    templateMethod.AssertNotNull().ForOverride( existingMethod ) ) );

                                            implementedInterfaceMembers.Add(
                                                new MemberImplementationResult(
                                                    compilation,
                                                    interfaceMember,
                                                    InterfaceMemberImplementationOutcome.Override,
                                                    existingMethod ) );
                                        }
                                        else
                                        {
                                            if ( !existingMethod.IsVirtual || existingMethod.IsSealed )
                                            {
                                                diagnostics.Report(
                                                    AdviceDiagnosticDescriptors.CannotOverrideNonVirtualInterfaceMember.CreateRoslynDiagnostic(
                                                        targetType.GetDiagnosticLocation(),
                                                        (this.AspectInstance.AspectClass.ShortName, existingMethod),
                                                        this ) );

                                                continue;
                                            }

                                            IntroduceMethod( false, true );
                                        }

                                        break;

                                    case InterfaceMemberOverrideStrategy.MakeExplicit:
                                        IntroduceMethod( true, false );

                                        break;

                                    default:
                                        throw new AssertionFailedException(
                                            $"Unexpected value for InterfaceMemberOverrideStrategy: {memberSpec.OverrideStrategy}." );
                                }
#pragma warning restore CS0618
                            }
                            else
                            {
                                IntroduceMethod( memberSpec.IsExplicit, false );
                            }

                            void IntroduceMethod( bool isExplicit, bool isOverride )
                            {
                                var isIteratorMethod = templateMethod?.IsIteratorMethod ?? redirectionTargetMethod.AssertNotNull().IsIteratorMethod() == true;
                                var isVirtual = templateAttributeProperties?.IsVirtual ?? templateMethodDeclaration is { IsVirtual: true };

                                var methodBuilder = this.GetImplMethodBuilder( targetType, interfaceMethod, isIteratorMethod, isExplicit, isVirtual, isOverride );

                                CopyAttributes( interfaceMethod, methodBuilder );
                                methodBuilder.Freeze();

                                AddTransformationNoDuplicates( methodBuilder.ToTransformation() );
                                interfaceMemberMap.Add( interfaceMethod, methodBuilder );

                                if ( templateMethod != null )
                                {
                                    AddTransformationNoDuplicates(
                                        new OverrideMethodTransformation(
                                            this.AspectLayerInstance,
                                            methodBuilder.ToFullRef(),
                                            templateMethod.ForIntroduction( methodBuilder ) ) );
                                }
                                else
                                {
                                    AddTransformationNoDuplicates(
                                        new RedirectMethodTransformation(
                                            this,
                                            methodBuilder.ToFullRef(),
                                            memberSpec.TargetMember.AssertNotNull().AsFullRef<IMethod>() ) );
                                }

                                implementedInterfaceMembers.Add(
                                    new MemberImplementationResult(
                                        compilation,
                                        interfaceMember,
                                        InterfaceMemberImplementationOutcome.Introduce,
                                        methodBuilder ) );
                            }

                            break;

                        case IProperty interfaceProperty:
                            var existingProperty = targetType.Properties.SingleOrDefault( p => p.SignatureEquals( interfaceProperty ) );
                            var templateProperty = memberSpec.Template?.As<IProperty>();
                            var templatePropertyDeclaration = templateProperty?.GetDeclaration( context.MutableCompilation );
                            var redirectionTargetProperty = memberSpec.TargetMember?.As<IProperty>().GetTarget( context.MutableCompilation );

                            if ( existingProperty != null && !memberSpec.IsExplicit )
                            {
#pragma warning disable CS0618 // Type is obsolete
                                switch ( memberSpec.OverrideStrategy )
                                {
                                    case { } when forceIgnore:
                                    case InterfaceMemberOverrideStrategy.Ignore:

                                        implementedInterfaceMembers.Add(
                                            new MemberImplementationResult(
                                                compilation,
                                                interfaceMember,
                                                InterfaceMemberImplementationOutcome.UseExisting,
                                                existingProperty ) );

                                        continue;

                                    case InterfaceMemberOverrideStrategy.Default when this._overrideStrategy != OverrideStrategy.Override:
                                    case InterfaceMemberOverrideStrategy.Fail:
                                        diagnostics.Report(
                                            AdviceDiagnosticDescriptors.ImplicitInterfaceMemberAlreadyExists.CreateRoslynDiagnostic(
                                                targetType.GetDiagnosticLocation(),
                                                (this.AspectInstance.AspectClass.ShortName, interfaceProperty, targetType, existingProperty),
                                                this ) );

                                        continue;

                                    case InterfaceMemberOverrideStrategy.Default when this._overrideStrategy == OverrideStrategy.Override:
                                        if ( existingProperty.Accessibility != Accessibility.Public )
                                        {
                                            diagnostics.Report(
                                                AdviceDiagnosticDescriptors.CannotOverrideNonPublicInterfaceMember.CreateRoslynDiagnostic(
                                                    targetType.GetDiagnosticLocation(),
                                                    (this.AspectInstance.AspectClass.ShortName, existingProperty),
                                                    this ) );

                                            continue;
                                        }

                                        if ( existingProperty.DeclaringType.Equals( targetType ) )
                                        {
                                            var accessorTemplates = templateProperty.GetAccessorTemplates();

                                            AddTransformationNoDuplicates(
                                                new OverridePropertyTransformation(
                                                    this.AspectLayerInstance,
                                                    existingProperty.ToFullRef(),
                                                    existingProperty.GetMethod != null
                                                        ? accessorTemplates.Get?.ForOverride( existingProperty.GetMethod )
                                                        : null,
                                                    existingProperty.SetMethod != null
                                                        ? accessorTemplates.Set?.ForOverride( existingProperty.SetMethod )
                                                        : null ) );

                                            implementedInterfaceMembers.Add(
                                                new MemberImplementationResult(
                                                    compilation,
                                                    interfaceMember,
                                                    InterfaceMemberImplementationOutcome.Override,
                                                    existingProperty ) );
                                        }
                                        else
                                        {
                                            if ( !existingProperty.IsVirtual || existingProperty.IsSealed )
                                            {
                                                diagnostics.Report(
                                                    AdviceDiagnosticDescriptors.CannotOverrideNonVirtualInterfaceMember.CreateRoslynDiagnostic(
                                                        targetType.GetDiagnosticLocation(),
                                                        (this.AspectInstance.AspectClass.ShortName, existingProperty),
                                                        this ) );

                                                continue;
                                            }

                                            IntroduceProperty( false, true );
                                        }

                                        break;

                                    case InterfaceMemberOverrideStrategy.MakeExplicit:
                                        IntroduceProperty( true, false );

                                        break;

                                    default:
                                        throw new NotImplementedException( $"The strategy OverrideStrategy.{this._overrideStrategy} is not implemented." );
                                }
#pragma warning restore CS0618
                            }
                            else
                            {
                                IntroduceProperty( memberSpec.IsExplicit, false );
                            }

                            void IntroduceProperty( bool isExplicit, bool isOverride )
                            {
                                var isAutoProperty = templatePropertyDeclaration?.IsAutoPropertyOrField == true;
                                var isVirtual = templateAttributeProperties?.IsVirtual ?? templatePropertyDeclaration is { IsVirtual: true };

                                var getAccessibility =
                                    templateProperty?.GetAccessorAccessibility ?? Accessibility.Public;

                                var setAccessibility =
                                    templateProperty?.SetAccessorAccessibility ?? Accessibility.Public;

                                var hasGetter =
                                    interfaceProperty.GetMethod != null || (!isExplicit && templatePropertyDeclaration?.GetMethod != null);

                                var hasSetter =
                                    interfaceProperty.SetMethod != null || (!isExplicit && templatePropertyDeclaration?.SetMethod != null);

                                // Check that there are no accessors required by interface that are missing from the template.

                                if ( templateProperty != null )
                                {
                                    Invariant.Assert( templatePropertyDeclaration != null );

                                    var getMethodMissingFromTemplate = interfaceProperty.GetMethod != null && templatePropertyDeclaration.GetMethod == null;
                                    var setMethodMissingFromTemplate = interfaceProperty.SetMethod != null && templatePropertyDeclaration.SetMethod == null;
                                    var getMethodUnexpectedInTemplate = interfaceProperty.GetMethod == null && templatePropertyDeclaration.GetMethod != null;
                                    var setMethodUnexpectedInTemplate = interfaceProperty.SetMethod == null && templatePropertyDeclaration.SetMethod != null;
                                    var setInitOnlyInTemplate = templatePropertyDeclaration.Writeability is Writeability.InitOnly;
                                    var setInitOnlyInInterface = interfaceProperty.Writeability is Writeability.InitOnly;

                                    var missingAccessor =
                                        (getMethodMissingFromTemplate, setMethodMissingFromTemplate, setInitOnlyInTemplate, setInitOnlyInInterface) switch
                                        {
                                            (true, _, _, _) => "get",              // Missing getter.
                                            (false, true, _, false) => "set",      // Missing setter.
                                            (false, true, _, true) => "init",      // Missing init-only setter.
                                            (false, false, true, false) => "set",  // Interface has setter, template has init-only setter.
                                            (false, false, false, true) => "init", // Interface has init-only setter, template has setter.
                                            _ => null
                                        };

                                    if ( missingAccessor != null )
                                    {
                                        diagnostics.Report(
                                            AdviceDiagnosticDescriptors.InterfacePropertyIsMissingAccessor.CreateRoslynDiagnostic(
                                                targetType.GetDiagnosticLocation(),
                                                (this.AspectInstance.AspectClass.ShortName, interfaceProperty, targetType,
                                                 templatePropertyDeclaration,
                                                 missingAccessor),
                                                this ) );

                                        return;
                                    }

                                    var unexpectedAccessor =
                                        (isExplicit, getMethodUnexpectedInTemplate, setMethodUnexpectedInTemplate, setInitOnlyInTemplate) switch
                                        {
                                            (true, true, _, _) => "get",         // Unexpected getter.
                                            (true, false, true, false) => "set", // Unexpected setter.
                                            (true, false, true, true) => "init", // Unexpected init-only setter.
                                            _ => null
                                        };

                                    if ( unexpectedAccessor != null )
                                    {
                                        diagnostics.Report(
                                            AdviceDiagnosticDescriptors.ExplicitInterfacePropertyHasSuperficialAccessor.CreateRoslynDiagnostic(
                                                targetType.GetDiagnosticLocation(),
                                                (this.AspectInstance.AspectClass.ShortName, interfaceProperty, targetType,
                                                 templatePropertyDeclaration,
                                                 unexpectedAccessor),
                                                this ) );

                                        return;
                                    }
                                }

                                var hasImplicitSetter = templatePropertyDeclaration?.SetMethod?.IsImplicitlyDeclared ?? false;

                                var propertyBuilder = this.GetImplPropertyBuilder(
                                    targetType,
                                    interfaceProperty,
                                    getAccessibility,
                                    setAccessibility,
                                    hasGetter,
                                    hasSetter,
                                    isAutoProperty,
                                    isExplicit,
                                    isVirtual,
                                    isOverride,
                                    hasImplicitSetter );

                                if ( templateProperty != null )
                                {
                                    Invariant.Assert( templatePropertyDeclaration != null );

                                    CopyAttributes( templatePropertyDeclaration, propertyBuilder );

                                    if ( hasGetter )
                                    {
                                        CopyAttributes(
                                            templatePropertyDeclaration.GetMethod.AssertNotNull(),
                                            propertyBuilder.GetMethod.AssertNotNull() );
                                    }

                                    if ( hasSetter )
                                    {
                                        CopyAttributes(
                                            templatePropertyDeclaration.SetMethod.AssertNotNull(),
                                            propertyBuilder.SetMethod.AssertNotNull() );
                                    }
                                }

                                propertyBuilder.Freeze();
                                AddTransformationNoDuplicates( propertyBuilder.CreateTransformation( templateProperty.GetInitializerTemplate() ) );
                                interfaceMemberMap.Add( interfaceProperty, propertyBuilder );

                                if ( templateProperty != null )
                                {
                                    if ( isAutoProperty != true )
                                    {
                                        var accessorTemplates = templateProperty.GetAccessorTemplates();

                                        AddTransformationNoDuplicates(
                                            new OverridePropertyTransformation(
                                                this.AspectLayerInstance,
                                                propertyBuilder.ToRef(),
                                                propertyBuilder.GetMethod != null
                                                    ? accessorTemplates.Get?.ForOverride( propertyBuilder.GetMethod )
                                                    : null,
                                                propertyBuilder.SetMethod != null
                                                    ? accessorTemplates.Set?.ForOverride( propertyBuilder.SetMethod )
                                                    : null ) );
                                    }
                                    else
                                    {
                                        OverrideHelper.AddTransformationsForStructField(
                                            targetType.ForCompilation( compilation ),
                                            this.AspectLayerInstance,
                                            AddTransformationNoDuplicates );
                                    }
                                }
                                else
                                {
                                    AddTransformationNoDuplicates(
                                        new RedirectPropertyTransformation(
                                            this.AspectLayerInstance,
                                            propertyBuilder.ToRef(),
                                            redirectionTargetProperty.AssertNotNull().ToFullRef() ) );
                                }

                                implementedInterfaceMembers.Add(
                                    new MemberImplementationResult(
                                        compilation,
                                        interfaceMember,
                                        InterfaceMemberImplementationOutcome.Introduce,
                                        propertyBuilder ) );
                            }

                            break;

                        case IIndexer:
                            throw new NotImplementedException( "Implementing interface indexers is not yet supported." );

                        case IEvent interfaceEvent:
                            var existingEvent = targetType.Events.SingleOrDefault( p => p.SignatureEquals( interfaceEvent ) );
                            var templateEvent = memberSpec.Template?.As<IEvent>();
                            var redirectionTargetEvent = memberSpec.TargetMember?.AsFullRef<IEvent>();

                            if ( existingEvent != null && !memberSpec.IsExplicit )
                            {
#pragma warning disable CS0618 // Type is obsolete
                                switch ( memberSpec.OverrideStrategy )
                                {
                                    case { } when forceIgnore:
                                    case InterfaceMemberOverrideStrategy.Ignore:

                                        implementedInterfaceMembers.Add(
                                            new MemberImplementationResult(
                                                compilation,
                                                interfaceMember,
                                                InterfaceMemberImplementationOutcome.UseExisting,
                                                existingEvent ) );

                                        continue;

                                    case InterfaceMemberOverrideStrategy.Default when this._overrideStrategy != OverrideStrategy.Override:
                                    case InterfaceMemberOverrideStrategy.Fail:
                                        diagnostics.Report(
                                            AdviceDiagnosticDescriptors.ImplicitInterfaceMemberAlreadyExists.CreateRoslynDiagnostic(
                                                targetType.GetDiagnosticLocation(),
                                                (this.AspectInstance.AspectClass.ShortName, interfaceEvent, targetType, existingEvent),
                                                this ) );

                                        continue;

                                    case InterfaceMemberOverrideStrategy.Default when this._overrideStrategy == OverrideStrategy.Override:
                                        if ( existingEvent.Accessibility != Accessibility.Public )
                                        {
                                            diagnostics.Report(
                                                AdviceDiagnosticDescriptors.CannotOverrideNonPublicInterfaceMember.CreateRoslynDiagnostic(
                                                    targetType.GetDiagnosticLocation(),
                                                    (this.AspectInstance.AspectClass.ShortName, existingEvent),
                                                    this ) );

                                            continue;
                                        }

                                        if ( existingEvent.DeclaringType.Equals( targetType ) )
                                        {
                                            var accessorTemplates = templateEvent.GetAccessorTemplates();

                                            AddTransformationNoDuplicates(
                                                new OverrideEventTransformation(
                                                    this.AspectLayerInstance,
                                                    existingEvent.ToFullRef(),
                                                    accessorTemplates.Add?.ForOverride( existingEvent.AddMethod ),
                                                    accessorTemplates.Remove?.ForOverride( existingEvent.RemoveMethod ) ) );

                                            implementedInterfaceMembers.Add(
                                                new MemberImplementationResult(
                                                    compilation,
                                                    interfaceMember,
                                                    InterfaceMemberImplementationOutcome.Override,
                                                    existingEvent ) );
                                        }
                                        else
                                        {
                                            if ( !existingEvent.IsVirtual || existingEvent.IsSealed )
                                            {
                                                diagnostics.Report(
                                                    AdviceDiagnosticDescriptors.CannotOverrideNonVirtualInterfaceMember.CreateRoslynDiagnostic(
                                                        targetType.GetDiagnosticLocation(),
                                                        (this.AspectInstance.AspectClass.ShortName, existingEvent),
                                                        this ) );

                                                continue;
                                            }

                                            IntroduceEvent( false, true );
                                        }

                                        break;

                                    case InterfaceMemberOverrideStrategy.MakeExplicit:
                                        IntroduceEvent( true, false );

                                        break;

                                    default:
                                        throw new NotImplementedException( $"The strategy OverrideStrategy.{this._overrideStrategy} is not implemented." );
                                }
#pragma warning restore CS0618
                            }
                            else
                            {
                                IntroduceEvent( memberSpec.IsExplicit, false );
                            }

                            void IntroduceEvent( bool isExplicit, bool isOverride )
                            {
                                var templateEventDeclaration = templateEvent?.GetDeclaration( contextCopy.MutableCompilation );
                                var isEventField = templateEventDeclaration?.IsEventField() ?? false;
                                var isVirtual = templateAttributeProperties?.IsVirtual ?? templateEventDeclaration is { IsVirtual: true };

                                var eventBuilder = this.GetImplEventBuilder(
                                    targetType,
                                    interfaceEvent,
                                    isEventField,
                                    isExplicit,
                                    isVirtual,
                                    isOverride );

                                if ( templateEvent != null )
                                {
                                    Invariant.Assert( templateEventDeclaration != null );

                                    eventBuilder.InitializerExpression = templateEventDeclaration.InitializerExpression;

                                    CopyAttributes( templateEventDeclaration, eventBuilder );
                                    CopyAttributes( templateEventDeclaration, eventBuilder.AddMethod.AssertNotNull() );
                                    CopyAttributes( templateEventDeclaration, eventBuilder.RemoveMethod.AssertNotNull() );
                                }

                                eventBuilder.Freeze();
                                AddTransformationNoDuplicates( eventBuilder.CreateTransformation( templateEvent ) );
                                interfaceMemberMap.Add( interfaceEvent, eventBuilder );

                                if ( templateEvent != null )
                                {
                                    if ( !isEventField )
                                    {
                                        var accessorTemplates = templateEvent.GetAccessorTemplates();

                                        AddTransformationNoDuplicates(
                                            new OverrideEventTransformation(
                                                this.AspectLayerInstance,
                                                eventBuilder.ToFullRef(),
                                                accessorTemplates.Add?.ForOverride( eventBuilder.AddMethod ),
                                                accessorTemplates.Remove?.ForOverride( eventBuilder.RemoveMethod ) ) );
                                    }
                                    else
                                    {
                                        OverrideHelper.AddTransformationsForStructField(
                                            targetType.ForCompilation( compilation ),
                                            this.AspectLayerInstance,
                                            AddTransformationNoDuplicates );
                                    }
                                }
                                else
                                {
                                    AddTransformationNoDuplicates(
                                        new RedirectEventTransformation(
                                            this.AspectLayerInstance,
                                            eventBuilder.ToFullRef(),
                                            redirectionTargetEvent.AssertNotNull() ) );
                                }

                                implementedInterfaceMembers.Add(
                                    new MemberImplementationResult(
                                        compilation,
                                        interfaceMember,
                                        InterfaceMemberImplementationOutcome.Introduce,
                                        eventBuilder ) );
                            }

                            break;

                        default:
                            throw new AssertionFailedException( $"Unexpected kind of declaration: '{interfaceMember}'." );
                    }

                    void CopyAttributes( IDeclaration source, DeclarationBuilder destination )
                    {
                        var classificationService = serviceProvider.Global.GetRequiredService<AttributeClassificationService>();

                        foreach ( var codeElementAttribute in source.Attributes )
                        {
                            if ( classificationService.MustCopyTemplateAttribute( codeElementAttribute ) )
                            {
                                destination.AddAttribute( codeElementAttribute.ToAttributeConstruction() );
                            }
                        }
                    }
                }
            }

            if ( !skipInterfaceBaseList )
            {
                AddTransformationNoDuplicates(
                    new IntroduceInterfaceTransformation( this.AspectLayerInstance, targetType.ToFullRef(), interfaceType.ToFullRef(), interfaceMemberMap ) );
            }
        }

        if ( diagnostics.HasError() )
        {
            return this.CreateFailedResult( diagnostics.ToImmutableArray() );
        }

        return new ImplementInterfaceAdviceResult(
            implementedInterfaces.All( i => i.Outcome == InterfaceImplementationOutcome.Ignore ) ? AdviceOutcome.Ignore : AdviceOutcome.Default,
            diagnostics.Count > 0 ? diagnostics.ToImmutableArray() : ImmutableArray<Diagnostic>.Empty,
            implementedInterfaces,
            implementedInterfaceMembers );
    }

    private MethodBuilder GetImplMethodBuilder(
        INamedType declaringType,
        IMethod interfaceMethod,
        bool isIteratorMethod,
        bool isExplicit,
        bool isVirtual,
        bool isOverride )
    {
        var name = GetInterfaceMemberName( interfaceMethod, isExplicit );

        var methodBuilder = new MethodBuilder( this.AspectLayerInstance, declaringType, name )
        {
            ReturnParameter = { Type = interfaceMethod.ReturnParameter.Type, RefKind = interfaceMethod.ReturnParameter.RefKind }
        };

        methodBuilder.SetIsIteratorMethod( isIteratorMethod );

        foreach ( var interfaceParameter in interfaceMethod.Parameters )
        {
            _ = methodBuilder.AddParameter(
                interfaceParameter.Name,
                interfaceParameter.Type,
                interfaceParameter.RefKind,
                interfaceParameter.DefaultValue );
        }

        foreach ( var interfaceGenericParameter in interfaceMethod.TypeParameters )
        {
            var genericParameterBuilder = methodBuilder.AddTypeParameter( interfaceGenericParameter );

            foreach ( var interfaceGenericParameterConstraint in interfaceGenericParameter.TypeConstraints )
            {
                genericParameterBuilder.AddTypeConstraint( interfaceGenericParameterConstraint );
            }
        }

        if ( isExplicit )
        {
            methodBuilder.SetExplicitInterfaceImplementation( interfaceMethod );
        }
        else
        {
            methodBuilder.Accessibility = Accessibility.Public;
        }

        if ( isOverride )
        {
            methodBuilder.IsOverride = true;
        }
        else if ( isVirtual )
        {
            methodBuilder.IsVirtual = true;
        }

        return methodBuilder;
    }

    private PropertyBuilder GetImplPropertyBuilder(
        INamedType declaringType,
        IProperty interfaceProperty,
        Accessibility getAccessibility,
        Accessibility setAccessibility,
        bool hasGetter,
        bool hasSetter,
        bool isAutoProperty,
        bool isExplicit,
        bool isVirtual,
        bool isOverride,
        bool hasImplicitSetter )
    {
        var name = GetInterfaceMemberName( interfaceProperty, isExplicit );

        var propertyBuilder = new PropertyBuilder(
            this.AspectLayerInstance,
            declaringType,
            name,
            hasGetter,
            hasSetter,
            isAutoProperty,
            interfaceProperty.Writeability == Writeability.InitOnly,
            false,
            hasImplicitSetter ) { Type = interfaceProperty.Type };

        if ( isExplicit )
        {
            propertyBuilder.SetExplicitInterfaceImplementation( interfaceProperty );
        }
        else
        {
            propertyBuilder.Accessibility = Accessibility.Public;

            if ( propertyBuilder.GetMethod != null )
            {
                propertyBuilder.GetMethod.Accessibility = interfaceProperty.GetMethod != null ? Accessibility.Public : getAccessibility;
            }

            if ( propertyBuilder.SetMethod != null )
            {
                propertyBuilder.SetMethod.Accessibility = interfaceProperty.SetMethod != null ? Accessibility.Public : setAccessibility;
            }
        }

        if ( isOverride )
        {
            propertyBuilder.IsOverride = true;
        }
        else if ( isVirtual )
        {
            propertyBuilder.IsVirtual = true;
        }

        return propertyBuilder;
    }

    private Location? GetDiagnosticLocation() => this.TargetDeclaration.GetDiagnosticLocation();

    private EventBuilder GetImplEventBuilder(
        INamedType declaringType,
        IEvent interfaceEvent,
        bool isEventField,
        bool isExplicit,
        bool isVirtual,
        bool isOverride )
    {
        var name = GetInterfaceMemberName( interfaceEvent, isExplicit );

        var eventBuilder = new EventBuilder( this.AspectLayerInstance, declaringType, name, isEventField ) { Type = interfaceEvent.Type };

        if ( isExplicit )
        {
            eventBuilder.SetExplicitInterfaceImplementation( interfaceEvent );
        }
        else
        {
            eventBuilder.Accessibility = Accessibility.Public;
        }

        if ( isOverride )
        {
            eventBuilder.IsOverride = true;
        }
        else if ( isVirtual )
        {
            eventBuilder.IsVirtual = true;
        }

        return eventBuilder;
    }

    private static string GetInterfaceMemberName( IMember interfaceMember, bool isExplicit )
        => isExplicit ? $"{interfaceMember.DeclaringType.FullName}.{interfaceMember.Name}" : interfaceMember.Name;
}
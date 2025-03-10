// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Attribute = System.Attribute;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Represents the metadata of an aspect class. This class is compilation-independent. It is not used to represent a fabric class.
/// </summary>
public sealed class AspectClass : TemplateClass, IBoundAspectClass
{
    private readonly UserCodeInvoker _userCodeInvoker;
    private readonly IAspect? _prototypeAspectInstance; // Null for abstract classes.
    private readonly List<AspectClass> _childAspectClasses = new();
    private IAspectDriver? _aspectDriver;
    private EligibilityHelper? _eligibilityHelper;

    internal override Type Type { get; }

    public override string FullName { get; }

    public string DisplayName { get; }

    public string? Description { get; }

    string IDiagnosticSource.DiagnosticSourceDescription => $"aspect '{this.ShortName}'";

    internal string? WeaverType { get; }

    internal CompileTimeProject? Project { get; }

    public ImmutableArray<TemplateClass> TemplateClasses { get; }

    public SyntaxAnnotation GeneratedCodeAnnotation { get; }

    /// <summary>
    /// Gets the aspect driver of the current class, responsible for executing the aspect.
    /// </summary>
    public IAspectDriver AspectDriver => this._aspectDriver.AssertNotNull();

    /// <summary>
    /// Gets the list of layers of the current aspect.
    /// </summary>
    internal ImmutableArray<AspectLayer> Layers { get; }

    ImmutableArray<AspectLayer> IAspectClassImpl.Layers => this.Layers;

    public Location? GetDiagnosticLocation( Compilation compilation ) => compilation.GetTypeByMetadataNameSafe( this.FullName ).GetDiagnosticLocation();

    public bool IsAbstract { get; }

    public bool? IsInheritable { get; } = false;

    public bool IsAttribute => typeof(Attribute).IsAssignableFrom( this.Type );

    Type IAspectClass.Type => this.Type;

    public EditorExperienceOptions EditorExperienceOptions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AspectClass"/> class.
    /// </summary>
    private AspectClass(
        ProjectServiceProvider serviceProvider,
        INamedTypeSymbol typeSymbol,
        AspectClass? baseClass,
        CompileTimeProject? project,
        Type aspectType,
        IAspect? prototype,
        IDiagnosticAdder diagnosticAdder,
        ITemplateReflectionContext templateReflectionContext ) : base(
        serviceProvider,
        templateReflectionContext,
        typeSymbol,
        diagnosticAdder,
        baseClass,
        AttributeHelper.GetShortName( typeSymbol.Name ) )
    {
        this.FullName = typeSymbol.GetReflectionFullName().AssertNotNull();
        this.DisplayName = this.ShortName;
        this.IsAbstract = typeSymbol.IsAbstract;
        this.Project = project;
        this._userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();

        var attributeDeserializer = serviceProvider.GetRequiredService<SystemAttributeDeserializer.Provider>()
            .Get( templateReflectionContext.CompilationContext );

        this.Type = aspectType;
        this._prototypeAspectInstance = prototype;
        this.TemplateClasses = ImmutableArray.Create<TemplateClass>( this );
        this.GeneratedCodeAnnotation = MetalamaCompilerAnnotations.CreateGeneratedCodeAnnotation( $"aspect '{this.ShortName}'" );

        baseClass?._childAspectClasses.Add( this );

        List<string?> layers = new();

        if ( baseClass != null )
        {
            this.Description = baseClass.Description;
            this.IsInheritable = baseClass.IsInheritable;
            this.WeaverType = baseClass.WeaverType;

            layers.AddRange( baseClass.Layers.Select( l => l.LayerName ) );

            this.EditorExperienceOptions = new EditorExperienceOptions
            {
                SuggestAsAddAttribute = baseClass.EditorExperienceOptions.SuggestAsAddAttribute,
                SuggestAsLiveTemplate = baseClass.EditorExperienceOptions.SuggestAsLiveTemplate
            };
        }
        else
        {
            this.EditorExperienceOptions = EditorExperienceOptions.Default;
            layers.Add( null );
        }

        if ( typeof(IConditionallyInheritableAspect).IsAssignableFrom( aspectType ) )
        {
            this.IsInheritable = null;
        }

        foreach ( var attribute in typeSymbol.GetAttributes() )
        {
            switch ( attribute.AttributeClass?.Name )
            {
                case null:
                    continue;

                case nameof(InheritableAttribute):
                    this.IsInheritable = true;

                    break;

                case nameof(EditorExperienceAttribute):
                    if ( !attributeDeserializer.TryCreateAttribute<EditorExperienceAttribute>(
                            attribute,
                            diagnosticAdder,
                            out var editorExperienceAttribute ) )
                    {
                        this.HasError = true;
                    }
                    else
                    {
                        this.EditorExperienceOptions = this.EditorExperienceOptions.Override( editorExperienceAttribute.Options );
                    }

                    break;

                case nameof(LayersAttribute):
                    layers.AddRange(
                        attribute.ConstructorArguments[0]
                            .Values.Select( v => (string?) v.Value )
                            .Where( v => !string.IsNullOrEmpty( v ) ) );

                    break;

                case nameof(DescriptionAttribute):
                    this.Description = (string?) attribute.ConstructorArguments[0].Value;

                    break;

                case nameof(DisplayNameAttribute):
                    this.DisplayName = (string?) attribute.ConstructorArguments[0].Value ?? this.ShortName;

                    break;

                case nameof(RequireAspectWeaverAttribute):
                    this.WeaverType = attribute.ConstructorArguments[0].Value switch
                    {
                        string weaverTypeName => weaverTypeName,
                        ITypeSymbol weaverTypeSymbol => weaverTypeSymbol.GetReflectionFullName(),
                        var value => throw new InvalidOperationException(
                            $"Invalid value '{value?.ToString() ?? "null"}' for RequireAspectWeaverAttribute argument." )
                    };

                    break;
            }
        }

        this.Layers = layers.SelectAsImmutableArray( l => new AspectLayer( this, l ) );

        if ( this.EditorExperienceOptions.SuggestAsLiveTemplate.GetValueOrDefault() )
        {
            if ( !typeSymbol.HasDefaultConstructor() )
            {
                diagnosticAdder.Report(
                    GeneralDiagnosticDescriptors.LiveTemplateMustHaveDefaultConstructor.CreateRoslynDiagnostic(
                        typeSymbol.GetDiagnosticLocation(),
                        typeSymbol,
                        this ) );

                this.HasError = true;
            }
        }
    }

    private bool TryInitialize( IDiagnosticAdder diagnosticAdder, AspectDriverFactory aspectDriverFactory, CompilationContext compilationContext )
    {
        if ( this.HasError )
        {
            // Errors were reported during the instantiation of the class.

            return false;
        }

        // This must be called after Members is built and assigned.
        this._aspectDriver = aspectDriverFactory.GetAspectDriver( this );

        if ( this._prototypeAspectInstance != null )
        {
            this._eligibilityHelper = new EligibilityHelper( this._prototypeAspectInstance, this.ServiceProvider, this );

            // Call BuildEligibility for all relevant interface implementations.

            // Add additional rules defined by the driver.
            if ( this._aspectDriver is AspectDriver { EligibilityRule: { } eligibilityRule } )
            {
                this._eligibilityHelper.Add( typeof(IDeclaration), eligibilityRule );
            }

            if ( !this._eligibilityHelper.PopulateRules( diagnosticAdder, compilationContext ) )
            {
                return false;
            }
        }
        else
        {
            // Abstract aspect classes don't have eligibility because they cannot be applied.
        }

        // TODO: get all eligibility rules from the prototype instance and combine them into a single rule.

        return true;
    }

    /// <summary>
    /// Creates a new  <see cref="AspectInstance"/> from a custom attribute.
    /// </summary>
    internal AspectInstance CreateAspectInstanceFromAttribute(
        IAspect aspect,
        IDeclaration target,
        IAttribute attribute )
        => new( aspect, target, this, new AspectPredecessor( AspectPredecessorKind.Attribute, attribute ) );

    /// <summary>
    /// Creates a new <see cref="AspectInstance"/> by using the default constructor of the current class.
    /// This method is used by live templates.
    /// </summary>
    internal AspectInstance CreateAspectInstance( IDeclaration target, IAspect aspect, in AspectPredecessor predecessor )
        => new( aspect, target, this, predecessor );

    /// <summary>
    /// Creates an instance of the <see cref="AspectClass"/> class.
    /// </summary>
    internal static bool TryCreate(
        ProjectServiceProvider serviceProvider,
        INamedTypeSymbol aspectTypeSymbol,
        Type aspectReflectionType,
        AspectClass? baseAspectClass,
        CompileTimeProject? compileTimeProject,
        IDiagnosticAdder diagnosticAdder,
        ITemplateReflectionContext templateReflectionContext,
        AspectDriverFactory aspectDriverFactory,
        [NotNullWhen( true )] out AspectClass? aspectClass )
    {
        IAspect? prototype;

        if ( aspectTypeSymbol.IsAbstract )
        {
            prototype = null;
        }
        else if ( aspectTypeSymbol.IsGenericType )
        {
            diagnosticAdder.Report(
                GeneralDiagnosticDescriptors.GenericAspectTypeNotSupported.CreateRoslynDiagnostic(
                    aspectTypeSymbol.GetDiagnosticLocation(),
                    aspectTypeSymbol ) );

            aspectClass = null;

            return false;
        }
        else
        {
            var aspectInterfaceType = typeof(IAspect);

            if ( !aspectInterfaceType.IsAssignableFrom( aspectInterfaceType ) )
            {
                // This happens in case of a bug in assembly resolution
                // (typically AppDomain.AssemblyResolve event handler).
                throw new AssertionFailedException( "Assembly version mismatch." );
            }

            var untypedPrototype = FormatterServices.GetUninitializedObject( aspectReflectionType ).AssertNotNull();

            prototype = (IAspect) untypedPrototype;
        }

        aspectClass = new AspectClass(
            serviceProvider,
            aspectTypeSymbol,
            baseAspectClass,
            compileTimeProject,
            aspectReflectionType,
            prototype,
            diagnosticAdder,
            templateReflectionContext );

        if ( !aspectClass.TryInitialize( diagnosticAdder, aspectDriverFactory, templateReflectionContext.CompilationContext ) )
        {
            aspectClass = null;

            return false;
        }

        return true;
    }

    private static bool IsMethod( MethodKind methodKind )
        => methodKind switch
        {
            MethodKind.Constructor => false,
            MethodKind.StaticConstructor => false,
            MethodKind.AnonymousFunction => false,
            _ => true
        };

    private static bool IsConstructor( MethodKind methodKind )
        => methodKind switch
        {
            MethodKind.Constructor => true,
            MethodKind.StaticConstructor => true,
            _ => false
        };

    /// <summary>
    /// Determines the eligibility of a Roslyn symbol for the current aspect without constructing a <see cref="CompilationModel"/>
    /// for the symbol.
    /// </summary>
    public bool IsEligibleFast( ISymbol symbol )
    {
        var ourDeclarationInterface = symbol switch
        {
            IMethodSymbol method when IsMethod( method.MethodKind ) => method.MethodKind != MethodKind.LocalFunction ? typeof(IMethod) : null,
            IMethodSymbol method when IsConstructor( method.MethodKind ) => typeof(IConstructor),
            IPropertySymbol => typeof(IProperty),
            IEventSymbol => typeof(IEvent),
            IFieldSymbol => typeof(IField),
            ITypeSymbol { TypeKind: TypeKind.TypeParameter } => typeof(ITypeParameter),
            INamedTypeSymbol => typeof(INamedType),
            IParameterSymbol => typeof(IParameter),
            IAssemblySymbol => typeof(ICompilation),
            _ => null
        };

        if ( ourDeclarationInterface == null )
        {
            return false;
        }

        var aspectInterface = typeof(IAspect<>).MakeGenericType( ourDeclarationInterface );

        return aspectInterface.IsAssignableFrom( this.Type );
    }

    /// <summary>
    /// Gets the eligibility of a an aspect instance of the current aspect class. If the aspect type implements <see cref="IConditionallyInheritableAspect"/>,
    /// this method assumes that the aspect instance is inheritable. 
    /// </summary>
    public EligibleScenarios GetEligibility( IDeclaration obj ) => this.GetEligibility( obj, this.IsInheritable != false );

    /// <summary>
    /// Gets the eligibility of a an aspect instance of the current aspect class without when knowing whether the aspect instance is inheritable.
    /// </summary>
    public EligibleScenarios GetEligibility( IDeclaration obj, bool isInheritable )
    {
        return this._eligibilityHelper?.GetEligibility( obj, isInheritable ) ?? EligibleScenarios.All;
    }

    [Memo]
    private IReadOnlyCollection<IAspectClassImpl> DescendantClassesAndSelfImpl => this.SelectManyRecursive( x => x._childAspectClasses, includeRoot: true );

    public IReadOnlyCollection<IAspectClass> DescendantClassesAndSelf => this.DescendantClassesAndSelfImpl;

    IReadOnlyCollection<IAspectClassImpl> IAspectClassImpl.DescendantClassesAndSelf => this.DescendantClassesAndSelfImpl;

    public FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<IDeclaration> describedObject )
        => this._eligibilityHelper.AssertNotNull().GetIneligibilityJustification( requestedEligibility, describedObject );

    internal IAspect CreateDefaultInstance( CompilationContext compilationContext )
        => this._userCodeInvoker.Invoke(
            () => (IAspect) Activator.CreateInstance( this.Type ).AssertNotNull(),
            new UserCodeExecutionContext(
                this.ServiceProvider,
                UserCodeDescription.Create( "executing the default constructor of {0}", this ),
                compilationContext ) );

    public override string ToString() => this.FullName;
}
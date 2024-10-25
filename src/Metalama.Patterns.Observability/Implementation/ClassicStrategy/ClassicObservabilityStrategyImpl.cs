// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Utilities;
using Metalama.Patterns.Observability.Configuration;
using Metalama.Patterns.Observability.Implementation.DependencyAnalysis;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

internal sealed class ClassicObservabilityStrategyImpl : IObservabilityStrategy
{
    // The order in the array determines the priority.
    private static readonly string[] _onPropertyChangedMethodNames = ["OnPropertyChanged", "NotifyOfPropertyChange", "RaisePropertyChanged"];

    private readonly Promise<ObservabilityTemplateArgs> _templateArgs = new();
    private readonly ObservabilityOptions _commonOptions;
    private readonly ClassicObservabilityStrategyOptions _classicOptions;
    private readonly Dictionary<IFieldOrProperty, bool> _validateFieldOrPropertyResults = new();
    private readonly IMethod? _baseOnPropertyChangedInvocableMethod;
    private readonly IMethod? _baseOnPropertyChangedOverridableMethod;
    private readonly IMethod? _baseOnChildPropertyChangedMethod;
    private readonly IMethod? _baseOnObservablePropertyChangedMethod;
    private readonly bool _targetImplementsInpc;

    public Assets Assets { get; }

    // Useful to see when debugging:
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly bool _baseImplementsInpc;
    private readonly Promise<IMethod> _onPropertyChangedInvocableMethod = new();
    private readonly Promise<IMethod> _onPropertyChangedOverridableMethod = new();
    private readonly Promise<IMethod?> _onChildPropertyChangedMethod = new();
    private readonly Promise<IMethod?>? _onObservablePropertyChangedMethod;
    private readonly List<string> _propertyPathsForOnChildPropertyChangedMethod = [];
    private readonly List<string> _propertyNamesForOnObservablePropertyChangedMethod = [];
    private readonly Promise<ClassicObservableTypeInfo> _dependencyTypeNode = new();

    public InpcInstrumentationKindLookup InpcInstrumentationKindLookup { get; }

    public IAspectBuilder<INamedType> AspectBuilder { get; }

    public INamedType CurrentType => this.AspectBuilder.Target;

    public ClassicObservabilityStrategyImpl( IAspectBuilder<INamedType> aspectBuilder )
    {
        var target = aspectBuilder.Target;

        this.AspectBuilder = aspectBuilder;
        this.Assets = target.Compilation.Cache.GetOrAdd( _ => new Assets() );
        this.InpcInstrumentationKindLookup = new InpcInstrumentationKindLookup( this.CurrentType, this.Assets );
        this._commonOptions = this.CurrentType.Enhancements().GetOptions<ObservabilityOptions>();
        this._classicOptions = this.CurrentType.Enhancements().GetOptions<ClassicObservabilityStrategyOptions>();

        // TODO: Consider using BaseType.Definition where possible for better performance.

        this._baseImplementsInpc =
            target.BaseType != null && (
                target.BaseType.IsConvertibleTo( this.Assets.INotifyPropertyChanged )
                || (target.BaseType is { BelongsToCurrentProject: true }
                    && target.BaseType.Definition.Enhancements().HasAspect( typeof(ObservableAttribute) )));

        this._targetImplementsInpc = this._baseImplementsInpc || target.IsConvertibleTo( this.Assets.INotifyPropertyChanged );
        (this._baseOnPropertyChangedInvocableMethod, this._baseOnPropertyChangedOverridableMethod) = GetOnPropertyChangedMethods( target );
        this._baseOnChildPropertyChangedMethod = GetOnChildPropertyChangedMethod( target );
        this._baseOnObservablePropertyChangedMethod = GetOnObservablePropertyChangedMethod( target, this.Assets );

        var useOnObservablePropertyChangedMethod =
            this._classicOptions.EnableOnObservablePropertyChangedMethod == true &&
            (!target.IsSealed || this._baseOnObservablePropertyChangedMethod != null);

        if ( useOnObservablePropertyChangedMethod )
        {
            this._onObservablePropertyChangedMethod = new Promise<IMethod?>();
        }
    }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        if ( builder != this.AspectBuilder )
        {
            throw new ArgumentOutOfRangeException();
        }

        // Validate, maximising the coverage of diagnostic reporting.

        var v1 = this.ValidateBaseImplementation();
        var v2 = this.BuildAndValidateDependencyGraph();
        var v3 = this.ValidateRootAutoProperties();

        // Transform, if valid. By design, aim to minimise diagnostic reporting that only occurs during
        // the transform phase.
        if ( !v1 || !v2 || !v3 )
        {
            return;
        }

        this.IntroduceInterfaceIfRequired();

        // Introduce methods like UpdateA1B1()
        this.IntroduceUpdateMethods();

        this.ProcessAutoPropertiesAndReferencedFields();

        this.AddPropertyPathsForOnChildPropertyChangedMethodAttribute();

        if ( !this.TryIntroduceOnPropertyChangedMethod() )
        {
            return;
        }

        if ( !this.TryIntroduceOnChildPropertyChangedMethod() )
        {
            return;
        }

        if ( !this.TryIntroduceOnObservablePropertyChanged() )
        {
            return;
        }

        this._templateArgs.Value = new ObservabilityTemplateArgs(
            this._commonOptions,
            this._classicOptions,
            this.CurrentType,
            this.Assets,
            this.InpcInstrumentationKindLookup,
            this.ObservableTypeInfo,
            this._onObservablePropertyChangedMethod?.Value,
            this._onPropertyChangedInvocableMethod.Value,
            this._onChildPropertyChangedMethod.Value,
            this._baseOnPropertyChangedOverridableMethod,
            this._baseOnChildPropertyChangedMethod,
            this._baseOnObservablePropertyChangedMethod );
    }

    private bool ValidateRootAutoProperties()
    {
        // TODO: Add support for fields.

        // Root auto properties that are referenced by other properties will also be validated
        // when building the dependency graph. Here we make sure to cover unreferenced properties.
        // The ValidateFieldOrProperty method caches results and will not raise duplicate diagnostics,
        // so it's simplest to just run though all the root properties regardless of whether they
        // are covered by the dependency graph.

        var relevantProperties =
            this.CurrentType.Properties
                .Where(
                    p =>
                        p is { IsStatic: false, IsAutoPropertyOrField: true }
                        && !p.Attributes.Any( this.Assets.NotObservableAttribute ) );

        var allValid = true;

        foreach ( var p in relevantProperties )
        {
            allValid &= this.ValidateFieldOrPropertyIntrinsicCharacteristics( p );
        }

        return allValid;
    }

    private void AddPropertyPathsForOnChildPropertyChangedMethodAttribute()
    {
        // NB: The selection logic here must be kept in sync with the logic in the OnObservablePropertyChanged template.

        this._propertyPathsForOnChildPropertyChangedMethod.AddRange(
            this.ObservableTypeInfo.AllExpressions
                .Where(
                    n => n.InpcBaseHandling switch
                    {
                        InpcBaseHandling.OnObservablePropertyChanged when this._onObservablePropertyChangedMethod != null =>
                            true,
                        InpcBaseHandling.OnPropertyChanged when n.HasChildren => true,
                        _ => false
                    } )
                .Select( n => n.DottedPropertyPath ) );
    }

    private bool TryIntroduceOnPropertyChangedMethod()
    {
        var isOverride = this._baseOnPropertyChangedOverridableMethod != null;

        var template = this._baseOnPropertyChangedOverridableMethod == null
                       || this._baseOnPropertyChangedOverridableMethod.Parameters[0].Type.IsConvertibleTo( SpecialType.String )
            ? nameof(Templates.OnPropertyChangedString)
            : nameof(Templates.OnPropertyChangedObject);

        var result = this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
            .IntroduceMethod(
                this.CurrentType,
                template,
                IntroductionScope.Instance,
                isOverride ? OverrideStrategy.Override : OverrideStrategy.Fail,
                b =>
                {
                    if ( isOverride )
                    {
                        b.Name = this._baseOnPropertyChangedOverridableMethod!.Name;
                    }

                    if ( this.CurrentType.IsSealed )
                    {
                        b.Accessibility = this._baseOnPropertyChangedOverridableMethod?.Accessibility ?? Accessibility.Private;
                    }
                    else
                    {
                        b.Accessibility = this._baseOnPropertyChangedOverridableMethod?.Accessibility ?? Accessibility.Protected;
                        b.IsVirtual = !isOverride;
                    }
                },
                args: new { templateArgs = this._templateArgs } );

        if ( result.Outcome == AdviceOutcome.Error )
        {
            return false;
        }

        this._onPropertyChangedOverridableMethod.Value = result.Declaration;

        if ( this._baseOnPropertyChangedOverridableMethod == this._baseOnPropertyChangedInvocableMethod )
        {
            this._onPropertyChangedInvocableMethod.Value = result.Declaration;
        }
        else
        {
            this._onPropertyChangedInvocableMethod.Value = this._baseOnPropertyChangedInvocableMethod!;
        }

        // Ensure that all required fields are generated in advance of template execution.
        // The node selection logic mirrors that of the template's loops and conditions.

        if ( this._onObservablePropertyChangedMethod != null )
        {
            foreach ( var propertyPath in this.ObservableTypeInfo
                         .AllExpressions
                         .Where( n => n.InpcBaseHandling == InpcBaseHandling.OnObservablePropertyChanged ) )
            {
                _ = this.GetOrCreateHandlerField( propertyPath );
            }
        }

        foreach ( var node in this.ObservableTypeInfo
                     .AllExpressions
                     .Where( node => node.InpcBaseHandling is InpcBaseHandling.OnPropertyChanged && node.HasChildren ) )
        {
            _ = this.GetOrCreateHandlerField( node );
            _ = this.GetOrCreateLastValueField( node );
        }

        return true;
    }

    private bool TryIntroduceOnChildPropertyChangedMethod()
    {
        var nodesForOnChildPropertyChanged =
            this.ObservableTypeInfo
                .AllExpressions
                .Where( n => n.Depth > 0 )
                .Where(
                    node =>
                    {
                        var rootPropertyNode = node.Root;

                        if ( rootPropertyNode.ReferencedFieldOrProperty.DeclaringType == this.CurrentType )
                        {
                            return false;
                        }

                        var firstAncestorWithNotNoneHandling = node.Ancestors().FirstOrDefault( n => n.InpcBaseHandling != InpcBaseHandling.None );

                        if ( firstAncestorWithNotNoneHandling != null )
                        {
                            switch ( firstAncestorWithNotNoneHandling.InpcBaseHandling )
                            {
                                case InpcBaseHandling.OnObservablePropertyChanged when this._onObservablePropertyChangedMethod != null:
                                    return false;

                                case InpcBaseHandling.OnChildPropertyChanged when node.Depth - firstAncestorWithNotNoneHandling.Depth > 1:
                                    return false;

                                case InpcBaseHandling.OnPropertyChanged:
                                    return false;
                            }
                        }

                        return true;
                    } )
                .ToList();

        if ( this._propertyPathsForOnChildPropertyChangedMethod.Count == 0 && nodesForOnChildPropertyChanged.Count == 0 )
        {
            // This method is not necessary.
            this._onChildPropertyChangedMethod.Value = null;

            return true;
        }

        var isOverride = this._baseOnChildPropertyChangedMethod != null;

        var result = this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
            .IntroduceMethod(
                this.CurrentType,
                nameof(Templates.OnChildPropertyChanged),
                IntroductionScope.Instance,
                isOverride ? OverrideStrategy.Override : OverrideStrategy.Fail,
                b =>
                {
                    if ( this._propertyPathsForOnChildPropertyChangedMethod.Count > 0 )
                    {
                        b.AddAttribute(
                            AttributeConstruction.Create(
                                this.Assets.InvokedForAttribute,
                                this._propertyPathsForOnChildPropertyChangedMethod.OrderBy( s => s ).ToArray() ) );
                    }

                    if ( isOverride )
                    {
                        b.Name = this._baseOnChildPropertyChangedMethod!.Name;
                    }

                    if ( this.CurrentType.IsSealed )
                    {
                        b.Accessibility = isOverride ? this._baseOnChildPropertyChangedMethod!.Accessibility : Accessibility.Private;
                    }
                    else
                    {
                        b.Accessibility = isOverride ? this._baseOnChildPropertyChangedMethod!.Accessibility : Accessibility.Protected;
                        b.IsVirtual = !isOverride;
                    }
                },
                args: new { templateArgs = this._templateArgs, nodesForOnChildPropertyChanged } );

        if ( result.Outcome != AdviceOutcome.Error )
        {
            this._onChildPropertyChangedMethod.Value = result.Declaration;

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool TryIntroduceOnObservablePropertyChanged()
    {
        if ( this._onObservablePropertyChangedMethod == null )
        {
            return true;
        }

        var nodesProcessedByOnObservablePropertyChanged = this.ObservableTypeInfo.AllExpressions
            .Where( n => n.InpcBaseHandling == InpcBaseHandling.OnObservablePropertyChanged )
            .ToList();

        if ( nodesProcessedByOnObservablePropertyChanged.Count == 0 && this._propertyNamesForOnObservablePropertyChangedMethod.Count == 0 )
        {
            // We don't create the method because it would not do anything and it would not be invoked.
            // Note that both conditions need to be true, because we still need the method because of the derived types.
            this._onObservablePropertyChangedMethod.Value = null;

            return true;
        }

        var isOverride = this._baseOnObservablePropertyChangedMethod != null;

        var result = this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
            .IntroduceMethod(
                this.CurrentType,
                nameof(Templates.OnObservablePropertyChanged),
                IntroductionScope.Instance,
                isOverride ? OverrideStrategy.Override : OverrideStrategy.Fail,
                b =>
                {
                    if ( this._propertyNamesForOnObservablePropertyChangedMethod.Count > 0 )
                    {
                        b.AddAttribute(
                            AttributeConstruction.Create(
                                this.Assets.InvokedForAttribute,
                                this._propertyNamesForOnObservablePropertyChangedMethod.OrderBy( s => s ).ToArray() ) );
                    }

                    if ( isOverride )
                    {
                        b.Name = this._baseOnObservablePropertyChangedMethod!.Name;
                    }

                    if ( this.CurrentType.IsSealed )
                    {
                        b.Accessibility = isOverride ? this._baseOnObservablePropertyChangedMethod!.Accessibility : Accessibility.Private;
                    }
                    else
                    {
                        b.Accessibility = isOverride ? this._baseOnObservablePropertyChangedMethod!.Accessibility : Accessibility.Protected;
                        b.IsVirtual = !isOverride;
                    }
                },
                args: new { templateArgs = this._templateArgs, nodesProcessedByOnObservablePropertyChanged } );

        if ( result.Outcome == AdviceOutcome.Error )
        {
            return false;
        }

        this._onObservablePropertyChangedMethod.Value = result.Declaration;

        return true;
    }

    private bool ValidateBaseImplementation()
    {
        var isValid = true;

        if ( this._targetImplementsInpc )
        {
            // For clarity, we report just one error.
            if ( this._baseOnPropertyChangedOverridableMethod == null )
            {
                this.AspectBuilder.Diagnostics.Report(
                    DiagnosticDescriptors.ErrorClassImplementsInpcButDoesNotDefineOnOverridablePropertyChanged
                        .WithArguments( this.CurrentType ) );

                isValid = false;
            }
            else if ( this._baseOnPropertyChangedInvocableMethod == null )
            {
                this.AspectBuilder.Diagnostics.Report(
                    DiagnosticDescriptors.ErrorClassImplementsInpcButDoesNotDefineOnInvocablePropertyChanged
                        .WithArguments( this.CurrentType ) );

                isValid = false;
            }
        }

        return isValid;
    }

    private void IntroduceInterfaceIfRequired()
    {
        if ( !this._targetImplementsInpc )
        {
            this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
                .ImplementInterface( this.CurrentType, this.Assets.INotifyPropertyChanged );
        }
    }

    private void IntroduceUpdateMethods()
    {
        var allNodesDepthFirst = this.ObservableTypeInfo
            .AllExpressions
            .OrderByDescending( x => x.Depth )
            .ThenBy( x => x.DottedPropertyPath )
            .ToList();

        // Process all nodes in depth-first, leaf-to-root order, creating necessary update methods as we go.
        // The order is important, so that parent nodes test if child node methods were necessary and invoke them.

        foreach ( var node in allNodesDepthFirst )
        {
            if ( !node.HasChildren || node.IsRoot )
            {
                // Leaf nodes and root properties should never have update methods.
                node.UpdateMethod.Value = null;

                continue;
            }

            IMethod? thisUpdateMethod = null;

            // Don't add fields and update methods for properties handled by base, or for root properties of the target type, or for properties of types that don't implement INPC.
            if ( node.InpcInstrumentationKind.IsImplemented() == true
                 && !this.HasInheritedOnChildPropertyChangedPropertyPath( node.DottedPropertyPath ) )
            {
                var lastValueField = this.GetOrCreateLastValueField( node );
                var onPropertyChangedHandlerField = this.GetOrCreateHandlerField( node );

                var methodName = this.GetAndReserveUnusedMemberName( $"Update{node.ContiguousPropertyPath}" );

                var accessChildExprBuilder = new ExpressionBuilder();

#if NETCOREAPP
#pragma warning disable CA1307 // Specify StringComparison for clarity [Justification: code must remain compatible with netstandard2.0]
#endif
                accessChildExprBuilder.AppendVerbatim( node.DottedPropertyPath.Replace( ".", "?." ) );
#if NETCOREAPP
#pragma warning restore CA1307 // Specify StringComparison for clarity
#endif

                var accessChildExpression = accessChildExprBuilder.ToExpression();

                var introduceUpdateChildPropertyMethodResult = this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
                    .IntroduceMethod(
                        this.CurrentType,
                        nameof(Templates.UpdateChildInpcProperty),
                        IntroductionScope.Instance,
                        OverrideStrategy.Fail,
                        b =>
                        {
                            b.Name = methodName;
                            b.Accessibility = Accessibility.Private;
                        },
                        args: new
                        {
                            templateArgs = this._templateArgs,
                            node,
                            accessChildExpression,
                            lastValueField,
                            onPropertyChangedHandlerField
                        } );

                thisUpdateMethod = introduceUpdateChildPropertyMethodResult.Declaration;

                // This type will raise OnChildPropertyChanged for the current node, let derived types know.
                this._propertyPathsForOnChildPropertyChangedMethod.Add( node.DottedPropertyPath );
            }

            node.UpdateMethod.Value = thisUpdateMethod;
        }
    }

    private void ProcessAutoPropertiesAndReferencedFields()
    {
        var target = this.CurrentType;

        // Override all auto properties, and only those fields that are referenced in the graph.

        var properties =
            target.Properties
                .Where( p => p is { IsStatic: false, IsImplicitlyDeclared: false, Writeability: not Writeability.None } )
                .Select( p => (ClassicObservablePropertyInfo) this.ObservableTypeInfo.GetOrAddProperty( p ) )
                .Concat(
                    target.Fields
                        .Where( f => f is { IsStatic: false, IsImplicitlyDeclared: false } )
                        .Select( p => (ClassicObservablePropertyInfo) this.ObservableTypeInfo.GetOrAddProperty( p ) ) )
                .Where( node => !node.FieldOrProperty.Attributes.Any( this.Assets.NotObservableAttribute ) )
                .ToList();

        foreach ( var propertyInfo in properties )
        {
            var fieldOrProperty = propertyInfo.FieldOrProperty;
            var propertyTypeInstrumentationKind = this.InpcInstrumentationKindLookup.Get( fieldOrProperty.Type );
            var propertyTypeImplementsInpc = propertyTypeInstrumentationKind.IsImplemented() == true;

            // We don't report writes to non-INPC members, if the member can only be written from the constructor, or if it's an init-only property.
            var propertyIsWriteable = fieldOrProperty.Writeability is Writeability.All;

            switch ( fieldOrProperty.Type.IsReferenceType )
            {
                case null or true:

                    if ( propertyTypeImplementsInpc )
                    {
                        var hasDependentProperties = propertyInfo.RootReferenceNode.HasAnyReferencingProperties;

                        IField? handlerField = null;
                        IMethod? subscribeMethod = null;

                        if ( hasDependentProperties )
                        {
                            var rootReference = propertyInfo.RootReferenceNode;
                            handlerField = this.GetOrCreateHandlerField( rootReference );

                            if ( !this.TryGetOrCreateRootPropertySubscribeMethod( propertyInfo, out subscribeMethod ) )
                            {
                                return;
                            }

                            if ( fieldOrProperty.DeclarationKind == DeclarationKind.Property )
                            {
                                this._propertyPathsForOnChildPropertyChangedMethod.Add( fieldOrProperty.Name );
                                this._propertyNamesForOnObservablePropertyChangedMethod.Add( fieldOrProperty.Name );
                            }

                            if ( fieldOrProperty.InitializerExpression != null )
                            {
                                this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
                                    .AddInitializer(
                                        this.CurrentType,
                                        nameof(Templates.SubscribeInitializer),
                                        InitializerKind.BeforeInstanceConstructor,
                                        args: new { fieldOrProperty, subscribeMethod } );
                            }
                        }
                        else
                        {
                            if ( fieldOrProperty.DeclarationKind == DeclarationKind.Property )
                            {
                                this._propertyNamesForOnObservablePropertyChangedMethod.Add( fieldOrProperty.Name );
                            }
                        }

                        this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
                            .OverrideAccessors(
                                fieldOrProperty,
                                setTemplate: nameof(Templates.OverrideInpcRefTypePropertySetter),
                                args: new { templateArgs = this._templateArgs, handlerField, propertyInfo, subscribeMethod } );
                    }
                    else
                    {
                        if ( propertyIsWriteable )
                        {
                            var comparer = fieldOrProperty.Type.IsReferenceType == null
                                ? EqualityComparisonKind.DefaultEqualityComparer
                                : EqualityComparisonKind.ReferenceEquals;

                            this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
                                .OverrideAccessors(
                                    fieldOrProperty,
                                    setTemplate: nameof(Templates.OverrideUninstrumentedTypePropertySetter),
                                    args: new { templateArgs = this._templateArgs, propertyInfo, compareUsing = comparer, propertyTypeInstrumentationKind } );
                        }
                    }

                    break;

                case false:

                    if ( propertyIsWriteable )
                    {
                        var comparisonKind = fieldOrProperty.Type is INamedType nt && nt.SpecialType != SpecialType.ValueTask_T
                            ? EqualityComparisonKind.EqualityOperator
                            : EqualityComparisonKind.DefaultEqualityComparer;

                        this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
                            .OverrideAccessors(
                                fieldOrProperty,
                                setTemplate: nameof(Templates.OverrideUninstrumentedTypePropertySetter),
                                args: new { templateArgs = this._templateArgs, propertyInfo, compareUsing = comparisonKind, propertyTypeInstrumentationKind } );
                    }

                    break;
            }
        }
    }

    private HashSet<string>? _inheritedOnChildPropertyChangedPropertyPaths;

    public bool HasInheritedOnChildPropertyChangedPropertyPath( string parentPropertyPath )
    {
        this._inheritedOnChildPropertyChangedPropertyPaths ??=
            BuildPropertyPathLookup( GetPropertyPaths( this.Assets.InvokedForAttribute, this._baseOnChildPropertyChangedMethod ) );

        return this._inheritedOnChildPropertyChangedPropertyPaths.Contains( parentPropertyPath );
    }

    private HashSet<string>? _inheritedOnObservablePropertyChangedPropertyNames;

    public bool HasInheritedOnObservablePropertyChangedProperty( string propertyName )
    {
        this._inheritedOnObservablePropertyChangedPropertyNames ??=
            BuildPropertyPathLookup(
                GetPropertyPaths(
                    this.Assets.InvokedForAttribute,
                    this._baseOnObservablePropertyChangedMethod ) );

        return this._inheritedOnObservablePropertyChangedPropertyNames.Contains( propertyName );
    }

    /// <summary>
    /// Builds and validates the dependency graph (including validating dependency analysis).
    /// </summary>
    /// <returns>Success. <see langword="true"/> if the graph was built without diagnostic errors, otherwise <see langword="false"/>.</returns>
    private bool BuildAndValidateDependencyGraph()
    {
        var graphBuildingContext = new ClassicGraphBuildingContext( this );
        var graphBuilder = new ClassicDependencyGraphBuilder( graphBuildingContext, this.CurrentType );

        var typeNode = (ClassicObservableTypeInfo) graphBuilder.GetDependencyGraph(
            this.CurrentType,
            graphBuildingContext,
            cancellationToken: this.AspectBuilder.CancellationToken );

        var hasErrors = graphBuildingContext.HasReportedErrors;

        foreach ( var property in typeNode.Properties )
        {
            hasErrors |= !this.ValidateFieldOrPropertyIntrinsicCharacteristics( property.FieldOrProperty );
        }

        this._dependencyTypeNode.Value = typeNode;

        return !hasErrors;
    }

    private ClassicObservableTypeInfo ObservableTypeInfo => this._dependencyTypeNode.Value;

    private IField GetOrCreateLastValueField( ClassicObservableExpression node )
    {
        if ( !node.LastValueField.IsResolved )
        {
            var lastValueFieldName = this.GetAndReserveUnusedMemberName( $"_last{node.ContiguousPropertyPath}" );

            var introduceLastValueFieldResult = this.AspectBuilder.Advice.IntroduceField(
                this.CurrentType,
                lastValueFieldName,
                node.ReferencedFieldOrProperty.Type.ToNullable(),
                IntroductionScope.Instance,
                OverrideStrategy.Fail,
                b => b.Accessibility = Accessibility.Private );

            node.LastValueField.Value = introduceLastValueFieldResult.Declaration;
        }

        return node.LastValueField.Value;
    }

    private IField GetOrCreateHandlerField( ClassicObservableExpression node )
    {
        if ( !node.HandlerField.IsResolved )
        {
            var handlerFieldName = this.GetAndReserveUnusedMemberName( $"_handle{node.ContiguousPropertyPath}PropertyChanged" );

            var introduceHandlerFieldResult = this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
                .IntroduceField(
                    this.CurrentType,
                    handlerFieldName,
                    this.Assets.NullablePropertyChangedEventHandler,
                    IntroductionScope.Instance,
                    OverrideStrategy.Fail,
                    b => b.Accessibility = Accessibility.Private );

            node.HandlerField.Value = introduceHandlerFieldResult.Declaration;
        }

        return node.HandlerField.Value;
    }

    private bool TryGetOrCreateRootPropertySubscribeMethod( ClassicObservablePropertyInfo propertyInfo, out IMethod? subscribeMethod )
    {
        var referenceNode = propertyInfo.RootReferenceNode;

        if ( !referenceNode.SubscribeMethod.IsResolved )
        {
            var handlerField = this.GetOrCreateHandlerField( referenceNode );

            var subscribeMethodName = this.GetAndReserveUnusedMemberName( $"SubscribeTo{referenceNode.ContiguousPropertyPath}" );

            var result = this.AspectBuilder.Advice.WithTemplateProvider( Templates.Provider )
                .IntroduceMethod(
                    this.CurrentType,
                    nameof(Templates.SubscribeTo),
                    IntroductionScope.Instance,
                    OverrideStrategy.Fail,
                    b =>
                    {
                        b.Name = subscribeMethodName;
                        b.Accessibility = Accessibility.Private;
                    },
                    args: new { TValue = propertyInfo.FieldOrProperty.Type, templateArgs = this._templateArgs, propertyInfo, handlerField } );

            if ( result.Outcome != AdviceOutcome.Error )
            {
                referenceNode.SubscribeMethod.Value = result.Declaration;
            }
            else
            {
                subscribeMethod = null;

                return false;
            }
        }

        subscribeMethod = referenceNode.SubscribeMethod.Value;

        return true;
    }

    private HashSet<string>? _existingMemberNames;

    /// <summary>
    /// Gets an unused member name for the target type by adding an numeric suffix until an unused name is found.
    /// </summary>
    private string GetAndReserveUnusedMemberName( string desiredName )
    {
        this._existingMemberNames ??=
        [
            ..((IEnumerable<INamedDeclaration>) this.CurrentType.AllMembers()).Concat( this.CurrentType.Types ).Select( m => m.Name )
        ];

        if ( this._existingMemberNames.Add( desiredName ) )
        {
            return desiredName;
        }
        else
        {
            // ReSharper disable once BadSemicolonSpaces
            for ( var i = 1; /* Intentionally empty */; i++ )
            {
                var adjustedName = $"{desiredName}_{i}";

                if ( this._existingMemberNames.Add( adjustedName ) )
                {
                    return adjustedName;
                }
            }
        }
    }

    private static HashSet<string> BuildPropertyPathLookup( IEnumerable<string>? propertyPaths ) => propertyPaths == null ? [] : [..propertyPaths];

    [return: NotNullIfNotNull( nameof(method) )]
    private static IEnumerable<string>? GetPropertyPaths( INamedType attributeType, IMethod? method, bool includeInherited = true )
    {
        // NB: Assumes that attributeType instances will always be constructed with one arg of type string[].

        if ( method == null )
        {
            return null;
        }

        return includeInherited
            ? EnumerableExtensions.SelectRecursive( method, m => m.OverriddenMethod ).SelectMany( m => GetPropertyPathsCore( attributeType, m ) )
            : GetPropertyPathsCore( attributeType, method );

        static IEnumerable<string> GetPropertyPathsCore( INamedType attributeType, IMethod method )
            => method.Attributes
                .OfAttributeType( attributeType )
                .SelectMany( a => a.ConstructorArguments[0].Values.Select( k => (string?) k.Value ) )
                .Where( s => !string.IsNullOrWhiteSpace( s ) )!;
    }

    internal static (IMethod? Invocable, IMethod? Overridable) GetOnPropertyChangedMethods( INamedType type )
    {
        // Select methods of correct name, accessibility, and return type.
        var methodsOfCorrectName = type.AllMethods
            .Where(
                m => m is
                {
                    IsStatic: false,
                    Accessibility: Accessibility.Public or Accessibility.Protected,
                    ReturnType.SpecialType: SpecialType.Void
                } )
            .Select( m => (MethodNameIndex: Array.IndexOf( _onPropertyChangedMethodNames, m.Name ), Method: m) )
            .Where( x => x.MethodNameIndex >= 0 )
            .ToList();

        // In case of inheritance, we must have a method that accepts a string as a parameter, and
        // this is the method we use to notify changes.
        var invocableMethod = methodsOfCorrectName
            .OrderBy( x => x.MethodNameIndex )
            .FirstOrDefault( m => m.Method.Parameters is [{ Type.SpecialType: SpecialType.String }] )
            .Method;

        // We must also have a method that we are able to override, and here we support either string or PropertyChangedEventArgs. 
        // We give priority to PropertyChangedEventArgs.
        var overridableMethod = methodsOfCorrectName.Where(
                m =>
                    (m.Method.IsVirtual || m.Method.IsOverride)
                    && !m.Method.IsSealed
                    && m.Method.Parameters[0].Type is { SpecialType: SpecialType.String } or INamedType { Name: nameof(PropertyChangedEventArgs) } )
            .OrderBy( m => m.Method.Parameters[0].Type is { SpecialType: SpecialType.String } ? 0 : 1 )
            .ThenBy( m => m.MethodNameIndex )
            .FirstOrDefault()
            .Method;

        return (invocableMethod, overridableMethod);
    }

    internal static IMethod? GetOnChildPropertyChangedMethod( INamedType type )
        => type.AllMethods.SingleOrDefault(
            m =>
                !m.IsStatic
                && m is
                {
                    Name: "OnChildPropertyChanged",
                    Accessibility: Accessibility.Public or Accessibility.Protected,
                    ReturnType.SpecialType: SpecialType.Void,
                    Parameters: [{ Type.SpecialType: SpecialType.String }, { Type.SpecialType: SpecialType.String }]
                } );

    internal static IMethod? GetOnObservablePropertyChangedMethod( INamedType type, Assets assets )
        => type.AllMethods.SingleOrDefault(
            m =>
                !m.IsStatic
                && m is
                {
                    Name: "OnObservablePropertyChanged",
                    Accessibility: Accessibility.Public or Accessibility.Protected,
                    ReturnType.SpecialType: SpecialType.Void,
                    Parameters: [{ Type.SpecialType: SpecialType.String }, { } p1, { } p2]
                }
                && p1.Type == assets.NullableINotifyPropertyChanged
                && p2.Type == assets.NullableINotifyPropertyChanged );

    /// <summary>
    /// Validates the the intrinsic characteristics of the given <see cref="IFieldOrProperty"/>, reporting diagnostics if applicable. 
    /// The result is cached so that diagnostics are not repeated.
    /// </summary>
    /// <returns><see langword="true"/> if valid, or <see langword="false"/> if invalid.</returns>
    private bool ValidateFieldOrPropertyIntrinsicCharacteristics( IFieldOrProperty fieldOrProperty )
    {
        if ( !this._validateFieldOrPropertyResults.TryGetValue( fieldOrProperty, out var result ) )
        {
            result = IsValid( fieldOrProperty );
            this._validateFieldOrPropertyResults[fieldOrProperty] = result;
        }

        return result;

        bool IsValid( IFieldOrProperty fp )
        {
            var typeImplementsInpc =
                this.InpcInstrumentationKindLookup.Get( fp.Type ).IsImplemented() == true;

            var isValid = true;

            if ( fp.Type.IsReferenceType == false && typeImplementsInpc )
            {
                this.AspectBuilder.Diagnostics.Report(
                    DiagnosticDescriptors.ErrorFieldOrPropertyTypeIsStructImplementingInpc.WithArguments( (fp.DeclarationKind, fp, fp.Type) ),
                    fp );

                isValid = false;
            }

            if ( fp.IsVirtual )
            {
                this.AspectBuilder.Diagnostics.Report(
                    DiagnosticDescriptors.ErrorVirtualMemberIsNotSupported.WithArguments( (fp.DeclarationKind, fp) ),
                    fp );

                isValid = false;
            }

            if ( fp.IsNew )
            {
                this.AspectBuilder.Diagnostics.Report(
                    DiagnosticDescriptors.ErrorNewMemberIsNotSupported.WithArguments( (fp.DeclarationKind, fp) ),
                    fp );

                isValid = false;
            }

            return isValid;
        }
    }

    public InpcInstrumentationKind GetInpcInstrumentationKind( IType fieldOrPropertyType ) => this.InpcInstrumentationKindLookup.Get( fieldOrPropertyType );
}
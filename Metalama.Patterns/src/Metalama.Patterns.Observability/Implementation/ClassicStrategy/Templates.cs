// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Code.SyntaxBuilders;
using System.ComponentModel;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

[CompileTime]
internal sealed class Templates : ITemplateProvider
{
    internal static TemplateProvider Provider { get; } = TemplateProvider.FromInstance( new Templates() );

    private Templates() { }

    private static void CompileTimeThrow( Exception e ) => throw e;

    // ReSharper disable once EventNeverSubscribedTo.Global
    [InterfaceMember]
    public event PropertyChangedEventHandler? PropertyChanged;

    [Template]
    internal static void OverrideInpcRefTypePropertySetter(
        dynamic? value,
        [CompileTime] ObservabilityTemplateArgs templateArgs,
        [CompileTime] IField? handlerField,
        [CompileTime] IMethod? subscribeMethod,
        [CompileTime] ClassicObservablePropertyInfo propertyInfo )
    {
        var rootReference = propertyInfo.RootReferenceNode;

        meta.DebugBreak();

        var eventRequiresCast = templateArgs.InpcInstrumentationKindLookup.Get( propertyInfo.FieldOrProperty.Type ).RequiresCast();

        if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity > 0 )
        {
            meta.InsertComment( "Template: " + nameof(OverrideInpcRefTypePropertySetter) );

            if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity > 1 )
            {
                meta.InsertComment(
                    "Dependency graph (current node highlighted if defined):",
                    "\n" + templateArgs.ObservableTypeInfo.ToString( rootReference ) );
            }
        }

        var isReadOnly = propertyInfo.FieldOrProperty.Writeability != Writeability.All;

        if ( !ReferenceEquals( value, meta.Target.FieldOrProperty.Value ) )
        {
            if ( handlerField != null || templateArgs.OnObservablePropertyChangedMethod != null )
            {
                var oldValue = meta.Target.FieldOrProperty.Value;

                if ( handlerField != null )
                {
                    if ( oldValue != null )
                    {
                        if ( eventRequiresCast )
                        {
                            ((INotifyPropertyChanged) oldValue).PropertyChanged -= handlerField.Value;
                        }
                        else
                        {
                            oldValue.PropertyChanged -= handlerField.Value;
                        }
                    }
                }

                meta.Target.FieldOrProperty.Value = value;

                if ( !isReadOnly && templateArgs.OnObservablePropertyChangedMethod != null )
                {
                    templateArgs.OnObservablePropertyChangedMethod.With( InvokerOptions.Final )
                        .Invoke( meta.Target.FieldOrProperty.Name, oldValue, value );
                }
            }
            else
            {
                meta.Target.FieldOrProperty.Value = value;
            }

            if ( !isReadOnly )
            {
                // Update methods will deal with notifications - * for those children which have update methods *
                foreach ( var method in rootReference.ChildUpdateMethods )
                {
                    method.With( InvokerOptions.Final ).Invoke();
                }

                // Notify refs to the current node and any children without an update method.
                foreach ( var r in GetReferencingProperties( templateArgs, rootReference, excludeChildrenWithUpdateMethod: true ) )
                {
                    templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( r.Name );
                }

                if ( propertyInfo.FieldOrProperty.DeclarationKind != DeclarationKind.Field
                     && meta.Target.FieldOrProperty.Accessibility != Accessibility.Private )
                {
                    templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( meta.Target.FieldOrProperty.Name );
                }
            }

#pragma warning disable IDE0031 // Use null propagation
            if ( subscribeMethod != null )
            {
                subscribeMethod.Invoke( value );
            }
#pragma warning restore IDE0031 // Use null propagation
        }
    }

    [Template]
    internal static void SubscribeTo<[CompileTime] TValue>(
        TValue? value,
        [CompileTime] ObservabilityTemplateArgs templateArgs,
        [CompileTime] ClassicObservablePropertyInfo propertyInfo,
        [CompileTime] IField handlerField )
        where TValue : class, INotifyPropertyChanged
    {
        var eventRequiresCast = templateArgs.InpcInstrumentationKindLookup.Get( propertyInfo.FieldOrProperty.Type ).RequiresCast();

        if ( value != null )
        {
            handlerField.Value ??= (PropertyChangedEventHandler) HandlePropertyChanged;

            if ( eventRequiresCast )
            {
#pragma warning disable IDE0004

                // ReSharper disable once RedundantCast
                ((INotifyPropertyChanged) value).PropertyChanged += handlerField.Value;
#pragma warning restore IDE0004
            }
            else
            {
                value.PropertyChanged += handlerField.Value;
            }
        }

        // -----------------------------------------------------------------------
        //                Local Function: OnChildPropertyChanged
        // -----------------------------------------------------------------------

        // ReSharper disable once LocalFunctionHidesMethod
        void HandlePropertyChanged( object? sender, PropertyChangedEventArgs e )
        {
            // We use WithNullability to work around a bug in Metalama.Framework (perhaps rather in Roslyn) that randomly gives no
            // nullability information for 'e'.
            HandleChildPropertyChangedDelegateBody( templateArgs, propertyInfo.RootReferenceNode, ExpressionFactory.Capture( e ).WithNullability( false ) );
        }
    }

    [Template]
    internal static void SubscribeInitializer(
        [CompileTime] IFieldOrProperty fieldOrProperty,
        [CompileTime] IMethod subscribeMethod )
    {
        subscribeMethod.Invoke( fieldOrProperty.Value );
    }

    [Template]
    internal static void UpdateChildInpcProperty(
        [CompileTime] ObservabilityTemplateArgs templateArgs,
        [CompileTime] ClassicObservableExpression node,
        [CompileTime] IExpression accessChildExpression,
        [CompileTime] IField lastValueField,
        [CompileTime] IField onPropertyChangedHandlerField )
    {
        if ( node.IsRoot )
        {
            CompileTimeThrow( new InvalidOperationException( $"{nameof(UpdateChildInpcProperty)} template must not be called on a root property node." ) );
        }

        if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 0 )
        {
            meta.InsertComment( "Template: " + nameof(UpdateChildInpcProperty) );

            if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 1 )
            {
                meta.InsertComment( "Dependency graph (current node highlighted if defined):", "\n" + templateArgs.ObservableTypeInfo.ToString( node ) );
            }
        }

        var newValue = accessChildExpression.Value;

        if ( !ReferenceEquals( newValue, lastValueField.Value ) )
        {
            if ( !ReferenceEquals( lastValueField.Value, null ) )
            {
                lastValueField.Value!.PropertyChanged -= onPropertyChangedHandlerField.Value;
            }

            if ( newValue != null )
            {
                onPropertyChangedHandlerField.Value ??= (PropertyChangedEventHandler) HandleChildPropertyChanged;
                newValue.PropertyChanged += onPropertyChangedHandlerField.Value;

                // -----------------------------------------------------------------------
                //                Local Function: OnChildPropertyChanged
                // -----------------------------------------------------------------------

                // ReSharper disable once LocalFunctionHidesMethod
                void HandleChildPropertyChanged( object? sender, PropertyChangedEventArgs e )
                {
                    HandleChildPropertyChangedDelegateBody( templateArgs, node, ExpressionFactory.Capture( e ) );
                }
            }

            lastValueField.Value = newValue;

            // Update methods will deal with notifications - * for those children which have update methods *
            foreach ( var method in node.ChildUpdateMethods )
            {
                method.With( InvokerOptions.Final ).Invoke();
            }

            // Notify refs to the current node and any children without an update method.
            foreach ( var r in GetReferencingProperties( templateArgs, node, excludeChildrenWithUpdateMethod: true ) )
            {
                templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( r.Name );
            }

            var parentNode = (ClassicObservableExpression) node.Parent!;

            if ( parentNode.ReferencedFieldOrProperty.Accessibility != Accessibility.Private )
            {
                // Don't notify if we're joining on to existing NotifyChildPropertyChanged support from a base type, or we'll be stuck in a loop.
                if ( parentNode.InpcBaseHandling != InpcBaseHandling.OnChildPropertyChanged )
                {
                    templateArgs.OnChildPropertyChangedMethod!.With( InvokerOptions.Final ).Invoke( parentNode.DottedPropertyPath, node.Name );
                }
                else if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 0 )
                {
                    meta.InsertComment(
                        $"Not calling OnChildPropertyChanged('{parentNode.DottedPropertyPath}','{node.Name}') because a base type already provides OnChildPropertyChanged support for the parent property." );
                }
            }
            else
            {
                meta.InsertComment( $"Not calling OnChildPropertyChanged for private parent field or property '{parentNode.DottedPropertyPath}'." );
            }
        }
    }

    [Template]
    internal static void OverrideUninstrumentedTypePropertySetter(
        dynamic? value,
        [CompileTime] ObservabilityTemplateArgs templateArgs,
        [CompileTime] ClassicObservablePropertyInfo? propertyInfo,
        [CompileTime] EqualityComparisonKind compareUsing,
        [CompileTime] InpcInstrumentationKind propertyTypeInstrumentationKind )
    {
        if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 0 )
        {
            meta.InsertComment( "Template: " + nameof(OverrideUninstrumentedTypePropertySetter) );

            if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 1 )
            {
                meta.InsertComment(
                    "Dependency graph (current node highlighted if defined):",
                    "\n" + templateArgs.ObservableTypeInfo.ToString( propertyInfo?.RootReferenceNode ) );
            }
        }

        if ( propertyTypeInstrumentationKind == InpcInstrumentationKind.Unknown )
        {
            meta.InsertComment(
                "Warning: the type of this property could not be analysed at design time, so it has been treated",
                "as not implementing INotifyPropertyChanged. Code generated at compile time may differ." );
        }

        var compareExpr = compareUsing switch
        {
            EqualityComparisonKind.EqualityOperator => (IExpression) (meta.Target.FieldOrProperty.Value != value),
            EqualityComparisonKind.ThisEquals => (IExpression) !meta.Target.FieldOrProperty.Value!.Equals( value ),
            EqualityComparisonKind.ReferenceEquals => (IExpression) !ReferenceEquals( value, meta.Target.FieldOrProperty.Value ),
            EqualityComparisonKind.DefaultEqualityComparer => (IExpression)
                !templateArgs.Assets.GetDefaultEqualityComparerForType( meta.Target.FieldOrProperty.Type ).Value!.Equals(
                    value,
                    meta.Target.FieldOrProperty.Value ),
            _ => null
        };

        var isField = propertyInfo?.FieldOrProperty.DeclarationKind == DeclarationKind.Field;

        if ( compareExpr == null )
        {
            meta.Target.FieldOrProperty.Value = value;

            if ( propertyInfo != null )
            {
                foreach ( var r in GetReferencingProperties( templateArgs, propertyInfo.RootReferenceNode ) )
                {
                    templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( r.Name );
                }
            }

            if ( !isField && meta.Target.FieldOrProperty.Accessibility != Accessibility.Private )
            {
                templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( meta.Target.FieldOrProperty.Name );
            }
        }
        else
        {
            if ( compareExpr.Value )
            {
                meta.Target.FieldOrProperty.Value = value;

                if ( propertyInfo != null )
                {
                    foreach ( var r in GetReferencingProperties( templateArgs, propertyInfo.RootReferenceNode ) )
                    {
                        templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( r.Name );
                    }
                }

                if ( !isField && meta.Target.FieldOrProperty.Accessibility != Accessibility.Private )
                {
                    templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( meta.Target.FieldOrProperty.Name );
                }
            }
        }
    }

    private static IReadOnlyCollection<ClassicObservablePropertyInfo> GetReferencingProperties(
        ObservabilityTemplateArgs templateArgs,
        ClassicObservableExpression expression,
        bool excludeChildrenWithUpdateMethod = false )
    {
        Func<ClassicObservableExpression, bool>? shouldIncludeImmediateChild = excludeChildrenWithUpdateMethod ? n => n.UpdateMethod.Value == null : null;

        return expression.GetAllReferencingProperties( shouldIncludeImmediateChild )
            .Where( n => n.FieldOrProperty.Accessibility != Accessibility.Private )
            .Where( n => !n.FieldOrProperty.Attributes.Any( templateArgs.Assets.NotObservableAttribute ) )
            .OrderBy( n => n.Name )
            .ToArray();
    }

    [Template( Name = "OnPropertyChanged" )]
    internal void OnPropertyChangedString( string propertyName, [CompileTime] ObservabilityTemplateArgs templateArgs )
    {
        this.OnPropertyChanged(
            ExpressionFactory.Capture( propertyName ),
            ExpressionFactory.Capture( new PropertyChangedEventArgs( propertyName ) ),
            templateArgs );
    }

    [Template( Name = "OnPropertyChanged" )]
    internal void OnPropertyChangedObject( PropertyChangedEventArgs args, [CompileTime] ObservabilityTemplateArgs templateArgs )
    {
        this.OnPropertyChanged( ExpressionFactory.Capture( args.PropertyName ), ExpressionFactory.Capture( args ), templateArgs );
    }

    [Template]
    private void OnPropertyChanged(
        IExpression propertyNameExpression,
        IExpression propertyEventArgsExpression,
        [CompileTime] ObservabilityTemplateArgs templateArgs )
    {
        var templateArgsValue = templateArgs;

        if ( templateArgsValue.CommonOptions.DiagnosticCommentVerbosity! > 0 )
        {
            meta.InsertComment( "Template: " + nameof(this.OnPropertyChanged) );

            if ( templateArgsValue.CommonOptions.DiagnosticCommentVerbosity! > 1 )
            {
                meta.InsertComment( "Dependency graph:", "\n" + templateArgsValue.ObservableTypeInfo );
            }
        }

        var switchBuilder = new SwitchStatementBuilder( propertyNameExpression );

        foreach ( var propertyNode in templateArgsValue.ObservableTypeInfo.Properties )
        {
            var node = propertyNode.RootReferenceNode;

            switch ( node.InpcBaseHandling )
            {
                case InpcBaseHandling.NotApplicable or InpcBaseHandling.None:
                    continue;

                case InpcBaseHandling.OnObservablePropertyChanged when templateArgsValue.OnObservablePropertyChangedMethod != null:
                    {
                        if ( templateArgsValue.CommonOptions.DiagnosticCommentVerbosity! > 0 )
                        {
                            meta.InsertComment(
                                $"Skipping '{node.Name}': A base type supports OnObservablePropertyChanged for this property, and the current type is configured to use that feature." );
                        }

                        continue;
                    }
            }

            IReadOnlyCollection<ClassicObservablePropertyInfo> refsToNotify;

            // When a base supports OnChildPropertyChanged for a root property, changes to the ref itself will
            // be notified by OnPropertyChanged (the base won't call OnChildPropertyChanged for each child property
            // when the ref changes).

            if ( node is { InpcBaseHandling: InpcBaseHandling.OnChildPropertyChanged, IsRoot: false } )
            {
                if ( !node.HasAnyReferencingProperties )
                {
                    if ( templateArgsValue.CommonOptions.DiagnosticCommentVerbosity! > 0 )
                    {
                        meta.InsertComment(
                            $"Skipping '{node.Name}': A base type supports OnChildPropertyChanged for this property, and the property itself has no references." );
                    }

                    continue;
                }
                else
                {
                    // Only notify references to the node itself, child node changes will be handled via OnChildPropertyChanged.
                    refsToNotify = GetReferencingProperties( templateArgs, node );
                }
            }
            else
            {
                // Notify refs to the current node and any children without an update method.
                refsToNotify = GetReferencingProperties( templateArgs, node, excludeChildrenWithUpdateMethod: true );
            }

            var childUpdateMethods = node.ChildUpdateMethods;

            if ( refsToNotify.Any()
                 || childUpdateMethods.Count > 0
                 || node is
                 {
                     HasChildren: true, InpcBaseHandling: InpcBaseHandling.OnObservablePropertyChanged or InpcBaseHandling.OnPropertyChanged
                 } )
            {
                var rootPropertyNamesToNotify = refsToNotify
                    .Select( n => n.Name )
                    .ToList();

                switchBuilder.AddCase(
                    SwitchStatementLabel.CreateLiteral( node.Name ),
                    StatementFactory.FromTemplate(
                            nameof(OnPropertyChangedSwitchCase),
                            new { templateArgsValue, node, childUpdateMethods, rootPropertyNamesToNotify } )
                        .UnwrapBlock() );
            }
            else
            {
                if ( templateArgsValue.CommonOptions.DiagnosticCommentVerbosity! > 0 )
                {
                    meta.InsertComment( $"Skipping '{node.Name}' because there is nothing to do." );
                }
            }
        }

        if ( switchBuilder.SectionCount > 0 )
        {
            meta.InsertStatement( switchBuilder.ToStatement() );
        }

        if ( templateArgsValue.BaseOnPropertyChangedMethod == null )
        {
            this.PropertyChanged?.Invoke( meta.This, propertyEventArgsExpression.Value );
        }
        else
        {
            meta.Proceed();
        }
    }

    [Template]
    private static void OnPropertyChangedSwitchCase(
        ObservabilityTemplateArgs templateArgsValue,
        ClassicObservableExpression node,
        IReadOnlyCollection<IMethod> childUpdateMethods,
        [CompileTime] IReadOnlyCollection<string> rootPropertyNamesToNotify )
    {
        var emitDefaultNotifications = meta.CompileTime( true );

        if ( templateArgsValue.CommonOptions.DiagnosticCommentVerbosity! > 0 )
        {
            meta.InsertComment( $"InpcBaseHandling = {node.InpcBaseHandling}" );
        }

        switch ( node.InpcBaseHandling )
        {
            case InpcBaseHandling.Unknown:
                meta.InsertComment(
                    $"Warning: the type of property '{node.Name}' could not be analysed at design time, so it has been treated",
                    "as not implementing INotifyPropertyChanged. Code generated at compile time may differ." );

                break;

            case InpcBaseHandling.NotApplicable:
            case InpcBaseHandling.OnChildPropertyChanged:
                break;

            // NB: The OnObservablePropertyChanged case, when templateArgs.OnObservablePropertyChangedMethod.WillBeDefined is true, is handled above.
            case InpcBaseHandling.OnObservablePropertyChanged:
            case InpcBaseHandling.OnPropertyChanged:
                if ( node.HasChildren )
                {
                    // We get here because the current type as a ref to a base property of an INPC type, but we can't use
                    // OnChildPropertyChanged or OnObservablePropertyChanged from the base type (the base doesn't provide support, or we're
                    // configured not to use it). So this is like retrospectively adding a property setter override. Note that
                    // the base *must* provide OnPropertyChanged support for each of its properties as a minimum contract.

                    var handlerField = node.HandlerField.Value;

                    if ( !node.LastValueField.IsResolved )
                    {
                        meta.InsertComment( $"Error: LastValueField for {node.ReferencedFieldOrProperty} has not been set." );
                    }
                    else
                    {
                        var lastValueField = node.LastValueField.Value;
                        var eventRequiresCast = node.InpcInstrumentationKind.RequiresCast();

                        var oldValue = lastValueField.Value;
                        var newValue = node.ReferencedFieldOrProperty.Value;

                        if ( !ReferenceEquals( oldValue, newValue ) )
                        {
                            if ( oldValue != null )
                            {
                                if ( eventRequiresCast )
                                {
                                    meta.Cast( templateArgsValue.Assets.INotifyPropertyChanged, oldValue ).PropertyChanged -= handlerField.Value;
                                }
                                else
                                {
                                    oldValue.PropertyChanged -= handlerField.Value;
                                }
                            }

                            lastValueField.Value = newValue;

                            // Update methods will deal with notifications - * for those children which have update methods *
                            foreach ( var method in childUpdateMethods )
                            {
                                method.With( InvokerOptions.Final ).Invoke();
                            }

                            // rootPropertyNamesToNotify excludes children with update methods
                            // ReSharper disable once PossibleMultipleEnumeration
                            foreach ( var name in rootPropertyNamesToNotify )
                            {
                                templateArgsValue.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( name );
                            }

                            if ( newValue != null )
                            {
                                handlerField.Value ??= (PropertyChangedEventHandler) HandleChildPropertyChanged;

                                if ( eventRequiresCast )
                                {
                                    ((INotifyPropertyChanged) newValue).PropertyChanged += handlerField.Value;
                                }
                                else
                                {
                                    newValue.PropertyChanged += handlerField.Value;
                                }

                                // -----------------------------------------------------------------------
                                //                Local Function: OnChildPropertyChanged
                                // -----------------------------------------------------------------------

                                // ReSharper disable once LocalFunctionHidesMethod
                                void HandleChildPropertyChanged( object? sender, PropertyChangedEventArgs e )
                                {
                                    HandleChildPropertyChangedDelegateBody( templateArgsValue, node, ExpressionFactory.Capture( e ) );
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach ( var name in rootPropertyNamesToNotify )
                    {
                        templateArgsValue.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( name );
                    }
                }

                emitDefaultNotifications = false;

                break;

            default:
                CompileTimeThrow( new InvalidOperationException( $"InpcBaseHandling '{node.InpcBaseHandling}' was not expected here." ) );

                break;
        }

        if ( emitDefaultNotifications )
        {
            foreach ( var method in childUpdateMethods )
            {
                method.With( InvokerOptions.Final ).Invoke();
            }

            // ReSharper disable once PossibleMultipleEnumeration
            foreach ( var name in rootPropertyNamesToNotify )
            {
                templateArgsValue.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( name );
            }
        }
    }

    [Template]
    internal static void OnChildPropertyChanged(
        string parentPropertyPath,
        string propertyName,
        [CompileTime] ObservabilityTemplateArgs templateArgs,
        IReadOnlyList<ClassicObservableExpression> nodesForOnChildPropertyChanged )
    {
        if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 0 )
        {
            meta.InsertComment( "Template: " + nameof(OnChildPropertyChanged) );

            if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 1 )
            {
                meta.InsertComment( "Dependency graph:", "\n" + templateArgs.ObservableTypeInfo );
            }
        }

        var switchBuilder = new SwitchStatementBuilder( ExpressionFactory.Capture( (parentPropertyPath, propertyName) ) );

        foreach ( var node in nodesForOnChildPropertyChanged )
        {
            // NB: The following code is similar to the OnChildPropertyChangedDelegateBody template. Consider keeping any changes to relevant logic in sync.

            var hasUpdateMethod = node.UpdateMethod.Value != null;
            var hasRefs = node.LeafReferencingProperties.Any();

            if ( hasUpdateMethod || hasRefs )
            {
                switchBuilder.AddCase(
                    SwitchStatementLabel.CreateLiteral( node.Parent!.DottedPropertyPath, node.Name ),
                    StatementFactory.FromTemplate(
                            new TemplateInvocation( nameof(OnChildPropertyChangedMainSwitchCase), arguments: new { node, hasUpdateMethod, templateArgs } ) )
                        .UnwrapBlock() );
            }
        }

        if ( switchBuilder.SectionCount > 0 )
        {
            meta.InsertStatement( switchBuilder.ToStatement() );
        }

        if ( templateArgs.BaseOnChildPropertyChangedMethod != null )
        {
            meta.Proceed();
        }
    }

    [Template]
    private static void OnChildPropertyChangedMainSwitchCase(
        ClassicObservableExpression node,
        [CompileTime] bool hasUpdateMethod,
        ObservabilityTemplateArgs templateArgs )
    {
        if ( hasUpdateMethod )
        {
            // Update method will deal with notifications
            node.UpdateMethod.Value!.With( InvokerOptions.Final ).Invoke();
        }
        else
        {
            // No update method, notify here.
            foreach ( var r in GetReferencingProperties( templateArgs, node ) )
            {
                templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( r.Name );
            }
        }

        if ( templateArgs.BaseOnChildPropertyChangedMethod != null )
        {
            meta.Proceed();
        }
    }

    [Template]
    internal static void OnObservablePropertyChanged(
        string propertyPath,
        INotifyPropertyChanged? oldValue,
        INotifyPropertyChanged? newValue,
        [CompileTime] ObservabilityTemplateArgs templateArgs,
        IReadOnlyList<ClassicObservableExpression> nodesProcessedByOnObservablePropertyChanged )
    {
        if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 0 )
        {
            meta.InsertComment( "Template: " + nameof(OnObservablePropertyChanged) );

            if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 1 )
            {
                meta.InsertComment( "Dependency graph:", "\n" + templateArgs.ObservableTypeInfo );
            }
        }

        /*
         * The generated OnObservablePropertyChanged method is like an enhanced overload of OnPropertyChanged, the differences
         * being that the caller provides the old and new values, the method receives a property path rather than a root property name,
         * and it only applies to property types which implement INotifyPropertyChanged.
         *
         * NB:
         *
         * - In the current implementation (outside this template), the generated OnObservablePropertyChanged
         *   method is only called for root properties. As/when false positive detection is implemented and enabled,
         *   then the generated OnObservablePropertyChanged method could receive calls for leaf INotifyPropertyChanged
         *   properties.
         *
         * - OnObservablePropertyChanged is only called when a ref has changed - the caller checks this first.
         *
         * - For root properties, the caller will also call OnPropertyChanged - this is to maintain compatibility with derivations
         *   which do not observe OnObservablePropertyChanged.
         *
         * - Nodes only appear in the graph if they are relevant to the current class.
         *
         * - The logic which selects nodes for which support will be generated must be kept in sync with the logic in
         *   NaturalAspect.AddPropertyPathsForOnChildPropertyChangedMethodAttribute.
         */

        foreach ( var node in nodesProcessedByOnObservablePropertyChanged )
        {
            if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 0 )
            {
                meta.InsertComment( $"Node '{node.DottedPropertyPath}'" );
            }

            if ( propertyPath == node.DottedPropertyPath )
            {
                // This is very similar to an Update... method or a property setter override, except the base type keeps track of the "old" value so we don't have to.
                // Note that here we may be dealing with a root property defined in another type (so like a setter), or any descendant node (so
                // like an Update method).

                var handlerField = node.HandlerField.Value;

                if ( oldValue != null )
                {
                    oldValue.PropertyChanged -= handlerField.Value;
                }

                if ( newValue != null )
                {
                    handlerField.Value ??= (PropertyChangedEventHandler) HandleChildPropertyChanged;
                    newValue.PropertyChanged += handlerField.Value;

                    // -----------------------------------------------------------------------
                    //                Local Function: OnChildPropertyChanged
                    // -----------------------------------------------------------------------

                    // ReSharper disable once LocalFunctionHidesMethod
                    void HandleChildPropertyChanged( object? sender, PropertyChangedEventArgs e )
                    {
                        HandleChildPropertyChangedDelegateBody( templateArgs, node, ExpressionFactory.Capture( e ) );
                    }
                }

                // Update methods will deal with notifications - *for those children which have update methods*
                foreach ( var method in node.ChildUpdateMethods )
                {
                    method.With( InvokerOptions.Final ).Invoke();
                }

                // Notify refs to the current node and any children without an update method.
                foreach ( var r in GetReferencingProperties( templateArgs, node, excludeChildrenWithUpdateMethod: true ) )
                {
                    templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( r.Name );
                }
            }
        }

        if ( templateArgs.BaseOnObservablePropertyChangedMethod != null )
        {
            meta.Proceed();
        }
    }

    [Template]
    private static void HandleChildPropertyChangedDelegateBody(
        [CompileTime] ObservabilityTemplateArgs templateArgs,
        [CompileTime] ClassicObservableExpression node,
        [CompileTime] IExpression propertyChangedEventArgs )
    {
        if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 0 )
        {
            meta.InsertComment( "Template: " + nameof(HandleChildPropertyChangedDelegateBody) );

            if ( templateArgs.CommonOptions.DiagnosticCommentVerbosity! > 1 )
            {
                meta.InsertComment( "Dependency graph:", "\n" + templateArgs.ObservableTypeInfo.ToString( node ) );
            }
        }

        var propertyName = propertyChangedEventArgs.Value!.PropertyName;
        var propertyNameExpression = (IExpression) propertyName;
        var nodeIsAccessible = node.ReferencedFieldOrProperty.Accessibility != Accessibility.Private;

        var switchBuilder = new SwitchStatementBuilder( propertyNameExpression );

        foreach ( var childNode in node.Children )
        {
            // NB: The following code is similar to part of the OnChildPropertyChanged template. Consider keeping any changes to relevant logic in sync.

            var hasUpdateMethod = childNode.UpdateMethod.Value != null;
            var hasRefs = childNode.HasAnyReferencingProperties;

            if ( hasUpdateMethod || hasRefs )
            {
                switchBuilder.AddCase(
                    SwitchStatementLabel.CreateLiteral( childNode.Name ),
                    StatementFactory.FromTemplate(
                            nameof(HandleChildPropertyChangedCase),
                            args: new
                            {
                                templateArgs,
                                node,
                                hasUpdateMethod,
                                childNode,
                                nodeIsAccessible
                            } )
                        .UnwrapBlock() );
            }
        }

        if ( nodeIsAccessible )
        {
            switchBuilder.AddDefault(
                StatementFactory.FromTemplate(
                        nameof(HandleChildPropertyChangedDefault),
                        args: new { templateArgs, node, propertyName = propertyNameExpression } )
                    .UnwrapBlock() );
        }

        if ( switchBuilder.SectionCount > 0 )
        {
            meta.InsertStatement( switchBuilder.ToStatement() );
        }
    }

    [Template]
    private static void HandleChildPropertyChangedDefault(
        ObservabilityTemplateArgs templateArgs,
        ClassicObservableExpression node,
        IExpression propertyName )
        => templateArgs.OnChildPropertyChangedMethod!.With( InvokerOptions.Final ).Invoke( node.DottedPropertyPath, propertyName );

    [Template]
    private static void HandleChildPropertyChangedCase(
        ObservabilityTemplateArgs templateArgs,
        ClassicObservableExpression node,
        [CompileTime] bool hasUpdateMethod,
        ClassicObservableExpression childNode,
        [CompileTime] bool nodeIsAccessible )
    {
        if ( hasUpdateMethod )
        {
            // Update method will deal with notifications
            childNode.UpdateMethod.Value!.With( InvokerOptions.Final ).Invoke();
        }
        else
        {
            // No update method, notify here.
            foreach ( var r in GetReferencingProperties( templateArgs, childNode ) )
            {
                templateArgs.OnPropertyChangedInvocableMethod.With( InvokerOptions.Final ).Invoke( r.Name );
            }

            if ( nodeIsAccessible )
            {
                templateArgs.OnChildPropertyChangedMethod!.With( InvokerOptions.Final ).Invoke( node.DottedPropertyPath, childNode.Name );
            }
        }
    }
}
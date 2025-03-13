// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

[CompileTime]
internal sealed class ClassicObservableExpression : ObservableExpression
{
    public new IEnumerable<ClassicObservableExpression> Children => base.Children.Cast<ClassicObservableExpression>();

    public InpcInstrumentationKind InpcInstrumentationKind
        => ((ClassicObservableTypeInfo) this.ReferencedPropertyInfo.DeclaringTypeInfo).InpcInstrumentationKind;

    /// <summary>
    /// Gets the potentially uninitialized field like "B? _lastA2". From non-template code, use
    /// <see cref="ClassicObservabilityStrategyImpl.GetOrCreateLastValueField"/> instead.
    /// </summary>
    /// <remarks>
    /// The value will only be set for applicable nodes. Template code should only be accessing the value of
    /// <see cref="LastValueField"/> when logic determines that it should have been set, and <see cref="Promise{T}"/>
    /// will helpfully throw if there is an error in the program design.
    /// </remarks>
    public Promise<IField> LastValueField { get; } = new();

    /// <summary>
    /// Gets the potentially uninitialized field like "PropertyChangedEventHandler? _onA2PropertyChangedHandler".
    /// From non-template code, use <see cref="ClassicObservabilityStrategyImpl.GetOrCreateHandlerField"/> instead.
    /// </summary>
    /// <remarks>
    /// The value will only be set for applicable nodes. Template code should only be accessing the value of
    /// <see cref="HandlerField"/> when logic determines that it should have been set, and <see cref="Promise{T}"/>
    /// will helpfully throw if there is an error in the program design.
    /// </remarks>
    public Promise<IField> HandlerField { get; } = new();

    private IReadOnlyCollection<IMethod>? _childUpdateMethods;

    /// <summary>
    /// Gets the non-null <see cref="UpdateMethod"/> declarations of the children of the current node.
    /// </summary>
    public IReadOnlyCollection<IMethod> ChildUpdateMethods
    {
        get
        {
            // NB: This will intentionally throw if any encountered node has not yet had the UpdateMethod value setter
            // invoked. This ensures that the outcome is consistent and the result can be cached.

            this._childUpdateMethods ??= this.Children
                .Select( n => n.UpdateMethod.Value )
                .Where( m => m != null )
                .ToList()!;

            return this._childUpdateMethods;
        }
    }

    /// <summary>
    /// Gets a method like <c>void UpdateA2C2()</c>. 
    /// </summary>
    /// <remarks>
    /// In a given inheritance hierarchy, for a given property path, this method is defined in
    /// the type closest to the root type where a reference to a property of A2.C2 first occurs.
    /// The method is always private as it is only called by other members of the type where it
    /// is defined. The value of <see cref="UpdateMethod"/> is always explicitly set for all 
    /// nodes (regardless of whether the value is <see langword="null"/> or not), and accessing the value
    /// will throw if it has not been set. This provides defense against incorrect program design
    /// that could otherwise lead to subtle incorrect behaviour.
    /// </remarks>
    public Promise<IMethod?> UpdateMethod { get; } = new();

    public Promise<IMethod> SubscribeMethod { get; } = new();

    /// <summary>
    /// Gets the handling strategy of the current reference by the base type. 
    /// </summary>
    public InpcBaseHandling InpcBaseHandling { get; private set; }

    public ClassicObservableExpression(
        ObservablePropertyInfo referencedPropertyInfo,
        ObservableExpression? parent,
        ClassicDependencyGraphBuilder builder ) : base(
        referencedPropertyInfo,
        parent,
        builder )
    {
        var fieldOrProperty = referencedPropertyInfo.FieldOrProperty;

        this.InpcBaseHandling =
            ((ClassicObservableTypeInfo) this.ReferencedPropertyInfo.DeclaringTypeInfo).InpcInstrumentationKind.IsImplemented() switch
            {
                null => InpcBaseHandling.Unknown,

                false => InpcBaseHandling.NotApplicable,

                true when this.IsRoot =>
                    fieldOrProperty.DeclaringType.Equals( builder.CurrentType )
                        ? InpcBaseHandling.NotApplicable
                        : builder.Context.HasInheritedOnChildPropertyChangedPropertyPath( this.DottedPropertyPath )
                            ? InpcBaseHandling.OnChildPropertyChanged
                            : builder.Context.HasInheritedOnObservablePropertyChangedProperty( this.DottedPropertyPath )
                                ? InpcBaseHandling.OnObservablePropertyChanged
                                : InpcBaseHandling.OnPropertyChanged,
                true when !this.IsRoot =>
                    builder.Context.HasInheritedOnChildPropertyChangedPropertyPath( this.DottedPropertyPath )
                        ? InpcBaseHandling.OnChildPropertyChanged
                        : builder.Context.HasInheritedOnObservablePropertyChangedProperty( this.DottedPropertyPath )
                            ? InpcBaseHandling.OnObservablePropertyChanged
                            : InpcBaseHandling.None,

                _ => throw new NotSupportedException()
            };
    }

    public new IEnumerable<ClassicObservablePropertyInfo> LeafReferencingProperties => base.LeafReferencingProperties.Cast<ClassicObservablePropertyInfo>();

    public IReadOnlyCollection<ClassicObservablePropertyInfo> GetAllReferencingProperties(
        Func<ClassicObservableExpression, bool>? shouldIncludeImmediateChild = null )
        => this.GetAllReferencingProperties<ClassicObservablePropertyInfo>(
            shouldIncludeImmediateChild == null ? null : node => shouldIncludeImmediateChild.Invoke( (ClassicObservableExpression) node ) );
}
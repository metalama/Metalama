// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Patterns.Observability.Configuration;
using Microsoft.CodeAnalysis;
using static Metalama.Framework.Diagnostics.Severity;

namespace Metalama.Patterns.Observability.Implementation;

// ReSharper disable InconsistentNaming
[CompileTime]
internal static class DiagnosticDescriptors
{
    private const string _category = "Metalama.Patterns.Observability";

    // Reserved range 5150-5199

    /// <summary>
    /// Class {0} implements INotifyPropertyChanged but does not define an OnPropertyChanged method with the following signature: void OnPropertyChanged(string propertyName).
    /// </summary>
    public static readonly DiagnosticDefinition<INamedType> ErrorClassImplementsInpcButDoesNotDefineOnOverridablePropertyChanged =
        new(
            "LAMA5150",
            Error,
            "Class '{0}' implements INotifyPropertyChanged but does not define an overridable OnPropertyChanged method with the following signature: "
            +
            "`protected virtual void OnPropertyChanged(string)` or `protected virtual void OnPropertyChanged(PropertyChangedEventArgs)`. The method name can also be NotifyOfPropertyChange or RaisePropertyChanged.",
            "OnPropertyChanged is not defined.",
            _category );

    /// <summary>
    /// The project property '{0}' has invalid value '{1}' and will be ignored. The value must {2}.
    /// </summary>
    public static readonly DiagnosticDefinition<(string PropertyName, string PropertyValue, string Reason)> WarningInvalidProjectPropertyValueWillBeIgnored =
        new(
            "LAMA5151",
            Warning,
            "The project property '{0}' has invalid value '{1}' and will be ignored. The value must {2}.",
            "Invalid project property.",
            _category );

    /// <summary>
    /// The type {2} of {0} {1} is a struct implementing INotifyPropertyChanged. Structs implementing INotifyPropertyChanged are not supported.
    /// </summary>
    public static readonly DiagnosticDefinition<(DeclarationKind Kind, IFieldOrProperty FieldOrProperty, IType ParameterType)>
        ErrorFieldOrPropertyTypeIsStructImplementingInpc =
            new(
                "LAMA5152",
                Error,
                "The type '{2}' of {0} '{1}' is a struct implementing INotifyPropertyChanged. Structs implementing INotifyPropertyChanged are not supported.",
                "Property type is struct implementing INotifyPropertyChanged.",
                _category );

    /// <summary>
    /// The {0} {1} is virtual. This is not supported.
    /// </summary>
    public static readonly DiagnosticDefinition<(DeclarationKind Kind, IFieldOrProperty FieldOrProperty)> ErrorVirtualMemberIsNotSupported =
        new(
            "LAMA5154",
            Error,
            "The '{1}' {0} is virtual. This is not supported by the [Observable] aspect.",
            "Virtual member is not supported.",
            _category );

    /// <summary>
    /// The {0} {1} is 'new'. This is not supported.
    /// </summary>
    public static readonly DiagnosticDefinition<(DeclarationKind Kind, IFieldOrProperty FieldOrProperty)> ErrorNewMemberIsNotSupported =
        new(
            "LAMA5155",
            Error,
            "The '{1}' {0} is 'new'. This is not supported by the [Observable] aspect.",
            "'new' member is not supported.",
            _category );

    /// <summary>
    /// The children of fields or properties of type '{0}' cannot be observed because the type does not implement INotifyPropertyChanged.
    /// </summary>
    public static readonly DiagnosticDefinition<ITypeSymbol> WarningChildrenOfNonInpcFieldsOrPropertiesAreNotObservable =
        new(
            "LAMA5161",
            Warning,
            "The children of fields or properties of type '{0}' cannot be observed because the type does not implement INotifyPropertyChanged.",
            "Field or property type does not implement INotifyPropertyChanged.",
            _category );

    /// <summary>
    /// {0} {1} cannot be analysed, and has not been configured as safe for dependency analysis. Use [IgnoreUnobservableExpressions] or ConfigureDependencyAnalysis via a fabric to configure {0} as safe.
    /// </summary>
    public static readonly DiagnosticDefinition<(SymbolKind Kind, ISymbol MethodOrPropertySymbol)> WarningMethodOrPropertyIsNotSupportedForDependencyAnalysis =
        new(
            "LAMA5162",
            Warning,
            $"The '{{1}}' {{0}} cannot be analysed, and has not been configured with an observability contract. Mark this {{0}} with [{nameof(ConstantAttribute)}] or call "
            + nameof(ObservabilityExtensions.ConfigureObservability) + " via a fabric.",
            "Method or property is not supported for dependency analysis.",
            _category );

    public static readonly DiagnosticDefinition<(ISymbol Member, INamedTypeSymbol DeclaringType)> DeclaringTypeDoesNotImplementInpc =
        new(
            "LAMA5163",
            Warning,
            "The '{0}' property cannot be analysed: changes to children of non-auto properties declared on the current type cannot be observed unless the property type implements INotifyPropertyChanged. Consider implementing the INotifyPropertyChanged interface in '{1}', marking '{0}' with [Constant], or using "
            + nameof(ObservabilityExtensions.ConfigureObservability) + " via a fabric.",
            "Changes to children of non-auto properties declared on the current type cannot be observed unless the property type implements INotifyPropertyChanged.",
            _category );

    public static readonly DiagnosticDefinition<IFieldSymbol> NonPrivateFieldsNonSupported =
        new(
            "LAMA5164",
            Warning,
            "The '{0}' field cannot be analysed: only private instance fields of the current type, fields belonging to primitive types, readonly fields of primitive types, and fields configured with an observability contract are supported. Consider accessing the field through a property, marking '{0}' with [Constant], or using "
            + nameof(ObservabilityExtensions.ConfigureObservability) + " via a fabric.",
            "Only private instance fields of the current type, fields belonging to primitive types, readonly fields of primitive types, and fields configured with an observability contract are supported.",
            _category );

    // "" 
    public static readonly DiagnosticDefinition<ISymbol> LocalVariablesNonSupported =
        new(
            "LAMA5165",
            Warning,
            "The '{0}' local variable cannot be analysed: variables of types other than primitive types are not supported.",
            "Variables of types other than primitive types and types configured as deeply immutable are not supported.",
            _category );

    public static readonly DiagnosticDefinition<INamedType> ErrorClassImplementsInpcButDoesNotDefineOnInvocablePropertyChanged =
        new(
            "LAMA5156",
            Error,
            "Class '{0}' implements INotifyPropertyChanged but does not define an OnPropertyChanged method with the following signature: "
            +
            "`protected void OnPropertyChanged(string)`. The method name can also be NotifyOfPropertyChange or RaisePropertyChanged.",
            "OnPropertyChanged is not defined.",
            _category );
}
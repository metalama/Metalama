// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
            "The class '{0}' implements the PropertyChanged event, but neither it nor its base classes define an overridable 'void " +
            "OnPropertyChanged(string)' or 'void OnPropertyChanged(PropertyChangedEventArgs)' method. The method must be public or protected, " +
            "virtual or override, and not sealed. It can also be named 'NotifyOfPropertyChange' or 'RaisePropertyChanged'.",
            "The OnPropertyChanged method is not defined or cannot be overridden.",
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
                "The type '{2}' of the {0} '{1}' is a struct that implements INotifyPropertyChanged. The [Observable] aspect does not support " +
                "structs that implement INotifyPropertyChanged.",
                "The type of a field or property is a struct that implements INotifyPropertyChanged.",
                _category );

    /// <summary>
    /// The {0} {1} is virtual. This is not supported.
    /// </summary>
    public static readonly DiagnosticDefinition<(DeclarationKind Kind, IFieldOrProperty FieldOrProperty)> ErrorVirtualMemberIsNotSupported =
        new(
            "LAMA5154",
            Error,
            "The '{1}' {0} is virtual, which is not supported by the [Observable] aspect. Remove the 'virtual' modifier, or exclude the {0} " +
            "from the aspect with the [NotObservable] attribute.",
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

    public static readonly DiagnosticDefinition<INamedType> ErrorClassImplementsInpcButDoesNotDefineOnInvocablePropertyChanged =
        new(
            "LAMA5156",
            Error,
            "Class '{0}' implements INotifyPropertyChanged but neither defines nor inherits a method with the signature 'protected void " +
            "OnPropertyChanged(string)', which the [Observable] aspect requires to raise notifications. The method name can also be " +
            "NotifyOfPropertyChange or RaisePropertyChanged.",
            "The OnPropertyChanged(string) method is not defined.",
            _category );

    /// <summary>
    /// The children of fields or properties of type '{0}' cannot be observed because the type does not implement INotifyPropertyChanged.
    /// </summary>
    public static readonly DiagnosticDefinition<ITypeSymbol> WarningChildrenOfNonInpcFieldsOrPropertiesAreNotObservable =
        new(
            "LAMA5161",
            Warning,
            "The members of '{0}' cannot be observed because '{0}' does not implement INotifyPropertyChanged and is not immutable. Implement " +
            "INotifyPropertyChanged in '{0}', for example with the [Observable] aspect, or mark '{0}' as immutable.",
            "Field or property type does not implement INotifyPropertyChanged.",
            _category );

    /// <summary>
    /// {0} {1} cannot be analysed, and has not been configured as safe for dependency analysis. Use [IgnoreUnobservableExpressions] or ConfigureDependencyAnalysis via a fabric to configure {0} as safe.
    /// </summary>
    public static readonly DiagnosticDefinition<(SymbolKind Kind, ISymbol MethodOrPropertySymbol)> WarningMethodOrPropertyIsNotSupportedForDependencyAnalysis =
        new(
            "LAMA5162",
            Warning,
            "The '{1}' {0} cannot be observed because it is not known to be constant: it is an instance method of a type that is not immutable, or it has a parameter of a type that is not immutable. " +
            "If the {0} always returns the same value for the same arguments, mark it with [Constant] or call " + nameof(ObservabilityExtensions.ConfigureObservability) + " via a fabric.",
            "Method is not supported for dependency analysis.",
            _category );

    public static readonly DiagnosticDefinition<(ISymbol Member, INamedTypeSymbol DeclaringType)> DeclaringTypeDoesNotImplementInpcStem =
        new(
            "LAMA5163",
            Warning,
            "Changes to the children of the '{0}' property cannot be observed because '{0}' is not an auto-property. Access the child through an auto-property or a field of '{1}' instead.",
            "Changes to children of non-auto properties of the current type cannot be observed.",
            _category );

    public static readonly DiagnosticDefinition<IFieldSymbol> NonPrivateFieldsNonSupported =
        new(
            "LAMA5164",
            Warning,
            "The '{0}' field cannot be observed: only private instance fields, constants, and read-only fields of immutable types are supported. Consider replacing the field with a property, or making it private.",
            "Only private instance fields, constants, and read-only fields of immutable types are supported.",
            _category );

    public static readonly DiagnosticDefinition<ISymbol> LocalVariablesNonSupported =
        new(
            "LAMA5165",
            Warning,
            "Local variables of type '{0}' cannot be observed: only local variables of immutable types, such as primitive types, are supported.",
            "Local variables of types that are not immutable are not supported.",
            _category );

    public static readonly DiagnosticDefinition<(ISymbol Member, INamedTypeSymbol DeclaringType)> DeclaringTypeDoesNotImplementInpcLeaf =
        new(
            "LAMA5163",
            Warning,
            "The '{0}' property cannot be observed because '{1}' does not implement INotifyPropertyChanged. Consider implementing the INotifyPropertyChanged interface in '{1}', marking '{0}' with [Constant], or using "
            + nameof(ObservabilityExtensions.ConfigureObservability) + " via a fabric.",
            "Properties cannot be observed unless their declaring type implements INotifyPropertyChanged.",
            _category );
}
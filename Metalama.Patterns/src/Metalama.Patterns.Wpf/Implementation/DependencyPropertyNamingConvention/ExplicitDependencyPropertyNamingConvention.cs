// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;

namespace Metalama.Patterns.Wpf.Implementation.DependencyPropertyNamingConvention;

[CompileTime]
internal sealed class ExplicitDependencyPropertyNamingConvention : IDependencyPropertyNamingConvention
{
    private readonly string? _registrationFieldName;
    private readonly string? _propertyChangedMethodName;
    private readonly string? _validateMethodName;

    public ExplicitDependencyPropertyNamingConvention(
        string? registrationFieldName,
        string? propertyChangedMethodName,
        string? validateMethodName )
    {
        this._registrationFieldName = registrationFieldName;
        this._propertyChangedMethodName = propertyChangedMethodName;
        this._validateMethodName = validateMethodName;
    }

    public string Name => "explicitly-configured";

    public DependencyPropertyNamingConventionMatch Match( IProperty targetProperty )
    {
        var propertyName = targetProperty.Name;

        return DependencyPropertyNamingConventionMatcher.Match(
            this,
            targetProperty,
            propertyName,
            this._registrationFieldName ?? DefaultDependencyPropertyNamingConvention.GetRegistrationFieldNameFromPropertyName( propertyName ),
            new StringNameMatchPredicate(
                this._propertyChangedMethodName ?? DefaultDependencyPropertyNamingConvention.GetPropertyChangedMethodNameFromPropertyName( propertyName ) ),
            new StringNameMatchPredicate(
                this._validateMethodName ?? DefaultDependencyPropertyNamingConvention.GetValidateMethodNameFromPropertyName( propertyName ) ),
            requirePropertyChangedMatch: this._propertyChangedMethodName != null,
            requireValidateMatch: this._validateMethodName != null );
    }
}
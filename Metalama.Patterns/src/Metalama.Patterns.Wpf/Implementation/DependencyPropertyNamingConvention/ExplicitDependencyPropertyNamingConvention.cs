// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
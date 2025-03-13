// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;
using Metalama.Patterns.Wpf.Implementation;
using Metalama.Patterns.Wpf.Implementation.DependencyPropertyNamingConvention;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Text.RegularExpressions;

namespace Metalama.Patterns.Wpf.Configuration;

// Prevent netframework-only false positives

#if NETFRAMEWORK
#pragma warning disable CS8604 // Possible null reference argument.
#endif

#pragma warning disable SA1623

[CompileTime]
[PublicAPI]
public sealed record DependencyPropertyNamingConvention : IDependencyPropertyNamingConvention
{
    /// <summary>
    /// Gets or sets the regular expression pattern that will be evaluated against the name of the target property of the <see cref="DependencyPropertyAttribute"/> aspect. 
    /// The expression should yield a match group named <c>Name</c>. If <see cref="PropertyNamePattern"/> is not specified, the name of the target property is used
    /// as the name of the dependency property.
    /// </summary>
    public string? PropertyNamePattern { get; init; }

    /// <summary>
    /// Gets or sets the name of the registration field to be introduced. The substring <c>{PropertyName}</c> will be replaced with
    /// the name as determined according to <see cref="PropertyNamePattern"/>. The default value is  <c>{PropertyName}Property</c>.
    /// </summary>
    public string? RegistrationFieldName { get; init; }

    /// <summary>
    /// Gets or sets a regular expression pattern used to identify a method invoked <i>after</i> the property has changed. 
    /// All occurrences of the substring <c>{PropertyName}</c> will be replaced with the name
    /// determined according to <see cref="PropertyNamePattern"/> before the expression is evaluated. The default value is <c>On{PropertyName}Changed</c>.
    /// </summary>
    public string? OnPropertyChangedPattern { get; init; }

    /// <summary>
    /// Gets or sets a regular expression used to identify the method called before the property is changed to perform validation. 
    /// All occurrences of the substring <c>{PropertyName}</c>  will be replaced with the name
    /// determined according to <see cref="PropertyNamePattern"/> before the expression is evaluated. The default value is <c>^Validate{PropertyName}$</c>.
    /// </summary>
    public string? ValidatePattern { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>OnChanged</c> method is required. The default value of this property
    /// is <c>true</c> if a value is provided for <see cref="OnPropertyChangedPattern"/>, otherwise <c>false</c>.
    /// </summary>
    public bool? IsOnPropertyChangedRequired { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>Validate</c> method is required. The default value of this property
    /// is <c>true</c> if a value is provided for <see cref="ValidatePattern"/>, otherwise <c>false</c>.
    /// </summary>
    public bool? IsValidateRequired { get; init; }

    [NonCompileTimeSerialized]
    private Regex? _matchNameRegex;

    public DependencyPropertyNamingConvention( string name )
    {
        this.Name = name;
    }

    public string Name { get; }

    DependencyPropertyNamingConventionMatch INamingConvention<IProperty, DependencyPropertyNamingConventionMatch>.Match( IProperty targetProperty )
    {
        string? propertyName = null;

        if ( this.PropertyNamePattern != null )
        {
            this._matchNameRegex ??= new Regex( this.PropertyNamePattern );

            var m = this._matchNameRegex.Match( targetProperty.Name );

            if ( m.Success )
            {
                var g = m.Groups["Name"];

                if ( g.Success )
                {
                    propertyName = g.Value;
                }
            }
        }
        else
        {
            propertyName = targetProperty.Name;
        }

        if ( string.IsNullOrWhiteSpace( propertyName ) )
        {
            return new DependencyPropertyNamingConventionMatch(
                this,
                null,
                null,
                MemberMatch<IMemberOrNamedType, DefaultMatchKind>.Invalid(),
                MemberMatch<IMethod, ChangeHandlerSignatureKind>.NotFound(),
                MemberMatch<IMethod, ValidationHandlerSignatureKind>.NotFound(),
                [],
                this.IsOnPropertyChangedRequired.GetValueOrDefault( this.OnPropertyChangedPattern != null ),
                this.IsValidateRequired.GetValueOrDefault( this.ValidatePattern != null ) );
        }

#if NETCOREAPP
#pragma warning disable CA1307 // Specify StringComparison for clarity
#endif
        var registrationFieldName = this.RegistrationFieldName != null
            ? this.RegistrationFieldName.Replace( "{PropertyName}", propertyName )
            : DefaultDependencyPropertyNamingConvention.GetRegistrationFieldNameFromPropertyName( propertyName );

        var matchPropertyChanged = this.OnPropertyChangedPattern?.Replace( "{PropertyName}", propertyName );

        var matchValidate = this.ValidatePattern?.Replace( "{PropertyName}", propertyName );
#if NETCOREAPP
#pragma warning restore CA1307 // Specify StringComparison for clarity
#endif

        var propertyChangedPredicate = matchPropertyChanged == null
            ? (INameMatchPredicate) new StringNameMatchPredicate(
                DefaultDependencyPropertyNamingConvention.GetPropertyChangedMethodNameFromPropertyName( propertyName ) )
            : new RegexNameMatchPredicate( new Regex( matchPropertyChanged ) );

        var validatePredicate = matchValidate == null
            ? (INameMatchPredicate) new StringNameMatchPredicate(
                DefaultDependencyPropertyNamingConvention.GetValidateMethodNameFromPropertyName( propertyName ) )
            : new RegexNameMatchPredicate( new Regex( matchValidate ) );

        return DependencyPropertyNamingConventionMatcher.Match(
            this,
            targetProperty,
            propertyName,
            registrationFieldName,
            propertyChangedPredicate,
            validatePredicate,
            this.IsOnPropertyChangedRequired.GetValueOrDefault( this.OnPropertyChangedPattern != null ),
            this.IsValidateRequired.GetValueOrDefault( this.ValidatePattern != null ) );
    }
}
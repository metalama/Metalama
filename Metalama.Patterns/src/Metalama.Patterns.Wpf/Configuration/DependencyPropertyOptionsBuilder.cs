// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using Metalama.Patterns.Wpf.Implementation.DependencyPropertyNamingConvention;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Windows;

namespace Metalama.Patterns.Wpf.Configuration;

/// <summary>
/// Builder for configuring the <see cref="DependencyPropertyAttribute"/> aspect, allowing customization of naming conventions,
/// read-only behavior, and default value handling.
/// </summary>
/// <remarks>
/// <para>Use this builder through the <see cref="DependencyPropertyExtensions.ConfigureDependencyProperty(IQuery{ICompilation},Action{DependencyPropertyOptionsBuilder})"/> method
/// and its overloads to configure dependency property options at the project, namespace, type, or property level.</para>
/// </remarks>
/// <seealso cref="DependencyPropertyExtensions"/>
/// <seealso cref="DependencyPropertyNamingConvention"/>
/// <seealso cref="DependencyPropertyAttribute"/>
/// <seealso href="@wpf-dependency-property"/>
[PublicAPI]
[CompileTime]
public sealed class DependencyPropertyOptionsBuilder
{
    private DependencyPropertyOptions _options = new();
    private int _nextPriority;

    /// <summary>
    /// Gets the key of the default naming convention.
    /// </summary>
    public static string DefaultNamingConventionName => DefaultDependencyPropertyNamingConvention.RegistrationKey;

    /// <summary>
    /// Gets or sets a value indicating whether the property should be registered as a read-only property.
    /// </summary>
    public bool? IsReadOnly
    {
        get => this._options.IsReadOnly;
        set => this._options = this._options with { IsReadOnly = value };
    }

    /// <summary>
    /// Gets or sets a value indicating whether the property initializer (if present) should be used to for <see cref="PropertyMetadata.DefaultValue"/>.
    /// The default is <see langword="true"/>.
    /// </summary>
    public bool? InitializerProvidesDefaultValue
    {
        get => this._options.InitializerProvidesDefaultValue;
        set => this._options = this._options with { InitializerProvidesDefaultValue = value };
    }

    /// <summary>
    /// Adds or updates a naming convention that specifies, based on the name of the target method of the <see cref="DependencyProperty"/>
    /// aspect: the name of the registration field and of the <c>OnChanging</c>, <c>OnChanged</c> and <c>Validate</c> methods.
    /// </summary>
    /// <param name="namingConvention">A <see cref="DependencyPropertyNamingConvention"/>.</param>
    /// <param name="priority">The priority of the naming convention. By default, the priority is 0 for the first call of
    /// this method, then it is incremented at every call.</param>
    /// <remarks>
    /// <para>If a <see cref="DependencyPropertyNamingConvention"/> of the same <see cref="DependencyPropertyNamingConvention.Name"/> has already been registered,
    /// this call replaces the old instance by the new one, including the new <paramref name="priority"/>.</para>
    /// </remarks>
    public void AddNamingConvention(
        DependencyPropertyNamingConvention namingConvention,
        int? priority = null )
    {
        if ( namingConvention.Name == DefaultNamingConventionName )
        {
            throw new InvalidOperationException( "The default naming convention cannot be modified." );
        }

        var priorityValue = priority.GetValueOrDefault( this._nextPriority );
        this._nextPriority = priorityValue + 1;

        this._options = this._options with
        {
            NamingConventionRegistrations =
            this._options.NamingConventionRegistrations.AddOrApplyChanges(
                new NamingConventionRegistration<IDependencyPropertyNamingConvention>(
                    namingConvention.Name,
                    namingConvention,
                    priority ) )
        };
    }

    /// <summary>
    /// Changes the priority of a <see cref="DependencyPropertyNamingConvention"/>.
    /// </summary>
    /// <param name="key">The <see cref="DependencyPropertyNamingConvention.Name"/> of the naming convention.</param>
    /// <param name="priority">The new priority of the naming convention.</param>
    public void SetNamingConventionPriority( string key, int priority )
        => this._options = this._options with
        {
            NamingConventionRegistrations =
            this._options.NamingConventionRegistrations.AddOrApplyChanges(
                new NamingConventionRegistration<IDependencyPropertyNamingConvention>( key, null, priority ) )
        };

    /// <summary>
    /// Removes a <see cref="DependencyPropertyNamingConvention"/>.
    /// </summary>
    /// <param name="key">The <see cref="DependencyPropertyNamingConvention.Name"/> of the naming convention to remove.</param>
    public void RemoveNamingConvention( string key )
        => this._options = this._options with
        {
            NamingConventionRegistrations =
            this._options.NamingConventionRegistrations.Remove( key )
        };

    /// <summary>
    /// Resets naming convention registrations to the default state, removing any user-registered naming conventions.
    /// </summary>
    public void ResetNamingConventions()
        => this._options = this._options with
        {
            NamingConventionRegistrations =
            this._options.NamingConventionRegistrations
                .ApplyChanges( IncrementalKeyedCollection.Clear<string, NamingConventionRegistration<IDependencyPropertyNamingConvention>>(), default )
                .AddOrApplyChanges( DependencyPropertyOptions.DefaultNamingConventionRegistrations() )
        };

    internal DependencyPropertyOptions Build() => this._options;
}
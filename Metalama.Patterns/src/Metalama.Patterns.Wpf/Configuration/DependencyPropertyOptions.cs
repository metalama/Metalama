// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;
using Metalama.Framework.Serialization;
using Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;
using Metalama.Patterns.Wpf.Implementation.DependencyPropertyNamingConvention;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.Windows;

namespace Metalama.Patterns.Wpf.Configuration;

internal sealed record DependencyPropertyOptions : IHierarchicalOptions<ICompilation>, IHierarchicalOptions<INamespace>, IHierarchicalOptions<INamedType>,
                                                   IHierarchicalOptions<IProperty>
{
    [NonCompileTimeSerialized]
#pragma warning disable CS0169 // False positive
    private IReadOnlyList<IDependencyPropertyNamingConvention>? _namingConventions;
#pragma warning restore CS0169

    /// <summary>
    /// Gets the list of naming conventions that can be used to provide names and find members used to implement the <see cref="DependencyPropertyAttribute"/> aspect.
    /// </summary>
    public IncrementalKeyedCollection<string, NamingConventionRegistration<IDependencyPropertyNamingConvention>> NamingConventionRegistrations { get; init; } =
        IncrementalKeyedCollection<string, NamingConventionRegistration<IDependencyPropertyNamingConvention>>.Empty;

    /// <summary>
    /// Gets a value indicating whether the property should be registered as a read-only property.
    /// </summary>
    public bool? IsReadOnly { get; init; }

    /// <summary>
    /// Gets a value indicating whether the property initializer (if present) should be used to for <see cref="PropertyMetadata.DefaultValue"/>.
    /// The default is <see langword="true"/>.
    /// </summary>
    public bool? InitializerProvidesDefaultValue { get; init; }

    internal IReadOnlyCollection<IDependencyPropertyNamingConvention> GetSortedNamingConventions()
    {
        this._namingConventions ??=
            this.NamingConventionRegistrations
                .Where( r => r.NamingConvention != null )
                .OrderBy( v => v.Priority ?? 0 )
                .ThenBy( v => v.NamingConvention!.Name )
                .Select( v => v.NamingConvention! )
                .ToList();

        return this._namingConventions;
    }

    IHierarchicalOptions IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context )
        => new DependencyPropertyOptions { InitializerProvidesDefaultValue = true, NamingConventionRegistrations = DefaultNamingConventionRegistrations() };

    internal static IncrementalKeyedCollection<string, NamingConventionRegistration<IDependencyPropertyNamingConvention>> DefaultNamingConventionRegistrations()
        => IncrementalKeyedCollection.AddOrApplyChanges<string, NamingConventionRegistration<IDependencyPropertyNamingConvention>>(
            new NamingConventionRegistration<IDependencyPropertyNamingConvention>(
                DefaultCommandNamingConvention.RegistrationKey,
                new DefaultDependencyPropertyNamingConvention(),
                int.MaxValue ) );

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (DependencyPropertyOptions) changes;

        return new DependencyPropertyOptions
        {
            IsReadOnly = other.IsReadOnly ?? this.IsReadOnly,
            InitializerProvidesDefaultValue = other.InitializerProvidesDefaultValue ?? this.InitializerProvidesDefaultValue,
            NamingConventionRegistrations = this.NamingConventionRegistrations.ApplyChanges( other.NamingConventionRegistrations, context )
        };
    }
}
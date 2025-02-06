// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;
using Metalama.Framework.Serialization;
using Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.ComponentModel;
using System.Windows.Input;

namespace Metalama.Patterns.Wpf.Configuration;

internal sealed record CommandOptions : IHierarchicalOptions<ICompilation>, IHierarchicalOptions<INamespace>, IHierarchicalOptions<INamedType>,
                                        IHierarchicalOptions<IMethod>
{
    [NonCompileTimeSerialized]
#pragma warning disable CS0169 // False positive
    private IReadOnlyList<ICommandNamingConvention>? _namingConventions;
#pragma warning restore CS0169

    /// <summary>
    /// Gets the list of naming conventions that can be used to provide names and find members used to implement the <see cref="CommandAttribute"/> aspect.
    /// </summary>
    public IncrementalKeyedCollection<string, NamingConventionRegistration<ICommandNamingConvention>> NamingConventionRegistrations { get; init; } =
        IncrementalKeyedCollection<string, NamingConventionRegistration<ICommandNamingConvention>>.Empty;

    /// <summary>
    /// Gets a value indicating whether integration with <see cref="INotifyPropertyChanged"/> is enabled. The default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="EnableINotifyPropertyChangedIntegration"/> is <see langword="true"/> (the default), and when a can-execute property (not a method) is used,
    /// and when the containing type of the target property implements <see cref="INotifyPropertyChanged"/>,then the <see cref="ICommand.CanExecuteChanged"/> event of 
    /// the command will be raised when the can-execute property changes. A warning is reported if the can-execute property is not public because <see cref="INotifyPropertyChanged"/>
    /// implementations typically only notify changes to public properties.
    /// </para>
    /// </remarks>
    public bool? EnableINotifyPropertyChangedIntegration { get; init; }

    internal IReadOnlyList<ICommandNamingConvention> GetSortedNamingConventions()
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
        => new CommandOptions { EnableINotifyPropertyChangedIntegration = true, NamingConventionRegistrations = DefaultNamingConventionRegistrations() };

    internal static IncrementalKeyedCollection<string, NamingConventionRegistration<ICommandNamingConvention>> DefaultNamingConventionRegistrations()
        => IncrementalKeyedCollection.AddOrApplyChanges<string, NamingConventionRegistration<ICommandNamingConvention>>(
            new NamingConventionRegistration<ICommandNamingConvention>(
                DefaultCommandNamingConvention.RegistrationKey,
                new DefaultCommandNamingConvention(),
                int.MaxValue ) );

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (CommandOptions) changes;

        return new CommandOptions
        {
            NamingConventionRegistrations = this.NamingConventionRegistrations.ApplyChanges( other.NamingConventionRegistrations, context ),
            EnableINotifyPropertyChangedIntegration = other.EnableINotifyPropertyChangedIntegration ?? this.EnableINotifyPropertyChangedIntegration
        };
    }
}
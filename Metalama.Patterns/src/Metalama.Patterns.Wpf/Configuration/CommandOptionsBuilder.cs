// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Options;
using Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;
using System.ComponentModel;
using System.Windows.Input;

namespace Metalama.Patterns.Wpf.Configuration;

/// <summary>
/// Arguments of the <see cref="CommandExtensions.ConfigureCommand(Metalama.Framework.Aspects.IAspectReceiver{Metalama.Framework.Code.ICompilation},System.Action{Metalama.Patterns.Wpf.Configuration.CommandOptionsBuilder})"/>
/// method.
/// </summary>
[PublicAPI]
[CompileTime]
public sealed class CommandOptionsBuilder
{
    private CommandOptions _options = new();
    private int _nextPriority;

    /// <summary>
    /// Gets the key of the default naming convention.
    /// </summary>
    public static string DefaultNamingConventionName => DefaultCommandNamingConvention.RegistrationKey;

    /// <summary>
    /// Adds or updates a naming convention that specifies, based on the name of the target method of the <see cref="CommandAttribute"/>
    /// aspect: the name of the command property and the name of the <c>CanExecute</c> method or property.
    /// </summary>
    /// <param name="namingConvention">A <see cref="CommandNamingConvention"/>.</param>
    /// <param name="priority">The priority of the naming convention. By default, the priority is 0 for the first call of
    /// this method, then it is incremented at every call.</param>
    /// <remarks>
    /// <para>If a <see cref="CommandNamingConvention"/> of the same <see cref="CommandNamingConvention.Name"/> has already been registered,
    /// this call replaces the old instance by the new one, including the new <paramref name="priority"/>.</para>
    /// </remarks>
    public void AddNamingConvention(
        CommandNamingConvention namingConvention,
        int? priority = null )
    {
        if ( namingConvention.Name == DefaultCommandNamingConvention.RegistrationKey )
        {
            throw new InvalidOperationException( "The default naming convention cannot be modified." );
        }

        var priorityValue = priority.GetValueOrDefault( this._nextPriority );
        this._nextPriority = priorityValue + 1;

        this._options = this._options with
        {
            NamingConventionRegistrations =
            this._options.NamingConventionRegistrations.AddOrApplyChanges(
                new NamingConventionRegistration<ICommandNamingConvention>(
                    namingConvention.Name,
                    namingConvention,
                    priorityValue ) )
        };
    }

    /// <summary>
    /// Changes the priority of a <see cref="CommandNamingConvention"/>.
    /// </summary>
    /// <param name="name">The <see cref="CommandNamingConvention.Name"/> of the <see cref="CommandNamingConvention"/>.</param>
    /// <param name="priority">The new priority of the <see cref="CommandNamingConvention"/>.</param>
    public void SetNamingConventionPriority( string name, int priority )
        => this._options = this._options with
        {
            NamingConventionRegistrations =
            this._options.NamingConventionRegistrations.AddOrApplyChanges( new NamingConventionRegistration<ICommandNamingConvention>( name, null, priority ) )
        };

    /// <summary>
    /// Removes a <see cref="CommandNamingConvention"/>.
    /// </summary>
    /// <param name="name">The <see cref="CommandNamingConvention.Name"/> of the <see cref="CommandNamingConvention"/>.</param>
    public void RemoveNamingConvention( string name )
        => this._options = this._options with
        {
            NamingConventionRegistrations =
            this._options.NamingConventionRegistrations.Remove( name )
        };

    /// <summary>
    /// Resets naming convention registrations to the default state, removing any user-registered naming conventions.
    /// </summary>
    public void ResetNamingConventions()
        => this._options = this._options with
        {
            NamingConventionRegistrations =
            this._options.NamingConventionRegistrations
                .ApplyChanges( IncrementalKeyedCollection.Clear<string, NamingConventionRegistration<ICommandNamingConvention>>(), default )
                .AddOrApplyChanges( CommandOptions.DefaultNamingConventionRegistrations() )
        };

    /// <summary>
    /// Gets or sets a value indicating whether integration with <see cref="INotifyPropertyChanged"/> is enabled. The default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="EnableINotifyPropertyChangedIntegration"/> is <see langword="true"/> (the default), and when a can-execute property (not a method) is used,
    /// and when the containing type of the target property implements <see cref="INotifyPropertyChanged"/>,then the <see cref="ICommand.CanExecuteChanged"/> event of 
    /// the command will be raised when the can-execute property changes. A warning is reported if the can-execute property is not public because <see cref="INotifyPropertyChanged"/>
    /// implementations typically only notify changes to public properties.
    /// </para>
    /// </remarks>
    public bool? EnableINotifyPropertyChangedIntegration
    {
        get => this._options.EnableINotifyPropertyChangedIntegration;
        set => this._options = this._options with { EnableINotifyPropertyChangedIntegration = value };
    }

    internal CommandOptions Build() => this._options;
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Metalama.Backstage.Commands.Configuration;

internal abstract class ConfigurationFileCommandAdapter
{
    public abstract string Alias { get; }

    public abstract string? Description { get; }

    public virtual string? EnvironmentVariableName => null;

    public abstract void Print( ExtendedCommandContext context );

    public abstract void Reset( ExtendedCommandContext context );

    public abstract void Edit( ExtendedCommandContext context );

    public abstract void Validate( ExtendedCommandContext context );
}

#pragma warning disable SA1402

internal sealed class ConfigurationFileCommandAdapter<T> : ConfigurationFileCommandAdapter
    where T : ConfigurationFile, new()
{
    private readonly ConfigurationFileAttribute _attribute;

    public ConfigurationFileCommandAdapter()
    {
        this._attribute = typeof(T).GetCustomAttribute<ConfigurationFileAttribute>()
                          ?? throw new InvalidOperationException(
                              $"'{nameof(ConfigurationFileAttribute)}' custom attribute not found for '{typeof(T).FullName}' type." );

        this.Description = typeof(T).GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    public override string Alias => this._attribute.Alias;

    public override string? Description { get; }

    public override string? EnvironmentVariableName => this._attribute.EnvironmentVariableName;

    public override void Print( ExtendedCommandContext context )
    {
        var configurationManager = context.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();

        var configuration = configurationManager.Get( typeof(T) );

        context.Console.WriteMessage( configuration.ToJson() );
    }

    public override void Reset( ExtendedCommandContext context )
    {
        var configurationManager = context.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();

        configurationManager.Update<T>( _ => new T() );

        context.Console.WriteSuccess( $"The configuration '{this.Alias}' has been reset." );
    }

    public override void Edit( ExtendedCommandContext context )
    {
        var configurationManager = context.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();

        configurationManager.CreateIfMissing<T>();

        var filePath = configurationManager.GetFilePath<T>();
        context.Console.WriteSuccess( $"Opening '{filePath}' in the default editor." );

        Process.Start( new ProcessStartInfo( filePath ) { UseShellExecute = true } );
    }

    public override void Validate( ExtendedCommandContext context )
    {
        var configurationManager = context.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();

        // The side effect of getting the configuration is to get the warnings.
        _ = configurationManager.Get( typeof(T) );

        if ( context.Console is { HasErrors: false, HasWarnings: false } )
        {
            context.Console.WriteSuccess( $"The file '{configurationManager.GetFilePath<T>()}' is correct." );
        }
    }
}
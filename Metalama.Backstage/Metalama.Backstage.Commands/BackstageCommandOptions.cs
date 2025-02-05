// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Commands.Configuration;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.UserInterface;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;

namespace Metalama.Backstage.Commands;

public sealed class BackstageCommandOptions
{
    private readonly AnsiSupport _ansiSupport;
    private readonly Dictionary<string, ConfigurationFileCommandAdapter> _configurationFileCommandAdapters = [];

    public BackstageCommandOptions(
        IApplicationInfo applicationInfo,
        TextWriter? standardOutput = null,
        TextWriter? errorOutput = null,
        AnsiSupport ansiSupport = AnsiSupport.Detect ) : this( new CommandServiceProvider( applicationInfo ), standardOutput, errorOutput, ansiSupport ) { }

    internal BackstageCommandOptions(
        ICommandServiceProviderProvider serviceProvider,
        TextWriter? standardOutput = null,
        TextWriter? errorOutput = null,
        AnsiSupport ansiSupport = AnsiSupport.Detect )
    {
        this._ansiSupport = ansiSupport;
        this.ServiceProvider = serviceProvider;
        this.StandardOutput = standardOutput ?? Console.Out;
        this.ErrorOutput = errorOutput ?? Console.Error;
        this.AddConfigurationFileAdapter<DiagnosticsConfiguration>();
        this.AddConfigurationFileAdapter<ToastNotificationsConfiguration>();
    }

    internal void ConfigureConsole( AnsiConsoleSettings settings )
    {
        settings.Ansi = this._ansiSupport;
        settings.Interactive = InteractionSupport.No;
    }

    internal ICommandServiceProviderProvider ServiceProvider { get; }

    public TextWriter StandardOutput { get; }

    public TextWriter ErrorOutput { get; }

    internal IReadOnlyDictionary<string, ConfigurationFileCommandAdapter> ConfigurationFileCommandAdapters => this._configurationFileCommandAdapters;

    [PublicAPI]
    public void AddConfigurationFileAdapter<T>()
        where T : ConfigurationFile, new()
    {
        var adapter = new ConfigurationFileCommandAdapter<T>();
        this._configurationFileCommandAdapters.Add( adapter.Alias, adapter );
    }
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Commands.Configuration;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface.Rss;
using Metalama.Backstage.UserInterface.Toasts;
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
        AnsiSupport ansiSupport = AnsiSupport.Detect ) : this( applicationInfo, MetalamaProduct.Profile, standardOutput, errorOutput, ansiSupport ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackstageCommandOptions"/> class for a given product.
    /// </summary>
    public BackstageCommandOptions(
        IApplicationInfo applicationInfo,
        ProductProfile productProfile,
        TextWriter? standardOutput = null,
        TextWriter? errorOutput = null,
        AnsiSupport ansiSupport = AnsiSupport.Detect ) : this(
        new CommandServiceProvider( applicationInfo, productProfile ),
        standardOutput,
        errorOutput,
        ansiSupport )
    {
        this.ProductProfile = productProfile;
    }

    /// <summary>
    /// Gets the profile of the product, whose name appears in the descriptions of the commands.
    /// </summary>
    public ProductProfile ProductProfile { get; } = MetalamaProduct.Profile;

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
        this.AddConfigurationFileAdapter<RssClientConfiguration>();
        this.AddConfigurationFileAdapter<TelemetryConfiguration>();
    }

    internal void ConfigureConsole( AnsiConsoleSettings settings )
    {
        settings.Ansi = this._ansiSupport;
        settings.Interactive = InteractionSupport.No;
    }

    internal ICommandServiceProviderProvider ServiceProvider { get; }

    internal TextWriter StandardOutput { get; }

    internal TextWriter ErrorOutput { get; }

    internal IReadOnlyDictionary<string, ConfigurationFileCommandAdapter> ConfigurationFileCommandAdapters => this._configurationFileCommandAdapters;

    [PublicAPI]
    public void AddConfigurationFileAdapter<T>()
        where T : ConfigurationFile, new()
    {
        var adapter = new ConfigurationFileCommandAdapter<T>();
        this._configurationFileCommandAdapters.Add( adapter.Alias, adapter );
    }
}
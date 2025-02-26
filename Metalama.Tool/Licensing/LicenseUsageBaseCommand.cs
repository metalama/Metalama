// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Commands;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.Engine.Licensing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Metalama.Tool.Licensing;

// ReSharper disable once ClassNeverInstantiated.Global
[UsedImplicitly]
internal abstract class LicenseUsageBaseCommand : BaseCommand<LicenseUsageCommandSettings>
{
    protected sealed override void Execute( ExtendedCommandContext context, LicenseUsageCommandSettings settings )
    {
        var horizon = settings.GetHorizon();

        context.Console.WriteMessage( $"Considering builds from {horizon:f}. Use -d, -w or -h option to change the time horizon." );

        // Check horizon.
        if ( (DateTime.Now - horizon).TotalDays > 31 )
        {
            throw new CommandException( "The time horizon cannot be larger than 30 days because data files are automatically cleaned up over this horizon." );
        }

        // Check license settings.
        var licenseService = context.ServiceProvider.GetRequiredBackstageService<ILicenseConsumptionService>();

        var licenseConsumer = licenseService.CreateConsumer();

        if ( !licenseConsumer.IsTrialLicense )
        {
            context.Console.WriteWarning(
                "The trial mode is currently not activated. License usage data is therefore only being collected " +
                "when projects are built with the parameter `/p:MetalamaWriteLicenseUsageData=True` or when the build " +
                "fails because too many aspect classes have been used." );
        }

        var projectFilters = settings.GetProjectRegexes();

        // Read the files.
        var tempFileManager = context.ServiceProvider.GetRequiredBackstageService<ITempFileManager>();

        var files = new List<LicenseConsumptionFile>();

        var hasUnselectedFile = false;

        foreach ( var path in LicenseVerifier.GetConsumptionDataFiles( tempFileManager ) )
        {
            try
            {
                var file = LicenseConsumptionFile.FromFile( path );

                if ( file == null )
                {
                    continue;
                }

                hasUnselectedFile = true;

                var projectName = Path.GetFileNameWithoutExtension( file.ProjectPath );

                // Apply the filters.
                if ( file.BuildTime < horizon )
                {
                    continue;
                }

                if ( projectFilters.Count > 0 && !projectFilters.Any( r => r.IsMatch( projectName ) ) )
                {
                    continue;
                }

                if ( !string.IsNullOrWhiteSpace( settings.Configurations )
                     && !LicenseUsageCommandSettings.SplitCommaSeparatedList( settings.Configurations )
                         .Any( c => string.Equals( c, file.Configuration, StringComparison.OrdinalIgnoreCase ) ) )
                {
                    continue;
                }

                static string GetTargetFrameworkOrEmpty( string s ) => string.IsNullOrWhiteSpace( s ) ? "empty" : s;

                if ( !string.IsNullOrWhiteSpace( settings.TargetFrameworks )
                     && !LicenseUsageCommandSettings.SplitCommaSeparatedList( settings.TargetFrameworks )
                         .Any( c => string.Equals( c, GetTargetFrameworkOrEmpty( file.TargetFramework ), StringComparison.OrdinalIgnoreCase ) ) )
                {
                    continue;
                }

                files.Add( file );
            }
            catch ( Exception e )
            {
                context.Console.WriteWarning( $"Cannot read '{path}': {e.Message}" );
            }
        }

        if ( files.Count == 0 )
        {
            if ( hasUnselectedFile )
            {
                throw new CommandException( "No build matches the specified filters." );
            }
            else
            {
                throw new CommandException( "No license usage data has been collected yet." );
            }
        }

        // Execute the command.
        this.Execute( new LicenseUsageCommandContext( context, files ) );
    }

    protected abstract void Execute( LicenseUsageCommandContext context );
}
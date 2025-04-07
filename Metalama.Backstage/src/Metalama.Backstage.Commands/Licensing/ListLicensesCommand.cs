// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Registration;
using Spectre.Console;
using System;
using System.Globalization;
using System.Linq;

namespace Metalama.Backstage.Commands.Licensing
{
    internal class ListLicensesCommand : BaseCommand<BaseCommandSettings>
    {
        protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
        {
            var licenses = context.ServiceProvider.GetRequiredBackstageService<ILicenseRegistrationService>().RegisteredLicenses.ToList();

            if ( licenses.Count > 0 )
            {
                foreach ( var license in licenses )
                {
                    context.Console.WriteMessage( "The following license is currently registered:" + Environment.NewLine );

                    static string? Format( DateTime? dateTime )
                    {
                        if ( dateTime == null )
                        {
                            return null;
                        }

                        return dateTime.Value.ToString( "D", CultureInfo.InvariantCulture );
                    }

                    var table = new Table();
                    table.AddColumn( "Field" );
                    table.AddColumn( "Value" );

                    void AddRow( string description, string? value )
                    {
                        if ( value != null )
                        {
                            table.AddRow( description, value );
                        }
                    }

                    var data = license;

                    AddRow( "License ID", data.LicenseId?.ToString( CultureInfo.InvariantCulture ) );

                    if ( data.LicenseId != null )
                    {
                        AddRow( "License Key", license.LicenseString );
                    }

                    AddRow( "Description", data.Description );
                    AddRow( "Licensee", data.Licensee );

                    string? expiration = null;

                    if ( data.Perpetual != null )
                    {
                        if ( data.Perpetual.Value )
                        {
                            expiration = "Never (perpetual license)";
                        }
                        else
                        {
                            expiration = Format( data.ValidTo );
                        }
                    }

                    AddRow( "License Expiration", expiration );
                    AddRow( "Maintenance Expiration", Format( data.SubscriptionEndDate ) );
                    AddRow( "Eligible Servicing Phases", data.ServicingPhase.GetDisplayName( true ) );

                    context.Console.Out.Write( table );
                }
            }
            else
            {
                context.Console.WriteWarning( "No Metalama license is currently registered." );
            }
        }
    }
}
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Registration;
using Spectre.Console;
using System;
using System.Globalization;

namespace Metalama.Backstage.Commands.Licensing
{
    internal class ListLicensesCommand : BaseCommand<BaseCommandSettings>
    {
        protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
        {
            var license = context.ServiceProvider.GetRequiredBackstageService<ILicenseRegistrationService>().RegisteredLicense;

            if ( license != null )
            {
                context.Console.WriteMessage( "The following license is currently registered:" + Environment.NewLine );

                string? Format( DateTime? dateTime )
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

                void Write( string description, string? value )
                {
                    if ( value != null )
                    {
                        table.AddRow( description, value );
                    }
                }

                var data = license;

                Write( "License ID", data.LicenseId?.ToString( CultureInfo.InvariantCulture ) );

                if ( data == null || data.LicenseId != null )
                {
                    Write( "License Key", license.LicenseString );
                }

                if ( data != null )
                {
                    Write( "Description", data.Description );
                    Write( "Licensee", data.Licensee );

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

                    Write( "License Expiration", expiration );
                    Write( "Maintenance Expiration", Format( data.SubscriptionEndDate ) );
                }

                context.Console.Out.Write( table );
            }
            else
            {
                context.Console.WriteWarning( "No Metalama license is currently registered." );
            }
        }
    }
}
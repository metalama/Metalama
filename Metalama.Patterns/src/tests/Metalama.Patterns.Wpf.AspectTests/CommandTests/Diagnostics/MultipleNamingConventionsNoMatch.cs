// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Fabrics;
using Metalama.Patterns.Wpf.Configuration;

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.Diagnostics.MultipleNamingConventionsNoMatch;

internal class NsFabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        amender.ConfigureCommand(
            b =>
            {
                b.RemoveNamingConvention( "default" );
                
                b.AddNamingConvention(
                    new CommandNamingConvention( "rx1" )
                    {
                        CommandNamePattern = "^Rx1(?<CommandName>.+)$",
                    } );

                b.AddNamingConvention(
                    new CommandNamingConvention( "rx2" )
                    {
                        CommandNamePattern = "^Rx2(?<CommandName>.+)$",
                    } );

                b.AddNamingConvention(
                    new CommandNamingConvention( "rx3" )
                    {
                        CommandNamePattern = "^Rx3(?<CommandName>.+)$",
                    } );

                b.RemoveNamingConvention( CommandOptionsBuilder.DefaultNamingConventionName );
            } );
    }
}

// <target>
internal class MultipleNamingConventionsNoMatch
{
    [Command]
    private void ExecuteFoo() { }
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
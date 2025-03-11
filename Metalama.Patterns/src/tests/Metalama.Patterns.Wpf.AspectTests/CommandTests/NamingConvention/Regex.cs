// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Fabrics;
using Metalama.Patterns.Wpf.Configuration;

namespace Metalama.Patterns.Wpf.AspectTests.CommandTests.NamingConvention.Regex;

internal class NsFabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        amender.ConfigureCommand(
            b => b.AddNamingConvention(
                new CommandNamingConvention( "rx1" )
                {
                    CommandNamePattern = "^MakeIt(?<CommandName>.+)$",
                    CommandPropertyName = "The{CommandName}Command",
                    CanExecutePatterns = ["(CanIt{CommandName})|({CommandName}ItCan)"]
                } ) );
    }
}

// <target>
internal class Regex
{
    [Command]
    private void MakeItBeep() { }

    // Matches regex naming convention:    
    private bool CanItBeep() => true;

    // Not matched by regex naming convention, so should not be ambiguous:
    private bool CanExecuteBeep() => true;

    [Command]
    private void MakeItUseTheForce() { }

    // Also matches regex naming convention:    
    private bool UseTheForceItCan() => true;

    // Does not match regex naming convention, fallthrough to default naming convention:
    [Command]
    private void ExecuteFoo() { }

    private bool CanExecuteFoo() => true;

    // Not matched by default naming convention, so should not be ambiguous:
    private bool ItCanFoo() => true;
}
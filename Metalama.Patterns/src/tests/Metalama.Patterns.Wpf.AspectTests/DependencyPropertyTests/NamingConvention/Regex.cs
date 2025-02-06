// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Fabrics;
using Metalama.Patterns.Wpf.Configuration;
using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.NamingConvention.Regex;

internal class NsFabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        amender.ConfigureDependencyProperty(
            b => b.AddNamingConvention(
                new DependencyPropertyNamingConvention( "rx1" )
                {
                    PropertyNamePattern = "^Yoda(?<Name>.+)$",
                    OnPropertyChangedPattern = "^(Do|Make){PropertyName}Changed$",
                    ValidatePattern = "^Is{PropertyName}Valid",
                    RegistrationFieldName = "The{PropertyName}PropertyItIs"
                } ) );
    }
}

// <target>
internal class Regex : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    private void OnFooChanged() { }

    private void ValidateFoo( int v ) { }

    [DependencyProperty]
    public string YodaFoo { get; set; }

    private void MakeFooChanged( string a, string b ) { }

    private void IsFooValid( string s ) { }
}
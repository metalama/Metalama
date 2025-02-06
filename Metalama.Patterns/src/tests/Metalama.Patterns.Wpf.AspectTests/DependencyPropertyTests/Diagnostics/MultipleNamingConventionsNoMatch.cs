// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Fabrics;
using Metalama.Patterns.Wpf.Configuration;
using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Diagnostics.MultipleNamingConventionsNoMatch;

internal class NsFabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        amender.ConfigureDependencyProperty(
            b =>
            {
                b.AddNamingConvention(
                    new DependencyPropertyNamingConvention( "rx1" )
                    {
                        PropertyNamePattern = "(?<PropertyName>.*)Rx1", OnPropertyChangedPattern = "{PropertyName}Rx1Ed"
                    } );

                b.AddNamingConvention(
                    new DependencyPropertyNamingConvention( "rx2" )
                    {
                        PropertyNamePattern = "(?<PropertyName>.*)Rx1", OnPropertyChangedPattern = "{PropertyName}Rx2Ed"
                    } );

                b.AddNamingConvention(
                    new DependencyPropertyNamingConvention( "rx3" )
                    {
                        PropertyNamePattern = "(?<PropertyName>.*)Rx1", OnPropertyChangedPattern = "{PropertyName}Rx3Ed"
                    } );

                b.RemoveNamingConvention( CommandOptionsBuilder.DefaultNamingConventionName );
            } );
    }
}

// <target>
internal class MultipleNamingConventionsNoMatch : DependencyObject
{
    [DependencyProperty]
    public int Foo { get; set; }

    // Convention `rx1-name` => changing ambiguous, changed ok

    private void FooRx1Ing() { }

    private void FooRx1Ing( int v ) { }

    private void FooRx1Ed() { }

    // Convention `rx2-name` => changing invalid

    private string? FooRx2Ing( List<int> v ) => null;

    // Convention `rx3-name` => changing missing
}
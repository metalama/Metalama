// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Contracts;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Metalama.Patterns.Wpf.UnitTests.Assets.DependencyPropertyNS;

public partial class ContractsIntegrationTestClass : DependencyObject
{
    public List<string> Operations { get; } = new();

    private void Log( string? msg = null, [CallerMemberName] string? callerName = null )
    {
        this.Operations.Add(
            msg == null
                ? callerName ?? "<missing>"
                : $"{callerName}|{msg}" );
    }

    [DependencyProperty]
    [Trim]
    [NotNull]
    public string Name { get; set; }

    private void OnNameChanged( string oldValue, string newValue ) => this.Log( $"{oldValue}|{newValue}" );

    private void ValidateName( string value )
    {
        this.Log( value );
    }
}
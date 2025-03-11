// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
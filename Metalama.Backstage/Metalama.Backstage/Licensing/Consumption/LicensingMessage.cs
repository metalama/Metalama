// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Licensing.Consumption;

public record LicensingMessage( string Text )
{
    public bool IsError { get; init; }

    public override string ToString() => $"{(this.IsError ? "Error" : "Warning")}: {this.Text}";
}
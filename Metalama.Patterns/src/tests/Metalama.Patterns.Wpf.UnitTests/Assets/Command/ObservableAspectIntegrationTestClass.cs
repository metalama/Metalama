// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Observability;

namespace Metalama.Patterns.Wpf.UnitTests.Assets.Command;

[Observable]
public partial class ObservableAspectIntegrationTestClass : CommandAssetBase
{
    [Command]
    private void ExecuteFoo( int v )
    {
        LogCall( $"{v}" );
    }

    public bool CanExecuteFoo { get; set; }
}
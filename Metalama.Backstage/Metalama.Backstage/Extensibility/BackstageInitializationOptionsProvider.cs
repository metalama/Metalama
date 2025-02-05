// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Extensibility;

internal class BackstageInitializationOptionsProvider : IBackstageService
{
    public BackstageInitializationOptionsProvider( BackstageInitializationOptions options )
    {
        this.Options = options;
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public BackstageInitializationOptions Options { get; }
}
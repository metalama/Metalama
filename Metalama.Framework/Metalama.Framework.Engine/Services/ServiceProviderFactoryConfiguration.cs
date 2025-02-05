// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Engine.Services;

[PublicAPI]
public sealed record ServiceProviderFactoryConfiguration
{
    public IServiceProvider? NextProvider { get; init; }

    public AdditionalServiceCollection? AdditionalServices { get; init; }
}
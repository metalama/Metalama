// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface;

namespace Metalama.Backstage.Application;

/// <summary>
/// The values that bind the Backstage services to a product family: the profile, the web links, the telemetry
/// endpoints, the user interface addresses and the license product catalog. A product defines one instance, for
/// instance <c>MetalamaProduct.Instance</c>, and passes it wherever the services are composed.
/// </summary>
/// <param name="Profile">The profile of the product family.</param>
/// <param name="WebLinks">The web links of the product.</param>
/// <param name="TelemetryOptions">The telemetry endpoints of the product.</param>
/// <param name="UserInterfaceOptions">The user interface addresses of the product.</param>
/// <param name="LicenseProductCatalog">The catalog of the products whose license keys the product family consumes.</param>
[PublicAPI]
public sealed record BackstageProduct(
    ProductProfile Profile,
    IWebLinks WebLinks,
    TelemetryInitializationOptions TelemetryOptions,
    UserInterfaceInitializationOptions UserInterfaceOptions,
    ILicenseProductCatalog LicenseProductCatalog );

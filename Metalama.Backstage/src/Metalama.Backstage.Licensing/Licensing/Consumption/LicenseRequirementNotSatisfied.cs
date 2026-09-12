// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Licensing.Consumption;

/// <summary>
/// The event published by the licensing services when a component required a license and no registered license
/// satisfied the requirement. The user interface subscribes to it to invite the user to register a license.
/// </summary>
/// <param name="Requirement">The requirement that was not satisfied.</param>
/// <param name="Message">The message that explains what is not licensed and which products are eligible.</param>
[PublicAPI]
public sealed record LicenseRequirementNotSatisfied( LicenseRequirement Requirement, string Message );

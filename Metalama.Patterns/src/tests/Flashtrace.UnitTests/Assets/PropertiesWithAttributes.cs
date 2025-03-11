// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Records;

namespace Flashtrace.UnitTests.Assets;

// Resharper disable UnusedMember.Global
internal sealed class PropertiesWithAttributes
{
    [LoggingPropertyOptions( IsIgnored = true )]
    public string Ignored { get; set; }

    [LoggingPropertyOptions( IsInherited = true )]
    public string Inherited { get; set; }
}
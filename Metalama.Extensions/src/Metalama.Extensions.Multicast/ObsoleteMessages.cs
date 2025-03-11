// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Extensions.Multicast;

[CompileTime]
internal static class ObsoleteMessages
{
    public const string ExternalAssemblies = "Multicasting to external assemblies is not supported in Metalama.";
    public const string? AttributeReplace = "AttributeReplace is true by default and cannot be set to false.";
    public const string? Inheritance = "Inheritance is decided at the class level using the [Inheritable] attribute.";

    // We cannot have real errors because the template compiler will fail, but we could fix that in the future.
    public const bool Error = false;
}
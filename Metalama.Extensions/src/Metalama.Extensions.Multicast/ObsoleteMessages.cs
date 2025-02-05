// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
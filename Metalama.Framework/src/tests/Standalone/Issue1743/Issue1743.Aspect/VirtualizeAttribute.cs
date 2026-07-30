// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Issue1743
{
    /// <summary>
    /// Makes the public methods of the target type virtual, through the weaver named below.
    /// </summary>
    /// <remarks>
    /// The weaver is named by string, and the name resolves to a plug-in type that two referenced assemblies both
    /// provide. See the README of this directory.
    /// </remarks>
    [RequireAspectWeaver( "Issue1743.DuplicatedWeaver" )]
    public class VirtualizeAttribute : TypeAspect { }
}

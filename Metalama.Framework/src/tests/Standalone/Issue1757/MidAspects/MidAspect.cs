// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace MidAspects;

/// <summary>
/// An aspect compiled against a released Metalama of the current design-time contracts generation.
/// </summary>
public class MidAspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public string GetMidMessage() => "mid";
}

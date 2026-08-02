// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Issue1744;

/// <summary>
/// An aspect, whose only purpose is to make the compile-time pipeline initialize, which is what runs the nested
/// reference-assembly build that this scenario makes fail.
/// </summary>
public class Aspect : TypeAspect
{
    [Introduce]
    public string GetMessage() => "ok";
}

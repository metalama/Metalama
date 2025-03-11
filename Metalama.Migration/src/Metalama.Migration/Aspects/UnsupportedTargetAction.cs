// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects
{
    /// <summary>
    /// There is no equivalent in Metalama because Metalama will throw an exception if the target is not supported.
    /// </summary>
    public enum UnsupportedTargetAction
    {
        Default = 0,

        Fail = Default,

        Ignore,

        Fallback
    }
}
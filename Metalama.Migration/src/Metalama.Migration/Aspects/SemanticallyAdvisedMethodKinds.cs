// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Aspects
{
    [Flags]
    public enum SemanticallyAdvisedMethodKinds
    {
        None = 0,

        Async = 1 << 0,

        ReturnsAwaitable = 1 << 1,

        Iterator = 1 << 2,

        ReturnsEnumerable = 1 << 3,

        AsyncIterator = 1 << 4,

        Default = Async | ReturnsAwaitable | Iterator | AsyncIterator,

        All = Default | ReturnsEnumerable
    }
}
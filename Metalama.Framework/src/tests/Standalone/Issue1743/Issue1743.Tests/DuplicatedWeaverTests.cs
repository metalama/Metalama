// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

#pragma warning disable CA1822 // Mark members as static

namespace Issue1743.Tests
{
    /// <summary>
    /// The type the aspect is applied to. Its methods must become virtual, which proves that the weaver ran even
    /// though two referenced assemblies provide it.
    /// </summary>
    [Virtualize]
    public class Target
    {
        /// <summary>
        /// A method the weaver is expected to make virtual.
        /// </summary>
        public void Bar() { }
    }

    /// <summary>
    /// Tests that a project referencing two assemblies that provide the same aspect weaver builds and weaves.
    /// </summary>
    /// <remarks>
    /// Covers issue #1743. Building this project at all is the regression test: before the fix, the duplicate
    /// weaver aborted <c>AspectPipeline.TryInitialize</c> with an <see cref="System.ArgumentException"/> from
    /// <c>ImmutableDictionary</c>, so the compilation failed. The assertion below additionally verifies that
    /// deduplicating kept a usable weaver rather than dropping the aspect.
    /// </remarks>
    public class DuplicatedWeaverTests
    {
        [Fact]
        public void WeaverRuns()
        {
            Assert.True( typeof(Target).GetMethod( nameof(Target.Bar) )!.IsVirtual );
        }
    }
}

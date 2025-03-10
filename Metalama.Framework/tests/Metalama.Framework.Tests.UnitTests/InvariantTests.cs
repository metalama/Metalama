// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests
{
    public sealed class InvariantTests
    {
        // These tests essentially verify that the behavior is the same in the debug and release mode.

        [Fact]
        public void AssertExecutesArgument()
        {
            var called = false;

            Invariant.Assert(
                Execute(
                    () =>
                    {
                        called = true;

                        return true;
                    } ) );

            Assert.True( called );
        }

        [Fact]
        public void ImpliesExecutesArgument()
        {
            var called = false;

            Invariant.Implies(
                true,
                Execute(
                    () =>
                    {
                        called = true;

                        return true;
                    } ) );

            Assert.True( called );
        }

        private static bool Execute( Func<bool> action ) => action();
    }
}
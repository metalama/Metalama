// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class Test : LongLivedMarshalByRefObject, ITest
    {
        public string DisplayName => this.TestCase.DisplayName;

        public ITestCase TestCase { get; }

        public Test( ITestCase testCase )
        {
            this.TestCase = testCase;
        }
    }
}
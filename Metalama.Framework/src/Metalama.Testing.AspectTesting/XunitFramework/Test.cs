// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// The single test of a <see cref="XunitFramework.TestCase"/>.
    /// </summary>
    internal sealed class Test : ITest
    {
        public Test( TestCase testCase )
        {
            this.TestCase = testCase;
            this.UniqueID = UniqueIDGenerator.ForTest( testCase.UniqueID, 0 );
        }

        public TestCase TestCase { get; }

        ITestCase ITest.TestCase => this.TestCase;

        public string TestDisplayName => this.TestCase.TestCaseDisplayName;

        public string? TestLabel => null;

        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

        public string UniqueID { get; }
    }
}

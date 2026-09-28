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
        /// <summary>
        /// Initializes a new instance of the <see cref="Test"/> class for a test case.
        /// </summary>
        public Test( TestCase testCase )
        {
            this.TestCase = testCase;
            this.UniqueID = UniqueIDGenerator.ForTest( testCase.UniqueID, 0 );
        }

        /// <summary>
        /// Gets the test case that the test runs.
        /// </summary>
        public TestCase TestCase { get; }

        ITestCase ITest.TestCase => this.TestCase;

        /// <inheritdoc />
        public string TestDisplayName => this.TestCase.TestCaseDisplayName;

        /// <inheritdoc />
        public string? TestLabel => null;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

        /// <inheritdoc />
        public string UniqueID { get; }
    }
}

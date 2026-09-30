// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// The single test collection of a test assembly.
    /// </summary>
    internal sealed class TestCollection : ITestCollection
    {
        /// <summary>
        /// The display name of the single test collection.
        /// </summary>
        private const string _displayName = "All tests";

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCollection"/> class.
        /// </summary>
        public TestCollection( TestAssembly assembly )
        {
            this.TestAssembly = assembly;
            this.UniqueID = UniqueIDGenerator.ForTestCollection( assembly.UniqueID, _displayName, null );
        }

        /// <inheritdoc />
        public ITestAssembly TestAssembly { get; }

        /// <inheritdoc />
        public string? TestCollectionClassName => null;

        /// <inheritdoc />
        public string TestCollectionDisplayName => _displayName;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

        /// <inheritdoc />
        public string UniqueID { get; }
    }
}

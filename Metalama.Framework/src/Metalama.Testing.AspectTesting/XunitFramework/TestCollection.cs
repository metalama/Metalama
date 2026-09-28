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
        private const string _displayName = "All tests";

        public TestCollection( TestAssembly assembly )
        {
            this.TestAssembly = assembly;
            this.UniqueID = UniqueIDGenerator.ForTestCollection( assembly.UniqueID, _displayName, null );
        }

        public ITestAssembly TestAssembly { get; }

        public string? TestCollectionClassName => null;

        public string TestCollectionDisplayName => _displayName;

        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

        public string UniqueID { get; }
    }
}

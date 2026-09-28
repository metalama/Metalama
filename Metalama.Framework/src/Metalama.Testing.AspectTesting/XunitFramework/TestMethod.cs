// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.IO;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// The test method that represents a test file.
    /// </summary>
    internal sealed class TestMethod : ITestMethod
    {
        public TestMethod( TestFactory factory, string relativePath )
        {
            this.TestClass = factory.GetTestType( Path.GetDirectoryName( relativePath ) );
            this.MethodName = Path.GetFileNameWithoutExtension( relativePath );
            this.UniqueID = UniqueIDGenerator.ForTestMethod( this.TestClass.UniqueID, this.MethodName );
        }

        public ITestClass TestClass { get; }

        public int? MethodArity => null;

        public string MethodName { get; }

        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Traits => TestFactory.EmptyTraits;

        public string UniqueID { get; }
    }
}

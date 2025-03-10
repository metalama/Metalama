// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class TestCollection : LongLivedMarshalByRefObject, ITestCollection
    {
        private readonly TestAssembly _assembly;

        public TestCollection( TestAssembly assembly )
        {
            this._assembly = assembly;
        }

        void IXunitSerializable.Deserialize( IXunitSerializationInfo info ) { }

        void IXunitSerializable.Serialize( IXunitSerializationInfo info ) { }

        ITypeInfo ITestCollection.CollectionDefinition => null!;

        string ITestCollection.DisplayName => "All tests";

        ITestAssembly ITestCollection.TestAssembly => this._assembly;

        Guid ITestCollection.UniqueID { get; } = Guid.NewGuid();
    }
}
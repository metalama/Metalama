// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
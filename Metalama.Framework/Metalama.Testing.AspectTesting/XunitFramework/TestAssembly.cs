// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting.XunitFramework;

internal sealed class TestAssembly : LongLivedMarshalByRefObject, ITestAssembly
{
    private readonly TestFactory _factory;

    public TestAssembly( TestFactory factory )
    {
        this._factory = factory;
    }

    void IXunitSerializable.Deserialize( IXunitSerializationInfo info ) { }

    void IXunitSerializable.Serialize( IXunitSerializationInfo info ) { }

    IAssemblyInfo ITestAssembly.Assembly => this._factory.AssemblyInfo;

    string ITestAssembly.ConfigFileName => null!;
}
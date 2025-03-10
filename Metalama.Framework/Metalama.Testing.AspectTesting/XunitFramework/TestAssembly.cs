// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618, CS8602

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33167;

internal class TestContract : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        ( (IMethod)meta.Target.Parameter.ContainingDeclaration! ).Invoke( value );
    }
}

// <target>
internal class TestClass
{
    public void Method1( [TestContract] string nonNullableString ) { }
}
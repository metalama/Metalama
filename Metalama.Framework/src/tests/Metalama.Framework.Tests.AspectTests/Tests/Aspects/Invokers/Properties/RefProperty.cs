// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.IntegrationTests.Aspects.Invokers.Properties.RefProperty;

public class TestAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var m = meta.Target.Type.Methods.OfName( "M" ).Single();

        var mappingType = (INamedType)meta.Target.Method.Parameters[0].Type;

        var from = meta.Target.Method.Parameters[0];

        foreach (var fieldOrProperty in mappingType.FieldsAndProperties)
        {
            m.Invoke( fieldOrProperty );
        }

        return meta.Proceed();
    }
}

// <target>
internal class TargetClass
{
    private void M( ref int i ) { }

    public int F;

    public ref int P => ref F;

    [Test]
    public void Map( TargetClass source ) { }
}
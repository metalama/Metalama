// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System.Linq;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.ChildAspects.Issue600_GetMethod;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Proceed();
}

// <target>
internal class TargetClass
{
    private object _targetField;

    private class Fabric : TypeFabric
    {
        public override void AmendType( ITypeAmender amender )
        {
            amender.Select( t => t.Fields.Single() ).Select( f => f.GetMethod! ).AddAspect<Aspect>();
        }
    }
}

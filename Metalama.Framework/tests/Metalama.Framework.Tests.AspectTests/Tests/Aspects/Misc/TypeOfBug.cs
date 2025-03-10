// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.TypeOfBug
{
    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public class NotToStringAttribute : Attribute { }

    public class ToStringAttribute : TypeAspect
    {
        [Introduce]
        public string IntroducedToString()
        {
            var t = meta.CompileTime( typeof(NotToStringAttribute) );
            var n = meta.CompileTime( nameof(NotToStringAttribute) );
            Console.WriteLine( t );

            return n;
        }
    }

    // <target>
    [ToString]
    internal class TargetCode { }
}
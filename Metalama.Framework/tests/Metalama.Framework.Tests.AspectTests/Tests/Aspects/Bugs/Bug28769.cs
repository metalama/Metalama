// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28769
{
    internal class ConvertToRunTimeAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            // The cast to IEnumerable is to avoid referencing the LinqExtensions in the engine assembly.
            var parameterNamesCompileTime = ( (IEnumerable<IParameter>)meta.Target.Parameters ).Select( p => p.Name ).ToArray();
            var parameterNames = meta.RunTime( parameterNamesCompileTime );

            return null;
        }
    }

    internal class TargetCode
    {
        // <target>
        [ConvertToRunTimeAspect]
        private void Method( string a, int c, DateTime e ) { }

        // </target>
    }
}
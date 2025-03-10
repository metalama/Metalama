// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.CodeModel.ConvertToReflection
{
    // This tests serialization to reflection with types that cannot be constructed using a typeof.

    internal class Aspect : TypeAspect
    {
        [Introduce]
        public void Run()
        {
            foreach (var method in meta.Target.Type.Methods)
            {
                foreach (var parameter in method.Parameters)
                {
                    var type = meta.RunTime( parameter.Type.ToType() );
                }
            }
        }
    }

    // <target>
    [Aspect]
    internal unsafe class TargetCode
    {
        private void SystemTypesOnly( dynamic dyn, dynamic[] dynArray, List<dynamic> dynGeneric ) { }
    }
}
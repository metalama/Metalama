// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Iterators.Introduce
{
    internal class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod( nameof(ProgrammaticallyMethodAsync) );
        }

        [Introduce]
        public IEnumerable<int> DeclarativelyMethodAsync()
        {
            yield return 1;
        }

        [Template]
        public IEnumerable<int> ProgrammaticallyMethodAsync()
        {
            yield return 1;
        }
    }

    // <target>
    [Aspect]
    internal class TargetCode { }
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.LiveTemplateError
{
    [EditorExperience( SuggestAsLiveTemplate = true )]
    internal class Aspect : MethodAspect
    {
        public Aspect( int x ) { }

        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            // This should not be called.
            throw new Exception( "Oops" );
        }
    }

    // <target>
    internal class Target
    {
        [Aspect( 0 )]
        private void M() { }
    }
}
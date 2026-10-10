// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

// LAMA0045 must be reported for a concrete aspect without a default constructor when it inherits
// SuggestAsLiveTemplate = true from an abstract base aspect, but not for the abstract base aspect (#2174).

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.LiveTemplateAbstractBaseDerivedError
{
    [EditorExperience( SuggestAsLiveTemplate = true )]
    internal abstract class BaseAspect : MethodAspect { }

    internal class DerivedAspect : BaseAspect
    {
        public DerivedAspect( int x ) { }
    }

    // <target>
    internal class T { }
}

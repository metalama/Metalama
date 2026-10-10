// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

// An abstract base aspect with SuggestAsLiveTemplate = true must not report LAMA0045 (#2174).

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.LiveTemplateAbstractBase
{
    [EditorExperience( SuggestAsLiveTemplate = true )]
    internal abstract class BaseAspect : MethodAspect { }

    internal class DerivedAspect : BaseAspect { }

    // <target>
    internal class T { }
}

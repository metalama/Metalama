// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.ConditionallyInheritable;

public class TheAspect : TypeAspect, IConditionallyInheritableAspect
{
    public bool IsInheritable { get; set; }

    bool IConditionallyInheritableAspect.IsInheritable( IDeclaration targetDeclaration, IAspectInstance aspectInstance ) => IsInheritable;

    [Introduce]
    private void IntroducedMethod() { }
}

[TheAspect( IsInheritable = true )]
internal class BaseClass1 { }

// <target>
internal class DerivedClass1 : BaseClass1 { }

[TheAspect( IsInheritable = false )]
internal class BaseClass2 { }

// <target>
internal class DerivedClass2 : BaseClass2 { }
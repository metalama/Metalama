// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssembly
{
    [Inheritable]
    public class Aspect : TypeAspect
    {
        [Introduce]
        public void Introduced() { }

        public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        {
            base.BuildEligibility( builder );
            builder.ExceptForInheritance().MustNotBeInterface();
        }
    }

    [Aspect]
    public interface I { }

    public interface J : I { }
}
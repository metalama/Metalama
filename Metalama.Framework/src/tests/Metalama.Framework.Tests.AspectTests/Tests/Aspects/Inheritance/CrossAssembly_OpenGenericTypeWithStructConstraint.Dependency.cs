// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssembly_OpenGenericTypeWithStructConstraint
{
    /// <summary>
    /// An inheritable aspect that introduces a method into every non-abstract derived type.
    /// </summary>
    [Inheritable]
    public class Aspect : TypeAspect
    {
        [Introduce]
        public void Introduced() { }

        public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        {
            base.BuildEligibility( builder );
            builder.ExceptForInheritance().MustNotBeAbstract();
        }
    }

    /// <summary>
    /// An open generic type with a <c>struct</c> constraint, carrying an inheritable aspect.
    /// </summary>
    /// <remarks>
    /// The constraint is significant: the serialized identifier of the aspect target is resolved in the
    /// referencing compilation, and the type parameters of the target must not be mistaken for named types.
    /// </remarks>
    [Aspect]
    public abstract class OpenBase<T1, T2>
        where T1 : struct { }
}

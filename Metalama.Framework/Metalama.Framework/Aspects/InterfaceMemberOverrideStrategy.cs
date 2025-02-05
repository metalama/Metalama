// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Member conflict behavior of interface introduction advice.
    /// </summary>
    [CompileTime]
    public enum InterfaceMemberOverrideStrategy
    {
        /// <summary>
        /// The behavior depends on the <see cref="OverrideStrategy"/> specified when calling the <see cref="IAdviceFactory.ImplementInterface(INamedType,INamedType,OverrideStrategy,object?)"/>
        /// method. When set to <see cref="OverrideStrategy.Fail"/>, the default value is <see cref="Fail"/>. When set to <see cref="OverrideStrategy.Override"/>,
        /// the strategy is to override.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The advice fails with a compilation error if a matching interface member already exists in the target declaration.
        /// </summary>
        Fail,

        /// <summary>
        /// The advice introduces the interface member as explicit even if the interface member was supposed to be introduced as implicit.
        /// </summary>
        MakeExplicit,

        /// <summary>
        /// When the <see cref="OverrideStrategy"/> of the <see cref="IAdviceFactory.ImplementInterface(INamedType,INamedType,OverrideStrategy,object?)"/>
        /// is set to <see cref="OverrideStrategy.Override"/>, does not override this member if there is already an implementation. 
        /// </summary>
        Ignore = 3

        // TODO: Support.
        //       The problem is that these are not really useful when the other declaration is not compatible.
        //       If the existing declaration has a different return type, it's not usable, leading to an error. User can solve this only programatically.
        //       The name of this enum however implies that we can override.

        // /// <summary>
        // /// The advice uses the existing type member if it exactly matches the interface member and ignores the provided template, otherwise the advice fails with a compilation error.
        // /// </summary>
        // UseExisting,

        // /// <summary>
        // /// The advice overrides the target declaration using the template specified for the interface member. The advice fails with a compilation error if it is not possible.
        // /// </summary>
        // Override,
    }
}
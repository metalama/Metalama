// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Runtime.Serialization;

namespace Metalama.Framework.Eligibility
{
    /// <summary>
    /// An interface that allows aspect to specify to which declarations they are allowed to be applied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso href="@eligibility"/>
    [CompileTime]
    public interface IEligible<in T>
        where T : class, IDeclaration
    {
        /// <summary>
        /// Configures the eligibility of the aspect or attribute.
        /// Implementations are not allowed to reference non-static members.
        /// Implementations must call the implementation of the base class if it exists.
        /// </summary>
        /// <param name="builder">An object that allows the aspect to configure characteristics like
        /// description, dependencies, or layers.</param>
        /// <remarks>
        /// Do not reference instance class members in your implementation of  <see cref="BuildEligibility"/>.
        /// Indeed, this method is called on an instance obtained using <see cref="FormatterServices.GetUninitializedObject"/>, that is,
        /// <i>without invoking the class constructor</i>.
        /// </remarks>
        /// <seealso href="@eligibility"/>
        [CompileTime]
        void BuildEligibility( IEligibilityBuilder<T> builder )
#if NET5_0_OR_GREATER
        { }
#else
            ;
#endif
    }
}
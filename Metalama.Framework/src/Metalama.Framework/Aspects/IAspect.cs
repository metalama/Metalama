// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// The base interface for all aspects. A class should not implement
    /// this interface, but the strongly-typed variant <see cref="IAspect{T}"/>.
    /// </summary>
    [RunTimeOrCompileTime]
    public interface IAspect : ICompileTimeSerializable, ITemplateProvider;

    /// <summary>
    /// The base interface for all aspects, with the type parameter indicating to which types
    /// of declarations the aspect can be added.
    /// </summary>
    [ForcedGenericRunTimeOrCompileTime]
    public interface IAspect<in T> : IAspect, IEligible<T>
        where T : class, IDeclaration
    {
        /// <summary>
        /// Initializes the aspect. The implementation must add advice, child aspects and validators
        /// using the <paramref name="builder"/> parameter.
        /// </summary>
        /// <param name="builder">An object that allows the aspect to add advice, child aspects and validators.</param>
        [CompileTime]
        void BuildAspect( IAspectBuilder<T> builder )
#if NET5_0_OR_GREATER
        { }
#else
            ;
#endif
    }
}
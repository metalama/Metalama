// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using PostSharp.Aspects.Advices;
using System.Reflection;

namespace PostSharp.Aspects
{
    /// <summary>
    /// Equivalent to <see cref="IAspect{T}"/> where <c>T</c> is <see cref="IField"/>.
    /// </summary>
    public interface IFieldLevelAspect : IAspect
    {
        /// <summary>
        /// In Metalama, add an initializer from the <see cref="Metalama.Framework.Aspects.IAspect{T}.BuildAspect"/> method by calling
        /// <c>builder</c>.<see cref="Advice"/>.<see cref="IAdviceFactory.AddInitializer(Metalama.Framework.Code.INamedType,string,InitializerKind,object?,object?)"/>.
        /// </summary>
        /// <seealso href="@initializers"/>
        void RuntimeInitialize( FieldInfo field );
    }
}
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, implement the <see cref="IAspect{T}.BuildAspect"/> method and call
    ///  <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.<see cref="IAdviceFactory.ImplementInterface(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.INamedType,Metalama.Framework.Aspects.OverrideStrategy,object?)"/>.
    /// </summary>
    /// <seealso href="@implementing-interfaces"/>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
    [PublicAPI]
    public sealed class IntroduceInterfaceAttribute : Advice
    {
        public IntroduceInterfaceAttribute( Type interfaceType )
        {
            throw new NotImplementedException();
        }

        public InterfaceOverrideAction OverrideAction { get; set; }

        public InterfaceOverrideAction AncestorOverrideAction { get; set; }
    }
}
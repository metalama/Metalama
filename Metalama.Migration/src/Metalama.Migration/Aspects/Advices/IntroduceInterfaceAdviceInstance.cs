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
    /// In Metalama, use <c>builder</c>.<see cref="IAspectBuilder.Advice"/>.<see cref="IAdviceFactory.ImplementInterface(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.INamedType,Metalama.Framework.Aspects.OverrideStrategy,object?)"/>.
    /// </summary>
    /// <seealso href="@implementing-interfaces"/>
    [PublicAPI]
    public sealed class IntroduceInterfaceAdviceInstance : AdviceInstance
    {
        public IntroduceInterfaceAdviceInstance(
            Type interfaceType,
            InterfaceOverrideAction overrideAction = InterfaceOverrideAction.Fail,
            InterfaceOverrideAction ancestorOverrideAction = InterfaceOverrideAction.Fail )
        {
            throw new NotImplementedException();
        }

        public Type InterfaceType { get; }

        public InterfaceOverrideAction OverrideAction { get; }

        public InterfaceOverrideAction AncestorOverrideAction { get; }

        public override object MasterAspectMember { get; }
    }
}
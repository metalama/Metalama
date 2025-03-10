// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;

internal sealed partial class ImplementInterfaceAdvice
{
    private sealed class InterfaceSpecification
    {
        public IRef<INamedType> InterfaceType { get; }

        /// <summary>
        /// Gets specifications of interface members using the <see cref="Framework.Aspects.InterfaceMemberAttribute"/>.
        /// </summary>
        public IReadOnlyList<MemberSpecification> MemberSpecifications { get; }

        public InterfaceSpecification(
            IRef<INamedType> interfaceType,
            IReadOnlyList<MemberSpecification> memberSpecification )
        {
            this.InterfaceType = interfaceType;
            this.MemberSpecifications = memberSpecification;
        }

        public override string ToString() => $"{this.InterfaceType}";
    }
}
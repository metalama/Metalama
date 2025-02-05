// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
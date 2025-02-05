// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;

namespace Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;

internal sealed partial class ImplementInterfaceAdvice
{
    private readonly struct MemberSpecification
    {
        public IRef<IMember> InterfaceMember { get; }

        public IRef<IMember>? TargetMember { get; }

        public TemplateMember<IMember>? Template { get; }

        public bool IsExplicit => ((IInterfaceMemberAttribute?) this.Template?.TemplateClassMember.Attribute).AssertNotNull().IsExplicit;

#pragma warning disable CS0618 // Type or member is obsolete
        public InterfaceMemberOverrideStrategy OverrideStrategy
            => (this.Template?.TemplateClassMember.Attribute as InterfaceMemberAttribute)?.WhenExists
               ?? InterfaceMemberOverrideStrategy.MakeExplicit;
#pragma warning restore CS0618

        public MemberSpecification(
            IRef<IMember> interfaceMember,
            IRef<IMember>? targetMember,
            TemplateMember<IMember>? template )
        {
            Invariant.AssertNot( targetMember == null && template == null );

            this.InterfaceMember = interfaceMember;
            this.TargetMember = targetMember;
            this.Template = template;
        }
    }
}
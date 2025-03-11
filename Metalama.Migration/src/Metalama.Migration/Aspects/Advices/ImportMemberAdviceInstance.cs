// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Reflection;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, members of the target declaration do not need to be imported into the aspect. Instead, the
    /// aspect accesses the target code using dynamic code or invokers.
    /// </summary>
    /// <seealso href="template-dynamic-code"/>
    [PublicAPI]
    public abstract class ImportMemberAdviceInstance : AdviceInstance
    {
        public FieldInfo AspectField { get; }

        public bool IsRequired { get; }

        public abstract object Member { get; }

        public abstract string[] MemberNames { get; }

        public ImportMemberOrder Order { get; }

        public override object MasterAspectMember { get; }
    }
}
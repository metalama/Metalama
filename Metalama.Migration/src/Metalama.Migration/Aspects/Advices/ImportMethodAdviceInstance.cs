// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System;
using System.Reflection;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, members of the target declaration do not need to be imported into the aspect. Instead, the
    /// aspect accesses the target code using dynamic code or invokers. To generate code that calls a method, use <see cref="IMethod"/>.<see cref="IMethodInvoker.Invoke(object[])"/>.
    /// </summary>
    /// <seealso href="template-dynamic-code"/>
    [PublicAPI]
    public sealed class ImportMethodAdviceInstance : ImportMemberAdviceInstance
    {
        public ImportMethodAdviceInstance(
            FieldInfo aspectField,
            string methodName,
            bool isRequired = false,
            ImportMemberOrder order = ImportMemberOrder.Default )
        {
            throw new NotImplementedException();
        }

        public ImportMethodAdviceInstance(
            FieldInfo aspectField,
            string[] methodNames,
            bool isRequired = false,
            ImportMemberOrder order = ImportMemberOrder.Default )
        {
            throw new NotImplementedException();
        }

        public override object Member { get; }

        public override string[] MemberNames { get; }
    }
}
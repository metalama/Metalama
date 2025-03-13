// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System;

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, members of the target declaration do not need to be imported into the aspect. Instead, the
    /// aspect accesses the target code using dynamic code or invokers. To generate code that calls a method, use <see cref="IMethod"/>.<see cref="IMethod.Invoke(object[])"/>.
    /// To generate code that accesses a field or property, use <see cref="IFieldOrProperty"/>.<see cref="IFieldOrProperty.Value"/>.
    /// </summary>
    /// <seealso href="template-dynamic-code"/>
    [AttributeUsage( AttributeTargets.Field )]
    [PublicAPI]
    public sealed class ImportMemberAttribute : Advice
    {
        public ImportMemberAttribute( params string[] memberNames )
        {
            throw new NotImplementedException();
        }

        public ImportMemberAttribute( string memberName )
        {
            throw new NotImplementedException();
        }

        public bool IsRequired { get; set; }

        public string[] MemberNames { get; }

        public ImportMemberOrder Order { get; set; }
    }
}
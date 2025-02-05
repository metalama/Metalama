// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using JetBrains.Annotations;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using System;

namespace PostSharp.Constraints
{
    /// <summary>
    /// In Metalama, use <c>Metalama.Extensions.Architecture.Aspects.CanOnlyBeUsedFromAttribute</c>.
    /// </summary>
    [PublicAPI]
    public sealed class ComponentInternalAttribute : ReferenceConstraint
    {
        /// <summary>
        /// In Metalama, use <c>Metalama.Extensions.Architecture.Aspects.CanOnlyBeUsedFromAttribute</c> and
        /// set the <see cref="BaseUsageValidationAttribute.CurrentNamespace"/> property to <c>true</c>.
        /// </summary>
        public ComponentInternalAttribute()
        {
            throw new NotImplementedException();
        }

        public SeverityType Severity { get; set; }

        /// <summary>
        /// In Metalama, use <c>Metalama.Extensions.Architecture.Aspects.CanOnlyBeUsedFromAttribute</c> and
        /// set the <see cref="BaseUsageValidationAttribute.Types"/> property.
        /// </summary>
        public ComponentInternalAttribute( params Type[] friendTypes )
            : this()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <c>Metalama.Extensions.Architecture.Aspects.CanOnlyBeUsedFromAttribute</c> and
        /// set the <see cref="BaseUsageValidationAttribute.Namespaces"/> property.
        /// </summary>
        public ComponentInternalAttribute( params string[] friendNamespaces )
            : this()
        {
            throw new NotImplementedException();
        }

        protected override void ValidateReference( ICodeReference codeReference )
        {
            throw new NotImplementedException();
        }
    }
}
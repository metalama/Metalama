// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Extensibility;
using PostSharp.Reflection;
using System;
using System.Reflection;

namespace PostSharp.Constraints
{
    /// <summary>
    /// In Metalama, use an aspect or a fabric, and register a reference validator using the <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
    /// </summary>
    /// <seealso href="@aspect-validating"/>
    /// <seealso href="@validating-usage"/>
    [AttributeUsage(
        AttributeTargets.All & ~(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter),
        AllowMultiple = true )]
    [MulticastAttributeUsage(
        MulticastTargets.AnyType | MulticastTargets.Method | MulticastTargets.InstanceConstructor | MulticastTargets.Field,
        TargetTypeAttributes = MulticastAttributes.Public | MulticastAttributes.Internal | MulticastAttributes.InternalOrProtected
                               | MulticastAttributes.UserGenerated,
        TargetMemberAttributes = MulticastAttributes.AnyVisibility & ~MulticastAttributes.Private )]
    public abstract class ReferenceConstraint : ReferentialConstraint
    {
        protected abstract void ValidateReference( ICodeReference reference );

        public sealed override void ValidateCode( object target, Assembly assembly )
        {
            throw new NotImplementedException();
        }
    }
}
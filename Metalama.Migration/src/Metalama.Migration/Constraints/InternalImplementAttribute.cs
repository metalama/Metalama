// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Extensibility;
using System;
using System.Reflection;

namespace PostSharp.Constraints
{
    /// <summary>
    /// In Metalama, use <see cref="Metalama.Extensions.Architecture.Aspects.InternalOnlyImplementAttribute"/>.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.All & ~(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter),
        AllowMultiple = true )]
    [MulticastAttributeUsage( MulticastTargets.Interface, TargetTypeAttributes = MulticastAttributes.Public )]
    public sealed class InternalImplementAttribute : ReferentialConstraint
    {
        public InternalImplementAttribute()
        {
            throw new NotImplementedException();
        }

        public SeverityType Severity { get; set; }

        public override void ValidateCode( object target, Assembly assembly )
        {
            throw new NotImplementedException();
        }
    }
}
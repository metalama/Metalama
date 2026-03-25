// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssembly_TypeOf
{
    // A run-time attribute type. typeof() on this type in compile-time code
    // will be rewritten to TypeOfResolver.Resolve().
    public class MarkerAttribute : Attribute { }

    // This [Inheritable] aspect has a static field initialized with typeof(MarkerAttribute).
    // Since MarkerAttribute is a run-time type, the typeof() gets rewritten to
    // TypeOfResolver.Resolve() in compile-time code. When this aspect is deserialized
    // transitively in a consuming project, the static initializer runs and requires
    // UserCodeExecutionContext to be set up.
    [Inheritable]
    public class TypeOfAspect : TypeAspect
    {
        private static readonly Type _markerAttributeType = typeof(MarkerAttribute);

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            base.BuildAspect( builder );

            // Use the static field in BuildAspect to ensure it's not optimized away.
            if ( _markerAttributeType != null )
            {
                builder.IntroduceMethod( nameof(GetMarkerTypeName), whenExists: OverrideStrategy.Ignore );
            }
        }

        [Template]
        public string GetMarkerTypeName() => _markerAttributeType.Name;
    }

    [TypeOfAspect]
    public class BaseClass { }
}

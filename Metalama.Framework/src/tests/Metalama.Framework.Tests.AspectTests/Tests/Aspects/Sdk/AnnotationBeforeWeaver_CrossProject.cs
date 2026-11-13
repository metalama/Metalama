// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// The exported annotation is added in the dependency, whose last pipeline stage is a low-level weaver.
// The annotation must be written to the transitive manifest of the dependency and be read here.

using System.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Sdk.AnnotationBeforeWeaver_CrossProject
{
    public class ReadAnnotationAspect : TypeAspect
    {
        [Introduce]
        public string? TheAnnotation = meta.Target.Type.BaseType!.Enhancements().GetAnnotations<MyAnnotation>().SingleOrDefault()?.Value;
    }

    // <target>
    [ReadAnnotationAspect]
    public class DerivedClass : BaseClass { }
}

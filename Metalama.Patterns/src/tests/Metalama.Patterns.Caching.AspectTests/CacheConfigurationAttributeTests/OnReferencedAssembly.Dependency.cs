// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.AspectTests.CacheConfigurationAttributeTests.OnReferencedAssembly;

[assembly: CachingConfiguration( ProfileName = TestProfiles.A, UseDependencyInjection = false )]

namespace Metalama.Patterns.Caching.AspectTests.CacheConfigurationAttributeTests.OnReferencedAssembly;

[RunTimeOrCompileTime]
public static class TestProfiles
{
    public const string Default = "default";
    public const string A = "[A]";
    public const string B = "[B]";
}

public class ReferencedParentCachingClass
{
    public sealed class ReferencedInnerCachingClassInBase
    {
        [Cache]
        public object GetValueReferencedInnerBase()
        {
            return null!;
        }
    }

    [Cache]
    public object GetValueReferencedBase()
    {
        return null!;
    }
}

public class ReferencedChildCachingClass : ReferencedParentCachingClass
{
    public sealed class ReferencedInnerCachingClassInChild
    {
        [Cache]
        public object GetValueReferencedInnerChild()
        {
            return null!;
        }
    }

    [Cache]
    public object GetValueReferencedChild()
    {
        return null!;
    }
}

[CachingConfiguration( ProfileName = TestProfiles.B )]
public class ReferencedParentCachingClassOverridden
{
    public sealed class ReferencedInnerCachingClassInBaseOverridden
    {
        [Cache]
        public object GetValueReferencedInnerBase()
        {
            return null!;
        }
    }

    [Cache]
    public object GetValueReferencedBase()
    {
        return null!;
    }
}

public class ReferencedChildCachingClassOverridden : ReferencedParentCachingClassOverridden
{
    public sealed class ReferencedInnerCachingClassInChildOverridden
    {
        [Cache]
        public object GetValueReferencedInnerChild()
        {
            return null!;
        }
    }

    [Cache]
    public object GetValueReferencedChild()
    {
        return null!;
    }
}
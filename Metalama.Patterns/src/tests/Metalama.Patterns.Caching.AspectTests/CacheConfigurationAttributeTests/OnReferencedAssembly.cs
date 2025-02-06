// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RemoveOutputCode
// @MainMethod(TestMain)
// @IgnoredDiagnostic(CS8603)
// @IgnoredDiagnostic(CS8600)
// @IgnoredDiagnostic(CS8602)
#endif

using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.TestHelpers;

// ReSharper disable once CheckNamespace
namespace Metalama.Patterns.Caching.AspectTests.CacheConfigurationAttributeTests.OnReferencedAssembly;

#pragma warning disable CA2201

public static class CacheConfigurationAttributeTest
{
    public static void CheckAfterCachedMethodCall(
        string calledMethod,
        TestingCacheBackend backend,
        ref string? previousCacheKey,
        string expectedProfileName )
    {
        if ( backend.LastCachedKey == previousCacheKey )
        {
            throw new Exception( $"{calledMethod}: backend.LastCachedKey '{backend.LastCachedKey}' == previousCacheKey '{previousCacheKey}'" );
        }

        if ( backend.LastCachedItem?.Configuration?.ProfileName != expectedProfileName )
        {
            throw new Exception(
                $"{calledMethod}: backend.LastCachedItem.Configuration.ProfileName '{backend.LastCachedItem?.Configuration?.ProfileName}' != expectedProfileName '{expectedProfileName}'" );
        }

        previousCacheKey = backend.LastCachedKey;
    }
}

[CachingConfiguration( UseDependencyInjection = false )]
public sealed class LocalChildCachingClass : ReferencedChildCachingClass
{
    public sealed class LocalChildCachingClass_Inner
    {
        [Cache]
        public object LocalChildCachingClass_Inner_Method() => null!;
    }

    [Cache]
    public object LocalChildCachingClass_Method() => null!;
}

[CachingConfiguration( UseDependencyInjection = false )]
public sealed class LocalChildCachingClassOverridden : ReferencedChildCachingClassOverridden
{
    public sealed class LocalChildCachingClassOverridden_Inner
    {
        [Cache]
        public object LocalChildCachingClassOverridden_Inner_Method() => null!;
    }

    [Cache]
    public object LocalChildCachingClassOverridden_Method() => null!;
}

public class Program
{
    public static void TestMain()
    {
        Console.WriteLine( "Test started." );

        var backend = new TestingCacheBackend( "test" );

        CachingService.Default =
            CachingService.Create(
                b => b.WithBackend( backend )
                    .AddProfile( new CachingProfile( TestProfiles.A ) )
                    .AddProfile( new CachingProfile( TestProfiles.B ) ) );

        var cachingClass = new LocalChildCachingClass();

        var cachingClassOverridden = new LocalChildCachingClassOverridden();

        var referencedInnerCachingClassInBase
            = new ReferencedParentCachingClass.ReferencedInnerCachingClassInBase();

        var referencedInnerCachingClassInChild
            = new ReferencedChildCachingClass.ReferencedInnerCachingClassInChild();

        var localInnerCachingClassInChild
            = new LocalChildCachingClass.LocalChildCachingClass_Inner();

        var referencedInnerCachingClassInBaseOverridden
            = new ReferencedParentCachingClassOverridden.ReferencedInnerCachingClassInBaseOverridden();

        var referencedInnerCachingClassInChildOverridden
            = new ReferencedChildCachingClassOverridden.ReferencedInnerCachingClassInChildOverridden();

        var localInnerCachingClassInChildOverridden
            = new LocalChildCachingClassOverridden.LocalChildCachingClassOverridden_Inner();

        string? previousCachedKey = null;

        if ( backend.LastCachedKey != null )
        {
            throw new Exception( "backend.LastCachedKey != null" );
        }

        if ( backend.LastCachedItem != null )
        {
            throw new Exception( "backend.LastCachedItem != null" );
        }

        cachingClass.GetValueReferencedBase();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "referenced base",
            backend,
            ref previousCachedKey,
            TestProfiles.A );

        cachingClass.GetValueReferencedChild();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "referenced child",
            backend,
            ref previousCachedKey,
            TestProfiles.A );

        cachingClass.LocalChildCachingClass_Method();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "local child",
            backend,
            ref previousCachedKey,
            TestProfiles.A );

        cachingClassOverridden.GetValueReferencedBase();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "overridden referenced base",
            backend,
            ref previousCachedKey,
            TestProfiles.B );

        cachingClassOverridden.GetValueReferencedChild();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "overridden referenced child",
            backend,
            ref previousCachedKey,
            TestProfiles.B );

        cachingClassOverridden.LocalChildCachingClassOverridden_Method();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "overridden local child",
            backend,
            ref previousCachedKey,
            TestProfiles.B );

        referencedInnerCachingClassInBase.GetValueReferencedInnerBase();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "referenced inner base",
            backend,
            ref previousCachedKey,
            TestProfiles.A );

        referencedInnerCachingClassInChild.GetValueReferencedInnerChild();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "referenced inner child",
            backend,
            ref previousCachedKey,
            TestProfiles.A );

        localInnerCachingClassInChild.LocalChildCachingClass_Inner_Method();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "local inner child",
            backend,
            ref previousCachedKey,
            TestProfiles.A );

        referencedInnerCachingClassInBaseOverridden.GetValueReferencedInnerBase();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "overridden referenced inner base",
            backend,
            ref previousCachedKey,
            TestProfiles.B );

        referencedInnerCachingClassInChildOverridden.GetValueReferencedInnerChild();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "overridden referenced inner child",
            backend,
            ref previousCachedKey,
            TestProfiles.B );

        localInnerCachingClassInChildOverridden.LocalChildCachingClassOverridden_Inner_Method();

        CacheConfigurationAttributeTest.CheckAfterCachedMethodCall(
            "overridden local inner child",
            backend,
            ref previousCachedKey,
            TestProfiles.B );

        Console.WriteLine( "Test completed." );
    }
}
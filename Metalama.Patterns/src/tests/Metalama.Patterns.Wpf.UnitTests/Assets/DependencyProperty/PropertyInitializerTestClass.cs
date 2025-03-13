// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Windows;

namespace Metalama.Patterns.Wpf.UnitTests.Assets.DependencyPropertyNS;

#pragma warning disable LAMA5206

public sealed partial class PropertyInitializerTestClass : DependencyObject
{
    public static int DefaultConfigurationInitializerCallCount { get; private set; }

    private static int DefaultConfigurationInitializer()
    {
        ++DefaultConfigurationInitializerCallCount;

        return 42;
    }

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    [DependencyProperty]
    public int DefaultConfiguration { get; set; } = DefaultConfigurationInitializer();

    // ReSharper disable once MemberCanBePrivate.Global
    public static int NotDefaultNotInitialInitializerCallCount { get; private set; }

    private static int NotDefaultNotInitialInitializer()
    {
        ++NotDefaultNotInitialInitializerCallCount;

        return 42;
    }

#pragma warning restore LAMA5200 // Initializer will not be used.

    // ReSharper disable once MemberCanBePrivate.Global
    public static int InitialOnlyInitializerCallCount { get; private set; }

    private static int InitialOnlyInitializer()
    {
        ++InitialOnlyInitializerCallCount;

        return 42;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static int DefaultAndInitialInitializerCallCount { get; private set; }

    private static int DefaultAndInitialInitializer()
    {
        ++DefaultAndInitialInitializerCallCount;

        return 42;
    }
}
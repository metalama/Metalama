// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// https://github.com/metalama/Metalama/issues/602

namespace Metalama.Extensions.DependencyInjection.AspectTests.Bugs.Issue602;

public interface IServiceA { }

public interface IServiceB { }

public interface IServiceC { }

public interface IServiceD { }

public interface IServiceE { }

// <target>
public class TargetClass
{
    [Dependency]
    private readonly IServiceA _serviceA;

    [Dependency]
    private readonly IServiceB _serviceB;

    [Dependency]
    private readonly IServiceC _serviceC;

    [Dependency]
    private readonly IServiceD _serviceD;

    [Dependency]
    private readonly IServiceE _serviceE;
}
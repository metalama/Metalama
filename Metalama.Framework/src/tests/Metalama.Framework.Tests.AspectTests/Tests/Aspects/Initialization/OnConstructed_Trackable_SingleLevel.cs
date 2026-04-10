// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Trackable_SingleLevel;

public enum ObjectStatus
{
    Constructing,
    Constructed,
    Initialized
}

public static class ObjectTracker
{
    [ThreadStatic]
    private static ObjectStatus _status;

    public static void Register( object instance, ObjectStatus status )
    {
        _status = status;
        Console.WriteLine( $"Status = {status}" );
    }

    public static ObjectStatus GetStatus( object instance ) => _status;
}

public class TrackableAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(OnConstructing), InitializerKind.BeforeInstanceConstructor );
        builder.AddInitializer( nameof(OnConstructedTemplate), InitializerKind.AfterLastInstanceConstructor );
        builder.AddInitializer( nameof(OnFullyInitialized), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void OnConstructing()
    {
        ObjectTracker.Register( meta.This, ObjectStatus.Constructing );
    }

    [Template]
    private void OnConstructedTemplate()
    {
        ObjectTracker.Register( meta.This, ObjectStatus.Constructed );
    }

    [Template]
    private void OnFullyInitialized()
    {
        ObjectTracker.Register( meta.This, ObjectStatus.Initialized );
    }
}

// <target>
[TrackableAspect]
public class TargetCode
{
    public string Color { get; init; } = "Red";

    public TargetCode()
    {
    }
}

// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32971;

public class TrackChangesAttribute : TypeAspect
{
    [Introduce]
    public bool? HasChanges { get; protected set; }

    [Introduce]
    public bool IsTrackingChanges
    {
        get => HasChanges.HasValue;
        set
        {
            if (IsTrackingChanges != value)
            {
                HasChanges = value ? false : null;
            }
        }
    }
}

// <target>
[TrackChanges]
public partial class Comment
{
    public Guid Id { get; }

    public string Author { get; set; }

    public string Content { get; set; }

    public Comment( Guid id, string author, string content )
    {
        Id = id;
        Author = author;
        Content = content;
    }
}
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
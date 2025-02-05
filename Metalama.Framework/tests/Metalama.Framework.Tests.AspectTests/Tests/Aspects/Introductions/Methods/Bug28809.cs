using System.ComponentModel;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.Bug28809
{
    internal class IntroducePropertyChangedAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var newEvent = builder.IntroduceEvent( nameof(PropertyChanged) )
                .Declaration;

            builder.IntroduceMethod(
                nameof(OnPropertyChanged),
                tags: new { @event = newEvent } );
        }

        [Template]
        public event PropertyChangedEventHandler? PropertyChanged;

        [Template]
        protected virtual void OnPropertyChanged( string propertyName )
        {
            var @event = (IEvent)meta.Tags["event"]!;
            @event.Raise( meta.This, new PropertyChangedEventArgs( propertyName ) );
        }
    }

    // <target>
    [IntroducePropertyChangedAspect]
    internal class TargetCode { }
}
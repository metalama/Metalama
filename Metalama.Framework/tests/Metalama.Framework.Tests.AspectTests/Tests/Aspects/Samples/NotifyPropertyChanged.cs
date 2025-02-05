#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
#endif

// In .NET Framework, INotifyPropertyChanged.PropertyChanged is not marked as nullable, so the output is slightly different.

using System;
using System.ComponentModel;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Samples.NotifyPropertyChanged
{
    [AttributeUsage( AttributeTargets.Class )]
    public class NotifyPropertyChangedAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.ImplementInterface( typeof(INotifyPropertyChanged) );

            foreach (var property in builder.Target.Properties
                         .Where( p => p.Accessibility == Accessibility.Public && p.Writeability == Writeability.All ))
            {
                builder.With( property ).OverrideAccessors( null, nameof(SetPropertyTemplate) );
            }
        }

        [InterfaceMember]
        public event PropertyChangedEventHandler? PropertyChanged;

        [Introduce]
        protected virtual void OnPropertyChanged( string name )
        {
            meta.This.PropertyChanged?.Invoke( meta.This, new PropertyChangedEventArgs( meta.Target.Parameters[0].Value ) );
        }

        [Template]
        public dynamic? SetPropertyTemplate()
        {
            var value = meta.Target.Parameters[0].Value;

            if (value != meta.Target.Property.Value)
            {
                meta.This.OnPropertyChanged( meta.Target.Property.Name );

                // TODO: Fix after Proceed refactoring (28573).
                meta.Proceed();
            }

            return value;
        }
    }

    // <target>
    [NotifyPropertyChanged]
    internal class Car
    {
        public string? Make { get; set; }

        public double Power { get; set; }
    }
}
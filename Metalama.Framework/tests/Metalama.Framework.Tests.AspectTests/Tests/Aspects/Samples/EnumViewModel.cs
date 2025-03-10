// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Samples.EnumViewModel
{
    public class EnumViewModelAttribute : TypeAspect
    {
        private static DiagnosticDefinition<INamedType> _missingFieldError = new(
            "ENUM01",
            Severity.Error,
            "The [EnumViewModel] aspect requires the type '{0}' to have a field named '_value'." );

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var valueField = builder.Target.Fields.OfName( "_value" ).FirstOrDefault();

            if (valueField == null)
            {
                builder.Diagnostics.Report( _missingFieldError.WithArguments( builder.Target ) );

                return;
            }

            var enumType = (INamedType)valueField.Type;

            foreach (var member in enumType.Fields)
            {
                var propertyBuilder = builder.IntroduceProperty(
                    nameof(IsMemberTemplate),
                    buildProperty: p => p.Name = "Is" + member.Name,
                    tags: new { member = member } );
            }
        }

        [Template]
        public bool IsMemberTemplate => meta.This._value == ( (IField)meta.Tags["member"]! ).Value;
    }

    internal enum Visibility
    {
        Visible,
        Hidden,
        Collapsed
    }

// <target>
    [EnumViewModel]
    internal class VisibilityViewModel
    {
        private Visibility _value;

        public VisibilityViewModel( Visibility value )
        {
            _value = value;
        }
    }
}
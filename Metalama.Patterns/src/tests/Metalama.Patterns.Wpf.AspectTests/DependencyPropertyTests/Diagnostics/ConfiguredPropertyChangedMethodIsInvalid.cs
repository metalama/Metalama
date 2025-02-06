// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using System.Windows;

namespace Metalama.Patterns.Wpf.AspectTests.DependencyPropertyTests.Diagnostics.ConfiguredPropertyChangedMethodIsInvalid;

public class ConfiguredPropertyChangedMethodIsInvalid : DependencyObject
{
    [DependencyProperty( PropertyChangedMethod = nameof(Changed) )]
    public int Foo { get; set; }

    private void Changed( DBNull blah ) { }
}
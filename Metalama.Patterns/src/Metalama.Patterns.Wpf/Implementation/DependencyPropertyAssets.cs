// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Windows;

namespace Metalama.Patterns.Wpf.Implementation;

[CompileTime]
internal sealed class DependencyPropertyAssets
{
    public DependencyPropertyAssets()
    {
        this.DependencyObject = (INamedType) TypeFactory.GetType( typeof(DependencyObject) );
        this.DependencyProperty = (INamedType) TypeFactory.GetType( typeof(DependencyProperty) );
    }

    public INamedType DependencyObject { get; }

    public INamedType DependencyProperty { get; }
}
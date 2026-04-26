using System.Windows;
using Metalama.Framework.Aspects;

namespace Issue1585;

// Introduces an event handler method whose signature matches WPF's RoutedEventHandler.
// XAML's Click="OnButtonClicked" binding is resolved at MarkupCompilePass1 against the
// code-behind class, so this aspect-introduced method must exist in the temp assembly
// for the XAML compile to succeed.
public class WithButtonHandlerAttribute : TypeAspect
{
    [Introduce]
    public void OnButtonClicked( object sender, RoutedEventArgs e ) { }
}

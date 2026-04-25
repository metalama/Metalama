using System.Windows;

namespace Issue1585;

public partial class SelectSettingsWindowControl : Window
{
    private readonly SwapTransformPackage _package = new();

    public SelectSettingsWindowControl()
    {
        this.InitializeComponent();
        this.DataContext = this._package;
    }

    // The three call sites that fail when Metalama doesn't run during the temp compile.
    // These reference aspect-introduced members (IsChanged / AcceptChanges / RejectChanges).
    private void OnApplyClicked( object sender, RoutedEventArgs e )
    {
        if ( this._package.IsChanged )
        {
            this._package.AcceptChanges();
        }
    }

    private void OnCancelClicked( object sender, RoutedEventArgs e )
    {
        this._package.RejectChanges();
    }
}

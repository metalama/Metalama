using System.Windows;

namespace CompiledBindingsWpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string Message => GetMessage();

    [Log]
    private string GetMessage()
    {
        return "Hello, World!";
    }
}

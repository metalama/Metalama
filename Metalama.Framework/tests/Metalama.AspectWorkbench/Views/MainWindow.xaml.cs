// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.AspectWorkbench.ViewModels;
using Metalama.Framework.Engine;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

#pragma warning disable VSTHRD100

namespace Metalama.AspectWorkbench.Views
{
    internal sealed partial class MainWindow
    {
        private const string _testsProjectPath = @"c:\src\Metalama\Tests\Metalama.Templating.UnitTests\";
        private const string _fileDialogueExt = ".cs";
        private const string _fileDialogueFilter = "C# Files (*.cs)|*.cs";

        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            this.InitializeComponent();

            var newViewModel = new MainViewModel();
            this._viewModel = newViewModel;
            this.DataContext = newViewModel;
            this.detailPaneComboBox.ItemsSource = Enum.GetValues( typeof(DetailPaneContent) ).Cast<DetailPaneContent>();
            newViewModel.PropertyChanged += this.ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged( object? sender, PropertyChangedEventArgs e )
        {
            // TODO RichTextBox doesn't support data binding out of the box. RoslynPad doesn't support binding to text either.
            switch ( e.PropertyName )
            {
                case nameof(MainViewModel.SourceCode):
                    this.sourceTextBox.Text = this._viewModel.SourceCode.AssertNotNull();

                    break;

                case nameof(MainViewModel.ColoredSourceCodeDocument):
                    this.highlightedSourceRichBox.Document = this._viewModel.ColoredSourceCodeDocument ?? new FlowDocument();

                    break;

                case nameof(MainViewModel.CompiledTemplateDocument):
                    this.compiledTemplateRichBox.Document = this._viewModel.CompiledTemplateDocument ?? new FlowDocument();

                    break;

                case nameof(MainViewModel.TransformedCodeDocument):
                    this.transformedCodeRichBox.Document = this._viewModel.TransformedCodeDocument ?? new FlowDocument();

                    break;

                case nameof(MainViewModel.ErrorsDocument):
                    this.errorsTextBlock.Document = this._viewModel.ErrorsDocument ?? new FlowDocument();

                    break;

                case nameof(MainViewModel.IntermediateLinkerCodeCodeDocument):
                    this.intermediateLinkerCodeTextBox.Document = this._viewModel.IntermediateLinkerCodeCodeDocument ?? new FlowDocument();

                    break;
            }
        }

        private void UpdateViewModel()
        {
            this._viewModel.SourceCode = this.sourceTextBox.Text;

            // Alternatively set the UpdateSourceTrigger property of the TextBox binding to PropertyChanged.
            this.expectedOutputTextBox.GetBindingExpression( TextBox.TextProperty )!.UpdateSource();
        }

        private void NewButton_Click( object sender, RoutedEventArgs e )
        {
            var dlg = new SaveFileDialog { DefaultExt = _fileDialogueExt, Filter = _fileDialogueFilter, InitialDirectory = _testsProjectPath };

            if ( dlg.ShowDialog() == false )
            {
                return;
            }

            this._viewModel.NewTest( dlg.FileName );
        }

        private async void OpenButton_Click( object sender, RoutedEventArgs e )
        {
            var dlg = new OpenFileDialog { DefaultExt = _fileDialogueExt, Filter = _fileDialogueFilter, InitialDirectory = _testsProjectPath };

            if ( dlg.ShowDialog() == true )
            {
                await this._viewModel.LoadTestAsync( dlg.FileName );
            }
        }

        private async void SaveButton_Click( object sender, RoutedEventArgs e )
        {
            if ( this._viewModel.IsNewTest )
            {
                this.SaveAsButton_Click( sender, e );

                return;
            }

            this.UpdateViewModel();
            await this._viewModel.SaveTestAsync( null );
        }

        private async void SaveAsButton_Click( object sender, RoutedEventArgs e )
        {
            this.UpdateViewModel();

            var dlg = new SaveFileDialog { DefaultExt = _fileDialogueExt, Filter = _fileDialogueFilter, InitialDirectory = _testsProjectPath };

            if ( dlg.ShowDialog() == false )
            {
                return;
            }

            await this._viewModel.SaveTestAsync( dlg.FileName );
        }

        private async void RunButton_Click( object sender, RoutedEventArgs e )
        {
            this.UpdateViewModel();

            try
            {
                await this._viewModel.RunTestAsync();
            }
            catch ( Exception exception )
            {
                MessageBox.Show( this, exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK );
            }
        }

        private void MakeExpectedTransformedCodeButton_Click( object sender, RoutedEventArgs e )
        {
            if ( this._viewModel.TransformedCodeDocument == null )
            {
                this._viewModel.ExpectedTransformedCode = "";

                return;
            }

            this._viewModel.ExpectedTransformedCode = new TextRange(
                this._viewModel.TransformedCodeDocument.ContentStart,
                this._viewModel.TransformedCodeDocument.ContentEnd ).Text;
        }

        private void MakeExpectedProgramOutputButton_Click( object sender, RoutedEventArgs e )
        {
            this._viewModel.ExpectedProgramOutput = this._viewModel.ActualProgramOutput;
        }
    }
}
using Management_System_WPF.Helpers;
using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace Management_System_WPF.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private MemoryStream _ms;
        private Package _package;
        private Uri _uri;
        private FixedDocumentSequence _fixedSequence;
        public PrintPreviewWindow(FlowDocument flowDoc)
        {
            InitializeComponent();
            LoadDocument(flowDoc);
            this.KeyDown += PrintPreviewWindow_KeyDown;
            this.Closed += (s, e) =>
            {
                PackageStore.RemovePackage(_uri);
                _package?.Close();
                _ms?.Close();
            };
        }

        private void LoadDocument(FlowDocument flowDoc)
        {
            flowDoc.PagePadding = new Thickness(0);
            flowDoc.ColumnGap = 0;
            flowDoc.ColumnWidth = double.PositiveInfinity;
            try
            {
                _ms = new MemoryStream();
                _package = Package.Open(_ms, FileMode.Create, FileAccess.ReadWrite);
                _uri = new Uri("pack://temp.xps");
                PackageStore.AddPackage(_uri, _package);

                var xpsDoc = new XpsDocument(_package, CompressionOption.NotCompressed, _uri.ToString());
                XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

                var originalPaginator = ((IDocumentPaginatorSource)flowDoc).DocumentPaginator;

                originalPaginator.PageSize = new Size(780, 1120);

                var numberedPaginator = new PageNumberPaginator(originalPaginator, originalPaginator.PageSize);


                writer.Write(numberedPaginator);


                _fixedSequence = xpsDoc.GetFixedDocumentSequence();
                docViewer.Document = _fixedSequence;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error previewing: " + ex.Message);
            }
        }


        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            pd.UserPageRangeEnabled = true;

            if (pd.ShowDialog() == true)
            {
                // Use the actual printable area of the printer selected by the user
                double printableWidth = pd.PrintableAreaWidth;
                double printableHeight = pd.PrintableAreaHeight;

                // Re-paginate with the correct size right before printing
                _fixedSequence.DocumentPaginator.PageSize = new Size(printableWidth, printableHeight);

                pd.PrintDocument(_fixedSequence.DocumentPaginator, "Invoice Print");
            }
        }


        private void PDF_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();


            var pdfQueue = new System.Printing.LocalPrintServer()
                .GetPrintQueues()
                .FirstOrDefault(q => q.Name.Contains("Microsoft Print to PDF"));

            if (pdfQueue != null)
            {
                pd.PrintQueue = pdfQueue;

            }
            else
            {
                MessageBox.Show("Microsoft Print to PDF driver not found. Please select a PDF printer manually.");
            }

            try
            {

                pd.PrintDocument(_fixedSequence.DocumentPaginator, $"Invoice_{DateTime.Now:yyyyMMdd_HHmm}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving PDF: " + ex.Message);
            }
        }
        private void PrintPreviewWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Print_Click(null, null);
            }
        }

    }
}
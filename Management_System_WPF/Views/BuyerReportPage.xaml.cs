using Management_System_WPF.Helpers;
using Management_System_WPF.Services;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;


namespace Management_System_WPF.Views
{
    public partial class BuyerReportPage : Page
    {
        int buyerId;

        //  Tracks the MONTH currently being viewed
        private DateTime _currentMonth;

        public BuyerReportPage(int id, string buyerName = "")
        {
            InitializeComponent();

            ((MainWindow)Application.Current.MainWindow).ShowFullScreenPage();

            buyerId = id;

            // Set the buyer name ONCE — do NOT overwrite it later
            if (!string.IsNullOrEmpty(buyerName))
                txtBuyerName.Text = buyerName;

            // Start at current system month
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            LoadBuyerData();
        }

       
        //  LOAD DATA FOR SELECTED MONTH
        
        private void LoadBuyerData()
        {
            var raw = SalesService.GetSalesRawForPivot(
                buyerId,
                _currentMonth.Year,
                _currentMonth.Month
            );

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(raw);
            BindPivotToGrid(pivot);

          

            //  SHOW MONTH NAME (not buyer name)
            txtMonthName.Text = _currentMonth.ToString("MMMM yyyy");

            //  UPDATE BUTTON STATES
            UpdateMonthNavigationButtons();
        }

        
        //  PREVIOUS MONTH BUTTON
       
        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var prev = SalesService.GetPreviousSalesMonthForBuyer(buyerId, _currentMonth);

            if (prev != null)
            {
                _currentMonth = prev.Value;
                LoadBuyerData();
            }
        }


       
        //  NEXT MONTH BUTTON
        
        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            var next = SalesService.GetNextSalesMonthForBuyer(buyerId, _currentMonth);

            if (next != null)
            {
                _currentMonth = next.Value;
                LoadBuyerData();
            }
        }


       
        // ENABLE/DISABLE NAVIGATION BUTTONS
       
        private void UpdateMonthNavigationButtons()
        {
            btnPrevMonth.IsEnabled =
                SalesService.GetPreviousSalesMonthForBuyer(buyerId, _currentMonth) != null;

            btnNextMonth.IsEnabled =
                SalesService.GetNextSalesMonthForBuyer(buyerId, _currentMonth) != null;

            btnPrevMonth.Opacity = btnPrevMonth.IsEnabled ? 1.0 : 0.4;
            btnNextMonth.Opacity = btnNextMonth.IsEnabled ? 1.0 : 0.4;
        }



        // BUILD GRID

        private void BindPivotToGrid(DataTable pivot)
        {
            dgBuyerReport.Columns.Clear();

            foreach (DataColumn col in pivot.Columns)
            {
                if (col.ColumnName.Equals("Date", StringComparison.OrdinalIgnoreCase))
                {
                    dgBuyerReport.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Date",
                        Binding = new Binding("Date"),
                        FontSize = 16,
                        IsReadOnly = true,          // ✅ NON EDITABLE
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    });
                }
                else
                {
                    dgBuyerReport.Columns.Add(new DataGridTextColumn
                    {
                        Header = col.ColumnName,
                        Binding = new Binding(col.ColumnName),
                        FontSize = 16,
                        IsReadOnly = true,          // ✅ NON EDITABLE
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    });
                }
            }

            dgBuyerReport.ItemsSource = pivot.DefaultView;
        }





        //  BACK & PRINT

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var main = (MainWindow)Application.Current.MainWindow;
            main.ResetLayoutBeforeNavigation();
            NavigationService.GoBack();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() != true) return;

            FlowDocument doc = BuildInvoiceDocument();

            doc.PageHeight = pd.PrintableAreaHeight;
            doc.PageWidth = pd.PrintableAreaWidth;
            doc.PagePadding = new Thickness(40);
            doc.ColumnWidth = pd.PrintableAreaWidth;

            pd.PrintDocument(
                ((IDocumentPaginatorSource)doc).DocumentPaginator,
                "Sales Invoice"
            );
        }
        private FlowDocument BuildInvoiceDocument()
        {
            FlowDocument doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13
            };

            // 🧾 TITLE
            doc.Blocks.Add(new Paragraph(new Run("SALES INVOICE"))
            {
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Buyer + Period
            doc.Blocks.Add(new Paragraph(new Run(
                $"Name : {txtBuyerName.Text}\n" +
                $"Invoice For : {txtMonthName.Text}\n" +
                $"Invoice Date : {DateTime.Now:dd/MM/yyyy}"
            ))
            {
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // 🔹 FETCH RAW DATA (NOT PIVOT)
            var sales = SalesService.GetSalesBetweenDates(
                buyerId,
                _currentMonth,
                _currentMonth.AddMonths(1).AddDays(-1)
            );

            Table table = new Table();
            doc.Blocks.Add(table);

            table.Columns.Add(new TableColumn { Width = new GridLength(110) }); // Date
            table.Columns.Add(new TableColumn { Width = new GridLength(180) }); // Item
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });  // Qty
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });  // Rate
            table.Columns.Add(new TableColumn { Width = new GridLength(120) }); // Amount


            // HEADER
            TableRowGroup headerGroup = new TableRowGroup();
            table.RowGroups.Add(headerGroup);

            TableRow header = new TableRow();
            headerGroup.Rows.Add(header);

            HeaderCell("Date");
            HeaderCell("Item");
            HeaderCell("Total Qty");
            HeaderCell("Unit Price");
            HeaderCell("Total Price");


            // BODY
            TableRowGroup body = new TableRowGroup();
            table.RowGroups.Add(body);

            decimal grandTotal = 0m;
            foreach (var s in sales)
            {
                decimal amount = s.Qty * s.Price;
                grandTotal += amount;


                TableRow row = new TableRow();
                body.Rows.Add(row);

                row.Cells.Add(Cell(
                DateTime.TryParse(s.Date.ToString(), out DateTime d)
                ? d.ToString("dd/MM/yyyy")
                 : s.Date.ToString()
                 ));
                row.Cells.Add(Cell(s.ItemName));
                row.Cells.Add(Cell(s.Qty.ToString()));
                row.Cells.Add(Cell($"₹ {s.Price:0.00}"));
                row.Cells.Add(Cell($"₹ {amount:0.00}"));
            }



            // 🔹 GRAND TOTAL ROW (INSIDE TABLE)
            TableRow totalRow = new TableRow();
            body.Rows.Add(totalRow);

            totalRow.Cells.Add(Cell(""));
            totalRow.Cells.Add(Cell(""));
            totalRow.Cells.Add(Cell(""));
            totalRow.Cells.Add(Cell("GRAND TOTAL", true));
            totalRow.Cells.Add(Cell($"₹ {grandTotal:0.00}", true));


            // SIGNATURE
            doc.Blocks.Add(new Paragraph(new Run("\n\nAuthorized Signature"))
            {
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 40, 0, 0)
            });

            return doc;

            // Local helpers
            void HeaderCell(string text)
            {
                header.Cells.Add(new TableCell(new Paragraph(new Run(text)))
                {
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.LightGray,
                    Padding = new Thickness(6),
                    TextAlignment = TextAlignment.Center
                });
            }
        }

        private TableCell Cell(string text, bool bold = false)
        {
            return new TableCell(new Paragraph(new Run(text)))
            {
                Padding = new Thickness(6),
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
            };
        }



        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility =
                FilterPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        private void FilterThisMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadBuyerData();
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterSixMonths_Click(object sender, RoutedEventArgs e)
        {
            DateTime from = DateTime.Now.AddMonths(-6);
            DateTime to = DateTime.Now;

            var raw = SalesService.GetSalesBetweenDates(buyerId, from, to);

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(raw);
            BindPivotToGrid(pivot);

            txtMonthName.Text = "Last 6 Months";
            

            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterYear_Click(object sender, RoutedEventArgs e)
        {
            DateTime from = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime to = DateTime.Now;

            var raw = SalesService.GetSalesBetweenDates(buyerId, from, to);

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(raw);
            BindPivotToGrid(pivot);

            txtMonthName.Text = DateTime.Now.Year + " (Year)";
           
            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void FilterCustom_Click(object sender, RoutedEventArgs e)
        {
            if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates.");
                return;
            }

            DateTime from = dpFrom.SelectedDate.Value;
            DateTime to = dpTo.SelectedDate.Value;

            var raw = SalesService.GetSalesBetweenDates(buyerId, from, to);

            DataTable pivot = PivotHelper.CreatePivotTableWithTotals(raw);
            BindPivotToGrid(pivot);

            txtMonthName.Text = $"{from:dd/MM/yyyy} → {to:dd/MM/yyyy}";
           

            FilterPanel.Visibility = Visibility.Collapsed;
        }
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataView dv = dgBuyerReport.ItemsSource as DataView;
                if (dv == null || dv.Count == 0)
                {
                    MessageBox.Show("No data available to export.");
                    return;
                }

                // Convert DataView → DataTable
                DataTable dt = dv.ToTable();

                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Buyer Report");

                    //  Write Headers
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        ws.Cells[1, col + 1].Value = dt.Columns[col].ColumnName;
                        ws.Cells[1, col + 1].Style.Font.Bold = true;
                        ws.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Teal);
                        ws.Cells[1, col + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                        ws.Column(col + 1).Width = 18;
                    }

                    //  Write Data Rows
                    for (int row = 0; row < dt.Rows.Count; row++)
                    {
                        for (int col = 0; col < dt.Columns.Count; col++)
                        {
                            ws.Cells[row + 2, col + 1].Value = dt.Rows[row][col];
                        }
                    }

                    //  Apply Borders
                    var range = ws.Cells[1, 1, dt.Rows.Count + 1, dt.Columns.Count];
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    //  AutoFit
                    ws.Cells.AutoFitColumns();

                    //  Create file
                    string fileName = $"BuyerReport_{txtBuyerName.Text}_{DateTime.Now:yyyyMMdd}.xlsx";
                    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                    File.WriteAllBytes(filePath, package.GetAsByteArray());

                    MessageBox.Show($"Excel Exported Successfully!\nSaved at Desktop:\n{fileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excel Export Failed:\n" + ex.Message);
            }
        }


    }
}

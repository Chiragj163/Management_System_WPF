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
            main.ResizeMode = ResizeMode.CanResize;
            if (main.WindowState == WindowState.Maximized)
            {
                main.WindowState = WindowState.Normal;
                main.WindowState = WindowState.Maximized;
            }
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        // ================= PRINT LOGIC STARTS HERE =================

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() != true) return;

            // Get Printer Size
            double pageWidth = pd.PrintableAreaWidth;
            double pageHeight = pd.PrintableAreaHeight;

            FlowDocument doc = BuildInvoiceDocument();

            // --- UPDATED SETTINGS ---
            doc.PageHeight = pageHeight;
            doc.PageWidth = pageWidth;

            // Set PagePadding to fit the borders nicely
            doc.PagePadding = new Thickness(30);

            // Calculate the width available for content
            // This helps the table know how wide it can be
            doc.ColumnWidth = pageWidth - 60; // (PageWidth - Left/Right Padding)

            pd.PrintDocument(
                ((IDocumentPaginatorSource)doc).DocumentPaginator,
                $"Report - {txtBuyerName.Text}"
            );
        }

        private FlowDocument BuildInvoiceDocument()
        {
            FlowDocument doc = new FlowDocument
            {
                FontFamily = new FontFamily("Calibri"), // Excel default font
                FontSize = 10, // Point 2: Smaller font to fit more data
                TextAlignment = TextAlignment.Left
            };

            // ================= 1. HEADING (BUYER NAME) =================
            // Point 1: Replaced "SALES INVOICE" with Buyer Name
            Paragraph titlePara = new Paragraph(new Run(txtBuyerName.Text.ToUpper()))
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            doc.Blocks.Add(titlePara);

            // Sub-heading (Month and Print Date)
            Paragraph subHeader = new Paragraph(new Run($"{txtMonthName.Text} | Generated: {DateTime.Now:dd-MM-yyyy}"))
            {
                FontSize = 12,
                Foreground = Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(subHeader);


            // ================= FETCH DATA =================
            var sales = SalesService.GetSalesBetweenDates(
                buyerId,
                _currentMonth,
                _currentMonth.AddMonths(1).AddDays(-1)
            );

            if (sales == null || sales.Count == 0)
            {
                doc.Blocks.Add(new Paragraph(new Run("No sales data available for this month.")));
                return doc;
            }

            // Grouping Logic
            var items = sales.GroupBy(x => x.ItemName)
                .Select(g => new
                {
                    Item = g.Key,
                    UnitPrice = g.First().Price,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Qty * x.Price),
                    DateQty = g.GroupBy(x => x.Date.ToString())
                               .ToDictionary(d => d.Key, d => d.Sum(x => x.Qty))
                }).ToList();

            var dates = sales.Select(x => x.Date.ToString()).Distinct().OrderBy(x => x).ToList();


            // ================= TABLE SETUP =================
            Table table = new Table();
            table.CellSpacing = 0;
            doc.Blocks.Add(table);

            // 1. Date Column (Fixed Width)
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });

            // 2. Item Columns (CHANGE THIS)
            // Star sizing (*) often fails in PrintDialog. Use a fixed width or Auto.
            foreach (var _ in items)
            {
                // Try 80 pixels per item column. 
                // If you have many items, the table will simply stretch to the right.
                table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            }


            // ================= TABLE HEADER =================
            TableRowGroup headerGroup = new TableRowGroup();
            table.RowGroups.Add(headerGroup);
            TableRow headerRow = new TableRow();
            headerGroup.Rows.Add(headerRow);

            // Header Cells (Point 3: Excel Style Headers)
            AddExcelCell(headerRow, "Date", true, true);
            foreach (var item in items)
            {
                AddExcelCell(headerRow, item.Item, true, true);
            }


            // ================= TABLE BODY =================
            TableRowGroup body = new TableRowGroup();
            table.RowGroups.Add(body);

            foreach (var date in dates)
            {
                TableRow row = new TableRow();
                body.Rows.Add(row);

                // Date Cell
                string dateStr = DateTime.TryParse(date, out DateTime d) ? d.ToString("dd-MM/yyyy") : date;
                AddExcelCell(row, dateStr, false, false);

                // Qty Cells
                foreach (var item in items)
                {
                    string qty = item.DateQty.ContainsKey(date) ? item.DateQty[date].ToString() : "";
                    AddExcelCell(row, qty, false, false);
                }
            }


            // ================= SUMMARY ROWS (Totals) =================
            // Divider Row
            TableRow emptyRow = new TableRow();
            body.Rows.Add(emptyRow); // Spacer

            // Total Qty Row
            TableRow qtyRow = new TableRow();
            body.Rows.Add(qtyRow);
            AddExcelCell(qtyRow, "Total Qty", true, true);
            foreach (var item in items) AddExcelCell(qtyRow, item.TotalQty.ToString(), true, false);

            // Unit Price Row
            TableRow priceRow = new TableRow();
            body.Rows.Add(priceRow);
            AddExcelCell(priceRow, "Price", true, true);
            foreach (var item in items) AddExcelCell(priceRow, item.UnitPrice.ToString("0.00"), false, false);

            // Total Amount Row
            TableRow amountRow = new TableRow();
            body.Rows.Add(amountRow);
            AddExcelCell(amountRow, "Amount", true, true);
            foreach (var item in items) AddExcelCell(amountRow, item.TotalAmount.ToString("0.00"), true, false);


            // ================= GRAND TOTAL =================
            decimal grandTotal = items.Sum(x => x.TotalAmount);

            Paragraph totalPara = new Paragraph(new Run($"Grand Total: {grandTotal:N2}"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            doc.Blocks.Add(totalPara);

            return doc;
        }


        // ================= HELPER: CREATE EXCEL STYLE CELL =================
        // Point 3: This function creates the borders and gridlines
        // ================= HELPER: CREATE EXCEL STYLE CELL =================

        private void AddExcelCell(TableRow row, string text, bool isBold, bool isHeader)
        {
            // 1. Create the Text Content
            Paragraph p = new Paragraph(new Run(text));
            p.Margin = new Thickness(0); // Remove default paragraph gap
            p.TextAlignment = TextAlignment.Center;
            p.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;

            // 2. Create the TableCell and add the Paragraph to it
            // TableCell automatically accepts a 'Block' (like Paragraph) in its constructor
            TableCell cell = new TableCell(p);

            // 3. Apply Excel Styling DIRECTLY to the Cell
            cell.BorderBrush = Brushes.Black;
            cell.BorderThickness = new Thickness(0.5); // This creates the gridline
            cell.Padding = new Thickness(4, 2, 4, 2);  // Padding inside the cell

            if (isHeader)
            {
                cell.Background = Brushes.LightGray;
            }

            // 4. Add to Row
            row.Cells.Add(cell);
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

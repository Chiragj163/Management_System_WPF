using Management_System.Models;
using Management_System_WPF.Helpers;
using Management_System_WPF.Models;
using Management_System_WPF.Services;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace Management_System_WPF.Views
{
    public partial class SaleByBuyerPage : Page
    {
       
        private DateTime _currentMonth;

        public SaleByBuyerPage()
        {
            InitializeComponent();

           

          

            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadReport(_currentMonth); // ✅ FIX
        }





        // LOAD REPORT (CURRENT MONTH / PREVIOUS MONTH)

        private void LoadReport(DateTime month)
        {
            var allSales = SalesService.GetSales();




            var sales = allSales
                .Where(s => s.SaleDate.Month == month.Month && s.SaleDate.Year == month.Year)
                .ToList();

            if (!sales.Any())
            {
                MessageBox.Show("No records found for this month");
                dgBuyerSales.ItemsSource = null;
                dgBuyerSales.Columns.Clear();
                txtTitle.Text = month.ToString("MMMM yyyy");
                UpdateMonthButtons();
                return;
            }

            txtTitle.Text = month.ToString("MMMM yyyy");

            // ✅ NORMALIZED BUYERS
            var buyers = sales
                .Select(s => s.BuyerName?.Trim())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var dates = sales
                .Select(s => s.SaleDate.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var rows = new List<SaleByBuyerRow>();
            sales = sales
    .Where(s => !string.IsNullOrWhiteSpace(s.BuyerName))
    .ToList();

            foreach (var date in dates)
            {
                var row = new SaleByBuyerRow { Date = date };
                decimal rowTotal = 0m;


                foreach (var buyer in buyers)
                {
                    decimal total = sales
                        .Where(x => x.BuyerName?.Trim().Equals(buyer, StringComparison.OrdinalIgnoreCase) == true
                                 && x.SaleDate.Date == date)
                        .Sum(x => x.Qty * x.Price);

                    row.BuyerValues[buyer] = total > 0 ? total : null;

                    if (total > 0) rowTotal += total;
                }

                row.Total = rowTotal > 0 ? rowTotal : null;
                rows.Add(row);
            }
            // ================= TOTAL ROW =================
            var totalRow = new SaleByBuyerRow { Date = DateTime.MinValue };
            decimal grandTotal = 0m;

            foreach (var buyer in buyers)
            {
                decimal colTotal = rows
                    .Where(r => r.BuyerValues.ContainsKey(buyer) && r.BuyerValues[buyer].HasValue)
                    .Sum(r => r.BuyerValues[buyer]!.Value);

                totalRow.BuyerValues[buyer] = colTotal > 0 ? colTotal : null;
                grandTotal += colTotal;
            }

            totalRow.Total = grandTotal;
            rows.Add(totalRow);


            // ✅ SAFE REFRESH ORDER
            dgBuyerSales.ItemsSource = null;
            dgBuyerSales.Columns.Clear();
            BuildDynamicColumns(buyers);
            dgBuyerSales.ItemsSource = rows;

            UpdateMonthButtons();
        }



        // BUILD TABLE COLUMNS

        private void BuildDynamicColumns(List<string> buyers)
        {
            dgBuyerSales.Columns.Clear();

            // ================= DATE COLUMN =================
            dgBuyerSales.Columns.Add(new DataGridTextColumn
            {
                Header = "Dates",
                Binding = new Binding("Date")
                {
                    Converter = new DateOrTotalConverter()
                },
                Width = 150,
                IsReadOnly = true
            });

            // ================= BUYER COLUMNS =================
            foreach (var buyer in buyers)
            {
                dgBuyerSales.Columns.Add(new DataGridTextColumn
                {
                    Header = buyer,
                    Binding = new Binding($"BuyerValues[{buyer}]")
                    {
                        StringFormat = "0.##"
                    },
                    Width = 120,
                    IsReadOnly = true
                });
            }

            // ================= TOTAL COLUMN (ONLY ONCE) =================
            dgBuyerSales.Columns.Add(new DataGridTextColumn
            {
                Header = "Total",
                Binding = new Binding("Total")
                {
                    StringFormat = "0.##"
                },
                Width = 140,
                IsReadOnly = true
            });
        }


        public class DateOrTotalConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value == null || value == DependencyProperty.UnsetValue)
                    return "";

                if (value is DateTime dt)
                {
                    if (dt == DateTime.MinValue)
                        return "Total";

                    return dt.ToString("dd/MM/yyyy");
                }

                return "";
            }


            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotImplementedException();
        }


        // BUTTON: PREVIOUS MONTH

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            var prev = SalesService.GetPreviousSalesMonth(_currentMonth);

            if (prev != null)
            {
                _currentMonth = prev.Value;
                LoadReport(_currentMonth);
            }
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            var next = SalesService.GetNextSalesMonth(_currentMonth);

            if (next != null)
            {
                _currentMonth = next.Value;
                LoadReport(_currentMonth);
            }
        }




        // BUTTON: EXPORT TO EXCEL (CSV format)

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            // 1. Get the raw data source (Better than looping UI items)
            var rows = dgBuyerSales.ItemsSource as IEnumerable<SaleByBuyerRow>;
            if (rows == null || !rows.Any())
            {
                MessageBox.Show("Nothing to export!");
                return;
            }

            // 2. Configure Save Dialog for .xlsx
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"SalesByBuyer_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                // 3. Set License (Required for EPPlus 5+)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Sales Report");

                    // --- HEADERS ---

                    // Get all unique Buyer names from the data to ensure we have columns for everyone
                    var allBuyers = rows
      .Where(r => r?.BuyerValues != null)
      .SelectMany(r => r.BuyerValues.Keys)
      .Distinct()
      .OrderBy(x => x)
      .ToList();


                    // Setup Header Row
                    ws.Cells[1, 1].Value = "Date";
                    int col = 2;
                    foreach (var buyer in allBuyers)
                    {
                        ws.Cells[1, col++].Value = buyer;
                    }
                    // Add "Total" Header
                    ws.Cells[1, col].Value = "Total";

                    // Dictionary for Vertical Totals (Bottom Row)
                    var columnTotals = allBuyers.ToDictionary(b => b, b => 0m);
                    decimal grandTotal = 0;


                    // --- DATA ROWS ---
                    int rowIdx = 2;
                    foreach (var r in rows)
                    {
                        if (r.Date.Year < 1900) continue;
                        // Date Column
                        ws.Cells[rowIdx, 1].Value = r.Date;
                        ws.Cells[rowIdx, 1].Style.Numberformat.Format = "dd/mm/yyyy";
                        ws.Cells[rowIdx, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        col = 2;
                        decimal rowSum = 0; // Horizontal Total

                        foreach (var buyer in allBuyers)
                        {
                            decimal val = 0;
                            if (r.BuyerValues.ContainsKey(buyer) && r.BuyerValues[buyer].HasValue)
                            {
                                val = (decimal)r.BuyerValues[buyer].Value;
                            }

                            if (val > 0)
                            {
                                ws.Cells[rowIdx, col].Value = val;
                                rowSum += val;
                                columnTotals[buyer] += val; // Add to vertical total
                            }
                            else
                            {
                                ws.Cells[rowIdx, col].Value = 0;
                                ws.Cells[rowIdx, col].Style.Font.Color.SetColor(System.Drawing.Color.LightGray);
                            }
                            col++;
                        }

                        // Write Row Total (Last Column)
                        ws.Cells[rowIdx, col].Value = rowSum;
                        ws.Cells[rowIdx, col].Style.Font.Bold = true;
                        ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.WhiteSmoke);

                        grandTotal += rowSum;
                        rowIdx++;
                    }

                    // --- TOTAL ROW (BOTTOM) ---
                    ws.Cells[rowIdx, 1].Value = " TOTAL";
                    ws.Cells[rowIdx, 1].Style.Font.Bold = true;

                    col = 2;
                    foreach (var buyer in allBuyers)
                    {
                        ws.Cells[rowIdx, col].Value = columnTotals[buyer];
                        ws.Cells[rowIdx, col].Style.Font.Bold = true;
                        ws.Cells[rowIdx, col].Style.Border.Top.Style = ExcelBorderStyle.Medium;
                        col++;
                    }

                    // Bottom Right Grand Total
                    ws.Cells[rowIdx, col].Value = grandTotal;
                    ws.Cells[rowIdx, col].Style.Font.Bold = true;
                    ws.Cells[rowIdx, col].Style.Font.Size = 12;
                    ws.Cells[rowIdx, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[rowIdx, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // --- STYLING ---

                    // Header Style
                    var headerRange = ws.Cells[1, 1, 1, col];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Teal);
                    headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);

                    // Borders
                    var fullRange = ws.Cells[1, 1, rowIdx, col];
                    fullRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
fullRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
fullRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
fullRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                    // AutoFit
                    ws.Cells.AutoFitColumns();
                    ws.View.FreezePanes(2, 1); // Freeze header

                    // Save
                    File.WriteAllBytes(dialog.FileName, package.GetAsByteArray());

                    // Ask to open
                    if (MessageBox.Show("Export Successful! Open file now?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dialog.FileName, UseShellExecute = true });
                    }
                }
            }
            catch (IOException)
            {
                MessageBox.Show("The file is open in Excel. Please close it and try again.", "File Locked", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting: " + ex.Message);
            }
        }


        // BUTTON: PRINT



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
        private void UpdateMonthButtons()
        {
            btnPrevMonth.IsEnabled = SalesService.GetPreviousSalesMonth(_currentMonth) != null;
            btnNextMonth.IsEnabled = SalesService.GetNextSalesMonth(_currentMonth) != null;

            btnPrevMonth.Opacity = btnPrevMonth.IsEnabled ? 1.0 : 0.4;
            btnNextMonth.Opacity = btnNextMonth.IsEnabled ? 1.0 : 0.4;
        }


    }
}

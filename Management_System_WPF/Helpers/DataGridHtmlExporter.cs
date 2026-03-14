using System.Data;
using System.Text;

public static class DataGridHtmlExporter
{
    public static string ConvertToHtml(DataTable table, string buyerName, string month, string grandTotal, string payment, string balance)
    {
        StringBuilder html = new StringBuilder();

        html.Append(@"
    <html>
    <head>
    <style>
        @page {
            /* Sets the outer white margin of the paper and hides default browser headers */
            margin: 15mm; 
        }

        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 15px; /* Keeps your text safely inside the new black border */
        }

        /* --- THE MAGIC OUTLINE --- */
        .page-outline {
            position: fixed;
            top: 0;
            bottom: 0;
            left: 0;
            right: 0;
            border: 2px solid black; /* Adjust thickness here */
            z-index: -1;
            pointer-events: none;
        }

        table {
            border-collapse: collapse;
            width: 100%;
            /* Ensures the table doesn't break in the middle of a row */
            page-break-inside: auto;
        }

        tr {
            page-break-inside: avoid;
            page-break-after: auto;
        }

        th, td {
            border: 1px solid #555;
            padding: 6px;
            text-align: center;
        }

        /* CRITICAL: This makes the header repeat on every page */
        thead {
            display: table-header-group;
        }

        th {
            background: #d0f0f0 !important;
            -webkit-print-color-adjust: exact; /* Ensures color shows in print */
        }

        .title {
            font-size: 24px;
            font-weight: bold;
            text-align: center;
            margin-bottom: 10px;
        }

        .summary-table {
            margin-top: 30px;
            page-break-inside: avoid; /* Keeps the summary block together */
        }
    </style>
    </head>
    <body>
    
    <div class='page-outline'></div>");

        html.Append($"<div class='title'>{buyerName} </div>");

        html.Append("<table>");

        // Wrap headers in THEAD for repetition
        html.Append("<thead><tr>");
        foreach (DataColumn col in table.Columns)
        {
            html.Append($"<th>{col.ColumnName}</th>");
        }
        html.Append("</tr></thead>");

        // Wrap rows in TBODY
        html.Append("<tbody>");
        
        // ... (Your existing code for the DataRow loop)
        foreach (DataRow row in table.Rows)
        {
            html.Append("<tr>");
            foreach (var item in row.ItemArray)
            {
                html.Append($"<td>{item}</td>");
            }
            html.Append("</tr>");
        }

        // 1. Calculate how many columns the value cell needs to span 
        int colSpan = table.Columns.Count > 1 ? table.Columns.Count - 1 : 1;

        // 2. Add the summary section as standard rows INSIDE the tbody (Black and White)
        html.Append($@"
        <tr style='color:black; font-size:18px;'>
            <td style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>Grand Total</td>
            <td colspan='{colSpan}' style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>{grandTotal}</td>
        </tr>
        <tr style='color:black; font-size:18px;'>
            <td style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>Less Amount</td>
            <td colspan='{colSpan}' style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>{payment}</td>
        </tr>
        <tr style='color:black; font-size:18px;'>
            <td style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>Due Amount</td>
            <td colspan='{colSpan}' style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>{balance}</td>
        </tr>
        ");

        // 3. NOW close the main body and the table
        html.Append("</tbody>");
        html.Append("</table>");

        html.Append("</body></html>");

        return html.ToString();
    }
}
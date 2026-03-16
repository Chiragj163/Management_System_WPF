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
            margin: 5mm; 
        }
        body {
            font-family: Arial, sans-serif;
            margin: 0; 
            /* 15px top, 0px right, 0px bottom, 0px left */
            padding: 0px 0px 0px 0px; 
        }

        /* --- THE MAGIC OUTLINE --- */
        .page-outline {
            position: fixed;
            top: 0;
            bottom: 0;
            left: 0;
            right: 0;
            border: 1px solid black; /* Adjust thickness here */
            z-index: -1;
            pointer-events: none;
        }

        table {
            /* CHANGE THIS to 'separate' */
            border-collapse: separate; 
            
            width: 100%;
            border-spacing: 3px; /* Now this will actually work! */
            page-break-inside: auto;
        }

        tr {
            page-break-inside: avoid;
            page-break-after: auto;
        }

        th, td {
            /* 3. Make the border a slightly lighter grey to match your screenshot */
            border: 1px solid black; 
            padding: 6px;
            text-align: center;
            font-weight: bold;
            white-space: pre-line;
            /* Optional: uncomment the line below to give the boxes slightly rounded corners */
            /* border-radius: 2px; */
        }

        /* CRITICAL: This makes the header repeat on every page */
        thead {
            display: table-header-group;
        }

       th {
            /* White background with Teal text to match the screenshot */
            background: #ffffff !important;
            color: solid balck; /* Teal color */
            font-weight: bold;
            -webkit-print-color-adjust: exact;
        }
        .title {
            font-size: 24px;
            font-weight: bold;
            text-align: center;
            margin-bottom: 10px;
text-transform: uppercase;
        }

        .summary-table {
            margin-top: 30px;
            page-break-inside: avoid; /* Keeps the summary block together */
        }
    </style>
    </head>
    <body>
    
    <div class='page-outline'></div>");

      // Start the table
        html.Append("<table>");
        html.Append($"<caption style='font-size: 22px; font-weight: bold; color: black; text-transform: uppercase; padding: 12px; letter-spacing: 1px;border: 1px solid black;  margin: 3px;'>");
        html.Append(buyerName);
        html.Append("</caption>");
        html.Append("<thead>");
        // 1. --- NEW TITLE ROW ---
        // This creates a single cell that spans across all columns


        // 2. --- EXISTING COLUMN HEADERS ---
        html.Append("<tr>");
        foreach (DataColumn col in table.Columns)
        {
            html.Append($"<th>{col.ColumnName}</th>");
        }
        html.Append("</tr>");
        
        html.Append("</thead>");

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

        // 2. Add the Grand Total row (This always prints)
        html.Append($@"
        <tr style='color:black; font-size:18px;'>
            <td style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>Grand Total</td>
            <td colspan='{colSpan}' style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>{grandTotal}</td>
        </tr>");

        // 3. Clean the string! Remove the '₹' symbol and any spaces so C# can read the pure number
        string cleanPaymentStr = payment.Replace("₹", "").Replace(" ", "").Trim();

        // 4. Safely parse the cleaned string
        decimal parsedPayment = 0;
        decimal.TryParse(cleanPaymentStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsedPayment);

        // 5. Only print Less and Due if the parsed payment is greater than 0
        if (parsedPayment > 0)
        {
            html.Append($@"
            <tr style='color:black; font-size:18px;'>
                <td style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>Less Amount</td>
                <td colspan='{colSpan}' style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>{payment}</td>
            </tr>
            <tr style='color:black; font-size:18px;'>
                <td style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>Due Amount</td>
                <td colspan='{colSpan}' style='padding:10px; border:1px solid black; font-weight:bold; text-align:center; vertical-align:middle;'>{balance}</td>
            </tr>");
        }

        // 6. NOW close the main body and the table
        html.Append("</tbody>");
        html.Append("</table>");

        html.Append("</body></html>");

        return html.ToString();
    }
}
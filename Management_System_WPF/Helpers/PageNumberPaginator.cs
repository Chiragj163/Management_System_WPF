using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Management_System_WPF.Helpers
{
    public class PageNumberPaginator : DocumentPaginator
    {
        private readonly DocumentPaginator _originalPaginator;
        private readonly Typeface _typeface;
        private readonly double _fontSize;
        private readonly Size _pageSize;

        public PageNumberPaginator(DocumentPaginator originalPaginator, Size pageSize)
        {
            _originalPaginator = originalPaginator;
            _pageSize = pageSize;
            _typeface = new Typeface("Arial");
            _fontSize = 12;

            // Force calculation of total pages immediately
            _originalPaginator.ComputePageCount();
        }

        public override bool IsPageCountValid => _originalPaginator.IsPageCountValid;
        public override int PageCount => _originalPaginator.PageCount;
        public override Size PageSize { get => _originalPaginator.PageSize; set => _originalPaginator.PageSize = value; }
        public override IDocumentPaginatorSource Source => _originalPaginator.Source;

        public override DocumentPage GetPage(int pageNumber)
        {
            // 1. Get the original page content
            DocumentPage originalPage = _originalPaginator.GetPage(pageNumber);

            // 2. Create a visual container to hold the original content + page number
            ContainerVisual visual = new ContainerVisual();

            // Add original content
            visual.Children.Add(originalPage.Visual);

            // 3. Create the Page Number Text
            //DrawingVisual textVisual = new DrawingVisual();
            //using (DrawingContext dc = textVisual.RenderOpen())
            //{
            //    // Format: "Page 1 of 5"
            //    string text = $"Page {pageNumber + 1} of {PageCount}";

            //    FormattedText formattedText = new FormattedText(
            //        text,
            //        CultureInfo.CurrentCulture,
            //        FlowDirection.LeftToRight,
            //        _typeface,
            //        _fontSize,
            //        Brushes.Black,
            //        VisualTreeHelper.GetDpi(visual).PixelsPerDip);

            //    // Position: Bottom Right
            //    double x = _pageSize.Width - formattedText.Width - 40; // 40px margin right
            //    double y = _pageSize.Height - 40; // 40px margin bottom

            //    dc.DrawText(formattedText, new Point(x, y));
            //}

            //visual.Children.Add(textVisual);

            // 4. Return the new combined page
            return new DocumentPage(visual, _pageSize, originalPage.BleedBox, originalPage.ContentBox);
        }
    }
}
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

            
            _originalPaginator.ComputePageCount();
        }

        public override bool IsPageCountValid => _originalPaginator.IsPageCountValid;
        public override int PageCount => _originalPaginator.PageCount;
        public override Size PageSize { get => _originalPaginator.PageSize; set => _originalPaginator.PageSize = value; }
        public override IDocumentPaginatorSource Source => _originalPaginator.Source;

        public override DocumentPage GetPage(int pageNumber)
        {
            
            DocumentPage originalPage = _originalPaginator.GetPage(pageNumber);
            ContainerVisual visual = new ContainerVisual();
            visual.Children.Add(originalPage.Visual);

           
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

           
            return new DocumentPage(visual, _pageSize, originalPage.BleedBox, originalPage.ContentBox);
        }
    }
}
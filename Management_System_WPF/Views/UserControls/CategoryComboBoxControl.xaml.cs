using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Management_System_WPF.Views.UserControls
{
    public partial class CategoryComboBoxControl : UserControl
    {
        public CategoryComboBoxControl()
        {
            InitializeComponent();
        }

        // =========================
        // 1. LABEL
        // =========================
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                "Label",
                typeof(string),
                typeof(CategoryComboBoxControl),
                new PropertyMetadata("LABEL"));

        // =========================
        // 2. ITEMS SOURCE
        // =========================
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                "ItemsSource",
                typeof(IEnumerable),
                typeof(CategoryComboBoxControl));

        // =========================
        // 3. SELECTED ITEM
        // =========================
        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                "SelectedItem",
                typeof(object),
                typeof(CategoryComboBoxControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // =========================
        // 4. SELECTED INDEX
        // =========================
        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(
                "SelectedIndex",
                typeof(int),
                typeof(CategoryComboBoxControl),
                new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // =========================
        // 5. EVENT
        // =========================
        public event SelectionChangedEventHandler SelectionChanged;

        private void MainComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }
    }
}

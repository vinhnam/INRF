namespace TaggerWPF
{
    /// <summary>
    ///     Interaction logic for TaggerWindow.xaml
    /// </summary>
    public partial class TaggerWindow
    {
        public TaggerWindow()
        {
            InitializeComponent();
            DataContext = new TaggerViewModel(this);
        }
    }
}
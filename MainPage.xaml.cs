using MirroRehab.ViewModels;

namespace MirroRehab
{
    public partial class MainPage : ContentPage
    {
       
        public MainPage(MainPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
          
        }
    }

}

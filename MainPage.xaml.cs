using MirroRehab.ViewModels;
using MirroRehab.Services;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace MirroRehab
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageViewModel viewModel)
        {
            Debug.WriteLine("[MainPage] Инициализация MainPage начата");
            InitializeComponent();
            BindingContext = viewModel;
            Debug.WriteLine("[MainPage] Инициализация MainPage завершена");
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
          
        }
    }

}

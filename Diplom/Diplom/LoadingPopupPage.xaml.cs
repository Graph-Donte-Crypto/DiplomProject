using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Diplom
{

    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LoadingPopupPage : Rg.Plugins.Popup.Pages.PopupPage
	{
		public LoadingPopupPage()
		{
			InitializeComponent();
		}

		public LoadingPopupPage(string title)
		{
			InitializeComponent();
			SetTitle(title);
		}
		public void SetTitle(string title) => LabelTitle.Text = title;

		public Action OnUnfocusClicked { get; set; } = null;

		protected override bool OnBackButtonPressed() {
			OnUnfocusClicked?.Invoke();
			return base.OnBackButtonPressed();
		}

		protected override bool OnBackgroundClicked() {
			OnUnfocusClicked?.Invoke();
			return base.OnBackgroundClicked();
		}

	}
}
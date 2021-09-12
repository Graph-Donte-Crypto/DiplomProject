using GoogleSheetsParser;
using Rg.Plugins.Popup.Extensions;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Diplom {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingsPage : ContentPage {
		MainPage main;

		Entry entryApikey = null;
		Button buttonApplySettings = null;
		Button buttonTestSettings = null;
		Entry entryTableId = null;

		public void SetTextValuesFromDataSource(DataSource source) {
			if (entryApikey == null) {
				entryApikey = new Entry {
					HorizontalOptions = LayoutOptions.CenterAndExpand,
					Text = source.ApiKey,
				};
				entryApikey.TextChanged += EntryApikey_Changed;
			} else {
				entryApikey.Text = source.ApiKey;
			}

			if (entryTableId == null) {
				entryTableId = new Entry {
					HorizontalOptions = LayoutOptions.CenterAndExpand,
					Text = source.TableID,
				};
				entryTableId.TextChanged += EntryApikey_Changed;
			} else {
				entryTableId.Text = source.TableID;
			}

			if (buttonApplySettings == null) {
				buttonApplySettings = new Button {
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.End,
					Text = "Применить",
					IsEnabled = false
				};
				buttonApplySettings.Clicked += ButtonApplySettings_Clicked;
			} else {
				buttonApplySettings.IsEnabled = false;
			}
		}
		private StackLayout CreateBoolSwitch(string settingName, string title) {
			Switch @switch = new Switch {
				IsToggled = Settings.Get<bool>(settingName),
				HorizontalOptions = LayoutOptions.Start,
				VerticalOptions = LayoutOptions.Center
			};
			@switch.Toggled += (object sender, ToggledEventArgs e) => {
				Settings.Set(settingName, e.Value);
			};
			return new StackLayout {
				Orientation = StackOrientation.Horizontal,
				Children = {
					new Label {
						Text = title,
						VerticalOptions = LayoutOptions.Center
					},
					@switch
				}
			};
		}

		public SettingsPage(MainPage main) {
			InitializeComponent();
			this.main = main;

			SetTextValuesFromDataSource(main.source);

			Label label_apikey = new Label {
				Text = "Apikey",
				HorizontalOptions = LayoutOptions.CenterAndExpand
			};

			Label label_tableid = new Label {
				Text = "Table ID",
				HorizontalOptions = LayoutOptions.CenterAndExpand
			};

			buttonTestSettings = new Button {
				HorizontalOptions = LayoutOptions.Center,
				Text = "Протестировать"
			};

			buttonTestSettings.Clicked += ButtonTestSettings_Clicked;
			Title = "Настройки";
			Content = new ScrollView {
				Content = new StackLayout {
					Children = {
						new Frame {
							Content = new StackLayout {
								Children = { 
									new Label {
										FontSize = 18,
										Text = "Настройки источника данных",
										VerticalOptions = LayoutOptions.Center,
										HorizontalOptions = LayoutOptions.Start
									},
									label_apikey, entryApikey, label_tableid, entryTableId, buttonTestSettings, buttonApplySettings 
								}
							}
						},
						new Frame {
							Content = new StackLayout {
								Children = {
									new Label {
										FontSize = 18,
										Text = "Прочие настройки",
										VerticalOptions = LayoutOptions.Center,
										HorizontalOptions = LayoutOptions.Start
									},
									CreateBoolSwitch("switchHideEmptyDays", "Скрывать пустые дни в рассписании"),
									CreateBoolSwitch("switchUseApplyAsDownloadTables", "Сохранить список таблиц при первом их скачивании (может понадобится перезагрузить приложение)")
								},
							},
							VerticalOptions = LayoutOptions.FillAndExpand
						}
					}
				}
			};
		}

		private void SwitchHideEmptyDays_Toggled(object sender, ToggledEventArgs e) {
		}

		private DataSource TestSettings() {
			DataSource testSource = new DataSource {
				ApiKey = entryApikey.Text,
				TableID = entryTableId.Text
			};
			testSource.LoadSheets();
			return testSource;
		}

		private async void ButtonTestSettings_Clicked(object sender, EventArgs args) {
			try {
				await Navigation.PushPopupAsync(new LoadingPopupPage("Попытка получить данные..."));
				_ = TestSettings();
				await Navigation.PopPopupAsync();
				await DisplayAlert("Успешно", "Данные успешно получены", "Ок");
			} catch (Exception e) {
				await Navigation.PopPopupAsync();
				main.HandleException("Данные получить не удалось", e);
			}
		}

		private void EntryApikey_Changed(object sender, TextChangedEventArgs e) {
			buttonApplySettings.IsEnabled =
				entryApikey.Text != main.source.ApiKey ||
				entryTableId.Text != main.source.TableID;
		}

		private void UpdateDataSource(DataSource source) {
			main.source = source;
			main.SaveDataSource(main.source);
			buttonApplySettings.IsEnabled = false;
			main.LoadSettings();
			main.pickerTable.ItemsSource = new List<string>();
			main.pickerTeacher.ItemsSource = new List<string>();
		}
		private async void ButtonApplySettings_Clicked(object sender, EventArgs e) {
			await Navigation.PushPopupAsync(new LoadingPopupPage("Попытка получить данные..."));
			DataSource test = new DataSource {
				ApiKey = entryApikey.Text,
				TableID = entryTableId.Text
			};
			if (Settings.Get<bool>("switchUseApplyAsDownloadTables"))
				Settings.Set<DataSource>("DataSourceLoadedAsTablesList", null);
			try {
				test.LoadSheets();
				UpdateDataSource(test);
				await Navigation.PopPopupAsync();
				await DisplayAlert("Успешно", "Новые настройки установлены", "Ок");
			} catch {
				await Navigation.PopPopupAsync();
				bool answer = await DisplayAlert("Ошибка", "При получении данных произошла ошибка. Вы всё равно хотите установить новые значения?", "Да", "Нет");
				if (answer) {
					UpdateDataSource(test);
				}
			}
		}
	}
}
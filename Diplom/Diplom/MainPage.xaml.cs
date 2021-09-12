using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using GoogleSheetsParser;
using Rg.Plugins.Popup.Extensions;
using System.Threading;

namespace Diplom
{

	public class ScheduleData {
		public TableParser TableParser;
		public List<Classes> Classes;
		public List<string> Lessons;
		public List<string> Teachers;
		public List<string> Places;

		public static ScheduleData FromTableParser(TableParser tableParser) {

			var classes = tableParser.GetClasses();

			var data = new ScheduleData {
				TableParser = tableParser,
				Classes     = classes,
				Lessons     = classes.Select(x => x.classesName).Distinct().ToList(),
				Teachers    = classes.Select(x => x.classesTeacher).Distinct().ToList(),
				Places      = classes.Select(x => x.classesPlace).Distinct().ToList()
			};

			data.Lessons.Sort();
			data.Teachers.Sort();
			data.Places.Sort();

			return data;
		}
	}

	public class Settings {
		public static T Get<T>(string name) {
			return Application.Current.Properties.TryGetValue(name, out object readed_value) ? JsonConvert.DeserializeObject<T>(readed_value.ToString()) : default;
		}
		public static void Set<T>(string name, T value) {
			Application.Current.Properties[name] = JsonConvert.SerializeObject(value);
		}
	}
	public partial class MainPage : ContentPage
	{
		public DataSource source;
		public static string DEFAULT_APIKEY = @"AIzaSyATiizTDm6ihCc8cwEY1kZaycUpKvM6sEs";
		public static string DEFAULT_TABLE_ID = @"1V_pEYllNh1ByuQAqf5fuWRXKiZ8LGdl4Uu6HpAQkJRE";
		public Task loadSourceTask = null;
		public void UpdateApiKey(string apikey)
		{
			source.ApiKey = apikey;
			Application.Current.Properties["apikey"] = apikey;
		}
		public void UpdateTableID(string talbeId) {
			source.TableID = talbeId;
			Application.Current.Properties["table_id"] = talbeId;
		}
		public void SaveDataSource(DataSource source)
		{
			Application.Current.Properties["apikey"] = source.ApiKey;
			Application.Current.Properties["table_id"] = source.TableID;
		}
		public DataSource LoadDataSource()
		{
			var source = new DataSource();

			object readed_value;
			bool success;

			if (success = Application.Current.Properties.TryGetValue("table_id", out readed_value))
				source.TableID = readed_value.ToString();
			if (!success || string.IsNullOrEmpty(source.TableID))
				source.TableID = DEFAULT_TABLE_ID;

			if (success = Application.Current.Properties.TryGetValue("apikey", out readed_value))
				source.ApiKey = readed_value.ToString();
			if (!success || string.IsNullOrEmpty(source.ApiKey))
				source.ApiKey = DEFAULT_APIKEY;

			return source;
		}

		public async void HandleException(string message, Exception e) {
			var errmsg = DisplayAlert("Ошибка", message, "Показать ошибку", "Ок");
			await errmsg;
			if (errmsg.Result)
				await DisplayAlert("Информация", e.ToString(), "Ок");
		}

		public Picker pickerTable;
		DateTime antiDoubleClickForTable = DateTime.Now;
		Button buttonLoadTable;
		public Picker pickerTeacher;
		DateTime antiDoubleClickForTeacher = DateTime.Now;

		Button buttonSetting;
		Button buttonSchedule;

		SheetDataTable CurrentTable;

		ScheduleData scheduleData;
		public async void LoadSettings() {
			loadSourceTask = null;

			if (Settings.Get<bool>("switchUseApplyAsDownloadTables")) {
				source = Settings.Get<DataSource>("DataSourceLoadedAsTablesList");
				if (source != null) {
					loadSourceTask = Task.Run(() => {
						pickerTable.ItemsSource = source.sheets.Select(x => x.title).ToList();
					});
					await loadSourceTask;
					return;
				}
			}
			source = LoadDataSource();
			try {
				loadSourceTask = Task.Run(() => { source.LoadSheets(); });
				await loadSourceTask;
				pickerTable.ItemsSource = source.sheets.Select(x => x.title).ToList();

				if (Settings.Get<bool>("switchUseApplyAsDownloadTables")) {
					Settings.Set("DataSourceLoadedAsTablesList", source);
				}
			} catch (Exception e) {
				HandleException("Ошибка при загрузке списка таблиц", e);
				loadSourceTask = null;
			}
		}

		public MainPage()
		{
			InitializeComponent();

			pickerTable = new Picker {
				Title = "Выбрать таблицу расписания"
			};
			pickerTable.Focused += PickerTable_Focused;

			pickerTeacher = new Picker {
				Title = "Выбрать преподавателя"
			};
			pickerTeacher.Focused += PickerTeacher_Focused;

			buttonLoadTable = new Button {
				Text = "Загрузить таблицу",
				HorizontalOptions = LayoutOptions.Center
			};
			buttonLoadTable.Clicked += ButtonLoadTable_Cliced;

			buttonSchedule = new Button {
				Text = "Показать расписание",
				HorizontalOptions = LayoutOptions.Center
			};
			buttonSchedule.Clicked += Schedule_Clicked;

			buttonSetting = new Button {
				Text = "Настройки",
				HorizontalOptions = LayoutOptions.Center,
			};
			buttonSetting.Clicked += ButtonSettings_Clicked;

			LoadSettings();
			this.Title = "Расписание преподавателей";
			this.Content = new ScrollView {
				Content = new StackLayout {
					Children = {
						new Frame {
							Content = new StackLayout {
								Children = { pickerTable, buttonLoadTable },
								Margin = new Thickness(0, 5)
							}
						},
						new Frame {
							Content = new StackLayout {
								Children = { pickerTeacher, buttonSchedule },
								Margin = new Thickness(0, 5)
							}
						},
						new Frame {
							Content = new StackLayout {
								Children = { buttonSetting }
							},
							VerticalOptions = LayoutOptions.FillAndExpand
						},
					}
				}
			};
		}
		
		private async void ButtonLoadTable_Cliced(object sender, EventArgs _) {

			//Должна быть выбрана таблица
			if (pickerTable.SelectedItem == null) {
				await DisplayAlert("Ошибка", "Чтобы загрузить таблицу нужно сначала выбрать таблицу", "Ок");
				pickerTable.Focus();
				return;
			}
			else {
				LoadingPopupPage popup = new LoadingPopupPage("Загрузка таблицы...");
				await Navigation.PushPopupAsync(popup);

				try {
					CurrentTable = source.GetSheetValues(source.sheets[pickerTable.SelectedIndex]);
				}
				catch (Exception e) {
					await Navigation.PopPopupAsync();
					HandleException("Произошла ошибка при загрузке таблицы", e);
					return;
				}
				scheduleData = null;

				SheetDataTable.AutoGetResult result;

				popup.SetTitle("Определение границ таблицы...");

				try {
					result = CurrentTable.AutoGetStartCell();
				}
				catch (Exception e) {
					await Navigation.PopPopupAsync();
					HandleException("Произошла ошибка при попытке получить стартовую клетку таблицы", e);
					return;
				}
				SheetCell cell;

				if (result.success) {
					cell = result.cell;
				} else if (result.maybeCells.Count == 0) {
					await Navigation.PopPopupAsync();
					await DisplayAlert("Ошибка", "В выбранной таблице не удалось найти расписание", "Ок");
					return;
				} else if (result.maybeCells.Count == 1) {
					cell = result.maybeCells[0];
				} else {
					//todo
					cell = result.maybeCells[0];
				}

				popup.SetTitle("Обработка данных таблицы...");

				try {
					scheduleData = ScheduleData.FromTableParser(TableParser.Create(CurrentTable, cell));
				}
				catch (Exception e) {
					await Navigation.PopPopupAsync();
					HandleException("Ошибка при получении данных из таблицы", e);
					return;
				}

				pickerTeacher.ItemsSource = scheduleData.Teachers;

				await Navigation.PopPopupAsync();
			}
		}
		private async void PickerTeacher_Focused(object sender, FocusEventArgs e) {
			if (scheduleData != null)
				return;

			pickerTeacher.Unfocus();

			if ((DateTime.Now - antiDoubleClickForTeacher).TotalMilliseconds < 300)
				return;
			else
				antiDoubleClickForTeacher = DateTime.Now;

			await DisplayAlert("Ошибка", "Чтобы выбрать преподавателя нужно сначала загрузить таблицу", "Ок");
		}
		private async void PickerTable_Focused(object sender, FocusEventArgs _) {

			if (loadSourceTask != null && loadSourceTask.IsCompleted)
				return;

			pickerTable.Unfocus();
			//150 for LoadingPopupPage
			if ((DateTime.Now - antiDoubleClickForTable).TotalMilliseconds < 150)
				return;
			else
				antiDoubleClickForTable = DateTime.Now;

			await Navigation.PushPopupAsync(new LoadingPopupPage("Идёт загрузка данных, пожалуйста подождите..."));

			if (loadSourceTask == null) {
				source = LoadDataSource();
				loadSourceTask = Task.Run(() => { source.LoadSheets(); });
			}
			try {
				try {
					await loadSourceTask;
					pickerTable.ItemsSource = source.sheets.Select(x => x.title).ToList();
				} catch (Exception e) {
					await Navigation.PopPopupAsync();
					HandleException("Ошибка при загрузке списка таблиц", e);
					loadSourceTask = null;
					return;
				}
				await Navigation.PopPopupAsync();
				pickerTable.Focus();
			}
			catch (Exception e) {
				HandleException("Произошла неизвестная ошибка", e);
			}
		}
		private async void Schedule_Clicked(object sender, EventArgs e)
		{
			if (scheduleData != null && pickerTeacher.SelectedItem != null)
				await Navigation.PushAsync(new SchedulePage(scheduleData, pickerTeacher.SelectedItem.ToString()));
			else {
				await DisplayAlert("Ошибка", "Для того, чтобы показать расписание, нужно выбрать преподавателя", "Ок");
			}
		}
		SettingsPage settingsPage = null;
		private void ButtonSettings_Clicked(object sender, EventArgs e)
		{
			if (settingsPage == null)
			{
				settingsPage = new SettingsPage(this);
				Navigation.PushAsync(settingsPage);
			}
			else
			{
				settingsPage.SetTextValuesFromDataSource(source);
				Navigation.PushAsync(settingsPage);
			}
		}
	}
}

using GoogleSheetsParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Diplom {
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SchedulePage : CarouselPage {

		ScheduleData scheduleData;
		string teacher;
		public SchedulePage(ScheduleData scheduleData, string teacher) {
			InitializeComponent();
			this.scheduleData = scheduleData;
			this.teacher = teacher;

			var template = new DataTemplate(() => {
				Label labelName = new Label {
					FontSize = 18,
					FontAttributes = FontAttributes.Bold
				};
				labelName.SetBinding(Label.TextProperty, "Name");

				Label labelTime = new Label();
				labelTime.SetBinding(Label.TextProperty, "Time");

				Label labelGroup = new Label();
				labelGroup.SetBinding(Label.TextProperty, "Group");

				Label labelPlace = new Label();
				labelPlace.SetBinding(Label.TextProperty, "Place");

				return new ViewCell {
					View = new Frame {
						Content = new StackLayout {
							Orientation = StackOrientation.Vertical,
							VerticalOptions = LayoutOptions.Center,
							Children = {
								labelName,
								labelTime,
								labelGroup,
								labelPlace
							}
						}
					}
				};
			});

			//Все пары преподавателя
			var items = scheduleData.Classes
				.Where(x => x.classesTeacher != null && x.classesTeacher.Contains(teacher))
				.ToArray();

			foreach ((Offset offset, int i) pair in scheduleData.TableParser.dayNames.Select((offset, i) => (offset, i))) {
				//Все пары за день
				var currentItems = items
					.Where(x => x.day_num == pair.i)
					.OrderBy(x => x.time.offset)
					.Select(x => new {
						Name = x.classesName,
						Group = x.grp.name,
						Time = x.time.name,
						Place = x.classesPlace
					})
					.ToArray();

				if (Settings.Get<bool>("switchHideEmptyDays") && currentItems.Length == 0)
					continue;

				//День
				//
				//Предмет
				//Группа
				//Время
				//Место

				var page = new ContentPage {
					Title = pair.offset.name,
					Content = new StackLayout {
						Children = {
							new Label {
								Text = pair.offset.name,
								FontSize = 24,
								HorizontalOptions = LayoutOptions.Center
							},
							new ListView {
								ItemTemplate = template,
								ItemsSource = currentItems,
								HasUnevenRows = true
							}
						}
					}
				};
				Title = "Расписание для " + teacher;
				Children.Add(page);
			}

			if (Children.Count == 0) {
				Children.Add(new ContentPage {
					Content = new StackLayout {
						Children = {
							new Label {
								Text = "Ни одно занятие не было найдено",
								FontSize = 24,
								HorizontalOptions = LayoutOptions.Center
							}
						}
					}
				});
			}
		}
	}
}
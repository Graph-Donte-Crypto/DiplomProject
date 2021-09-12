using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace GoogleSheetsParser {
	public struct Offset {

		public int offset;
		public int size;
		public string name;
		public Offset(string name, int offset) {
			size = 1;
			this.offset = offset;
			this.name = name;
		}
		public override string ToString() {
			return $"Name: [{name}], Offset: [{offset}], Size: [{size}]";
		}
		public bool IsKeep(int position) => offset <= position && position < offset + size;
	}
	public class Classes {
		public Offset grp;
		public Offset time;
		public Offset day;
		public int day_num;

		public string classesName;
		public string classesPlace;
		public string classesTeacher;

		public static Classes FromOffsets(Offset grp, Offset time, Offset day, int day_num) {
			return new Classes {
				grp = grp,
				day = day,
				time = time,
				day_num = day_num
			};
		}
	}
	public class TableParser {
		public SheetDataTable sheet;
		public SheetCell startCell;
		public List<Offset> grpNames;//horizontal
		public List<Offset> timeNames;//vertical
		public List<Offset> dayNames;//vertical

		public static List<Offset> CreateHorizontalOffsets(int start_column, int row, SheetDataTable sheet) {
			List<Offset> list = new List<Offset>();

			Offset temp_offset = new Offset(
				offset: start_column,
				name: sheet.GetValue(start_column, row)
			);
			int? max_delta = 1;
			for (int column = start_column + 1; column < sheet.ColumnsCount; column++) {
				string value = sheet.GetValue(column, row);
				if (string.IsNullOrWhiteSpace(value)) {
					temp_offset.size += 1;
					if (max_delta.HasValue && temp_offset.size > max_delta * 2) {
						temp_offset.size = max_delta.Value;
						break;
					}
				} else {
					max_delta = max_delta.HasValue
						? Math.Max(max_delta.Value, temp_offset.size)
						: temp_offset.size;
					list.Add(temp_offset);
					temp_offset = new Offset(sheet.GetValue(column, row), column);
				}
			}
			temp_offset.size = Math.Min(max_delta.Value, temp_offset.size);
			list.Add(temp_offset);

			return list;
		}
		public static List<Offset> CreateVerticalOffsets(int start_row, int column, SheetDataTable sheet) {
			List<Offset> list = new List<Offset>();
			Offset temp_offset = new Offset(
				offset: start_row,
				name: sheet.GetValue(column, start_row)
			);

			int? max_delta = null;

			for (int row = start_row + 1; row < sheet.RowsCount; row++) {
				string value = sheet.GetValue(column, row);
				if (string.IsNullOrWhiteSpace(value)) {
					temp_offset.size += 1;
					if (max_delta.HasValue && temp_offset.size > max_delta * 2) {
						temp_offset.size = max_delta.Value;
						break;
					}
				} else {
					max_delta = max_delta.HasValue
						? Math.Max(max_delta.Value, temp_offset.size)
						: temp_offset.size;
					list.Add(temp_offset);
					temp_offset = new Offset(sheet.GetValue(column, row), row);
				}
			}
			temp_offset.size = Math.Min(max_delta.Value, temp_offset.size);
			list.Add(temp_offset);

			return list;
		}


		public static TableParser Create(SheetDataTable sheet, SheetCell startCell) {
			//Стартовая клетка это такая клетка, что сверху от неё - название группы, а слева - время пары
			TableParser parser = new TableParser {
				sheet = sheet,
				startCell = startCell,
				grpNames = CreateHorizontalOffsets(startCell.column, startCell.row - 1, sheet),
				timeNames = CreateVerticalOffsets(startCell.row, startCell.column - 1, sheet),
				dayNames = CreateVerticalOffsets(startCell.row, startCell.column - 2, sheet)
			};
			if (SheetDataTable.IsTime(parser.timeNames.Last().name) == SheetDataTable.ThreeState.False)
				if (parser.timeNames.Count > 0)
					parser.timeNames.RemoveAt(parser.timeNames.Count - 1);
			return parser;
		}
		private (Offset day, int num) GetDayOffsetByTimeOffset(Offset time) =>
			dayNames.Select((day, num) => (day, num))
				.Where(x => x.day.IsKeep(time.offset))
				.First();

		/// <summary>
		/// Парсит конкретную ячейку расписания и выдаёт все пары, которые смогла найти
		/// </summary>
		/// <param name="time">Вертикальный Offset, описывающий время пары</param>
		/// <param name="grp">Горизонтальный Offset, описывающий группу пары</param>
		/// <returns>Список Classes для данной ячейки</returns>
		private List<Classes> ProcessRange(Offset time, Offset grp) {
			(Offset day, int day_num) = GetDayOffsetByTimeOffset(time);

			Classes tempClasses = Classes.FromOffsets(grp, time, day, day_num);
			List<Classes> classes = new List<Classes>();

			for (int column = grp.offset; column < grp.offset + grp.size; column++) {
				bool findName = false;
				bool findPlace = false;
				int delta = 0;
				for (int height = time.offset; height < time.offset + time.size; height += 1) {
					string cell = sheet.GetValue(column, height);
					if (!findName) {
						if (!string.IsNullOrWhiteSpace(cell)) {
							findName = true;
							tempClasses.classesName = cell;
							delta = height;
						}
					}
					else if (!findPlace) {
						if (!string.IsNullOrWhiteSpace(cell)) {
							findPlace = true;
							tempClasses.classesPlace = cell;
							delta = height - delta;
						}
					}
					else {
						if (!string.IsNullOrWhiteSpace(cell)) {
							findPlace = false;
							findName = false;
							string[] teachers;
							Classes newTempClasses = Classes.FromOffsets(grp, time, day, day_num);
							//Обработка случая, когда в одной паре указано только название и учителя
							//И при этом была найдена следующая пара
							if (sheet.GetValue(column, height - delta) != tempClasses.classesPlace) {
								teachers = tempClasses.classesTeacher.Split(new[] { ',', ';' }).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
								tempClasses.classesPlace = "";
								newTempClasses.classesName = cell;
								findName = true;
							}
							else {
								teachers = cell.Split(new[] { ',', ';' }).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
							}
							
							foreach (var teacher in teachers) {
								tempClasses.classesTeacher = teacher;
								classes.Add(tempClasses);
							}
							tempClasses = newTempClasses;
						}
					}
				}

				if (findName) {
					classes.Add(tempClasses);
					tempClasses = Classes.FromOffsets(grp, time, day, day_num);
				}
			}
			return classes;

		}
		public List<Classes> GetClasses() {
			return timeNames
				.SelectMany(time => grpNames.Select(grp => ProcessRange(time, grp)))//get cartesian product
				.SelectMany(x => x).ToList();//list of lists to list
		}
	}
}

using GoogleSheetsParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleSheetsParserTesting {
	class Program {
		static void Main(string[] args) {
			DataSource source = new DataSource {
				ApiKey = @"AIzaSyATiizTDm6ihCc8cwEY1kZaycUpKvM6sEs",
				TableID = @"1V_pEYllNh1ByuQAqf5fuWRXKiZ8LGdl4Uu6HpAQkJRE"
			};

			source.LoadSheets();

			foreach (var sheet in source.sheets) {
				if (sheet.title != "MITP")
					continue;
				Console.WriteLine(sheet.title + " Begins");
				var table = source.GetSheetValues(sheet);

				var result = table.AutoGetStartCell();

				SheetCell cell;

				if (result.success) {
					cell = result.cell;
				} else {
					if (result.maybeCells.Count == 0) {
						Console.WriteLine(sheet.title + " Has no begin cells");
						continue;
					}
					else {
						result.maybeCells
							.Select((c, i) => (c, i))
							.ToList()
							.ForEach(x => {
								SheetCell c = x.c;
								string grp = table.GetValue(c.column, c.row - 1);
								string time = table.GetValue(c.column - 1, c.row);
								string day = table.GetValue(c.column - 2, c.row);
								Console.WriteLine(x.i + $": grp:{grp}, time:{time}, day:{day}");
							});
						continue;
					}
				}

				var parser = TableParser.Create(table, cell);
				var classes = parser.GetClasses();

				var lessons = classes.Select(x => x.classesName).Distinct().ToList();
				var teachers = classes.Select(x => x.classesTeacher).Distinct().ToList();
				var places = classes.Select(x => x.classesPlace).Distinct().ToList();
			}
		}
	}
}

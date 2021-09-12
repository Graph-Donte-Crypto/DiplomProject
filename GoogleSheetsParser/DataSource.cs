using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace GoogleSheetsParser {
    public struct SheetCell {
        public int row;
        public int column;
        public SheetCell(int column, int row) {
            this.row = row;
            this.column = column;
        }
    }
    public class SheetDataTable {
        public string range { get; set; }
        public string majorDimension { get; set; }
        public List<List<string>> values { get; set; }

        public int RowsCount { get { return values.Count; } }

        private int _columnsCount = 0;
        public int ColumnsCount {
            get {
                if (_columnsCount != 0)
                    return _columnsCount;
                values.ForEach(x => _columnsCount = Math.Max(_columnsCount, x.Count));
                return _columnsCount;
            }
        }
        public string GetValue(int column, int row) {
            var getted_row = values.ElementAtOrDefault(row);
            if (getted_row == null)
                return "";
            var getted_val = getted_row.ElementAtOrDefault(column);
            if (getted_val == null)
                return "";
            return getted_val;
        }
        public enum ThreeState {
            True,
            False,
            Maybe
        }

        private static readonly Regex IsGrpRegex = new Regex(@"^[А-ЯA-Z]{1,5}[^А-Яа-яa-zA-Z0-9]?\d{1,4}");
        private static readonly Regex IsTimeRegex = new Regex(@"^\d");
        public static ThreeState IsGrp(string value) {
            if (string.IsNullOrWhiteSpace(value))
                return ThreeState.False;
            else
                return IsGrpRegex.Match(value.Trim()).Success ? ThreeState.True : ThreeState.Maybe;
        }

        public static ThreeState IsTime(string value) {
            if (string.IsNullOrWhiteSpace(value))
                return ThreeState.False;
            else {
                value = value.Trim();
                if (value.Length > 0)
                    return ThreeState.False;
                return IsTimeRegex.Match(value.Trim()).Success ? ThreeState.True : ThreeState.Maybe;
            }
        }

        public static ThreeState IsDay(string value) {
            if (string.IsNullOrWhiteSpace(value))
                return ThreeState.False;
            else {
                List<string> TrueValues = new List<string>{
                    "Понедельник",
                    "Вторник",
                    "Среда",
                    "Четверг",
                    "Пятница",
                    "Суббота"
                };

                if (TrueValues.Contains(value.Trim()))
                    return ThreeState.True;
                else
                    return ThreeState.Maybe;
            }
        }

        public class AutoGetResult {
            public bool success;
            public SheetCell cell;
            public List<SheetCell> maybeCells;
            public AutoGetResult SetCell(int column, int row) {
                cell = new SheetCell(column, row);
                return this;
            }
        }
        public AutoGetResult AutoGetStartCell() {
            AutoGetResult autoGetResult = new AutoGetResult {
                maybeCells = new List<SheetCell>(),
                success = true
            };

            for (int cur_row = 0; cur_row < 10; cur_row++) {
                for (int cur_column = 0; cur_column < 10; cur_column++) {
                    string value = GetValue(cur_column + 2, cur_row);
                    switch (IsGrp(value)) {
                        case ThreeState.True:
                            return autoGetResult.SetCell(cur_column + 2, cur_row + 1);
                        case ThreeState.False:
                            continue;
                        case ThreeState.Maybe:
                            break;
                    }

                    value = GetValue(cur_column + 1, cur_row + 1);
                    switch (IsTime(value)) {
                        case ThreeState.True:
                            return autoGetResult.SetCell(cur_column + 2, cur_row + 1);
                        case ThreeState.False:
                            continue;
                        case ThreeState.Maybe:
                            break;
                    }

                    value = GetValue(cur_column, cur_row + 1);
                    switch (IsDay(value)) {
                        case ThreeState.True:
                            return autoGetResult.SetCell(cur_column + 2, cur_row + 1);
                        case ThreeState.False:
                            continue;
                        case ThreeState.Maybe:
                            break;
                    }
                    autoGetResult.maybeCells.Add(new SheetCell(cur_column + 2, cur_row + 1));
                    //if isGrp, isDate and isDay is Maybe - continue searching
                }
            }
            autoGetResult.success = false;
            return autoGetResult;
        }
    }
    public class SheetGridProperties {
        public int rowCount { get; set; }
        public int columnCount { get; set; }
    }

    public class SheetProperties {
        public int sheetId { get; set; }
        public string title { get; set; }
        public int index { get; set; }
        public SheetGridProperties gridProperties { get; set; }
    }
    public class RequestHandlerForProperties {
        public SheetProperties properties { get; set; }
    }
    public class RequestHandlerForList {
        public List<RequestHandlerForProperties> sheets { get; set; }
    }
    public class DataSource {
        public static string GOOGLE_SHEETS = @"https://sheets.googleapis.com/v4/spreadsheets/";
        public static string PROPERTIES_REQUEST = @"fields=sheets.properties";
        public static string VALUES_REQUEST = @"/values/";

        public string TableID = @"1V_pEYllNh1ByuQAqf5fuWRXKiZ8LGdl4Uu6HpAQkJRE";
        public string ApiKey = @"AIzaSyATiizTDm6ihCc8cwEY1kZaycUpKvM6sEs";

        public List<SheetProperties> sheets = new List<SheetProperties>();

        public string Get(string uri) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }

        public void LoadSheets() {
            string request = GOOGLE_SHEETS + TableID + "?" + "key=" + ApiKey + "&" + PROPERTIES_REQUEST;

            string response = Get(request);

            var requestHandler = JsonConvert.DeserializeObject<RequestHandlerForList>(response);

            foreach (var handler in requestHandler.sheets) {
                sheets.Add(handler.properties);
            }
        }

        public SheetDataTable GetSheetValues(SheetProperties sheet, string startCell = "A1") {
            string request = GOOGLE_SHEETS + TableID + VALUES_REQUEST + Uri.EscapeDataString(sheet.title) + "!" + startCell + ":" + sheet.gridProperties.rowCount + "?" + "key=" + ApiKey;
            string response = Get(request);
            return JsonConvert.DeserializeObject<SheetDataTable>(response);
        }


    }
}

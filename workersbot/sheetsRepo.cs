using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using kpworkersbot;
using System.Configuration;

namespace kpworkersbotsql
{
    internal static class sheetsRepo
    {
        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private static readonly string SpreadsheetId = ConfigurationManager.AppSettings.Get("sheetsID");
        private const string GoogleCredentialsFileName = "jsconfig1.json";
        private static SpreadsheetsResource.ValuesResource valuesResource = GetSheetsService().Spreadsheets.Values;
        private static SheetsService GetSheetsService()
        {
            using (var stream =
                new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read))
            {
                var serviceInitializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
                };
                return new SheetsService(serviceInitializer);
            }
        }
        public static async Task<List<string>> ReadProjectsAsync()
        {
            var projects = new List<string>();
            var response = await valuesResource.Get(SpreadsheetId, "Проекты!A:A").ExecuteAsync();
            var values = response.Values;

            if (values == null || !values.Any())
            {
                Console.WriteLine("No data found.");
                projects.Add("Проект без названия");
                return null;
            }
            projects.Clear();
            foreach (var row in values.Skip(1))
            {
                var res = string.Join(" ", row.Select(r => r.ToString()));

                projects.Add(res);
            }
            return projects;
        }
        public static async Task<Dictionary<string, int>> ReadPricePerHourAsync()
        {
            var pricePerHour = new Dictionary<string, int>();
            var response = await valuesResource.Get(SpreadsheetId, "Зарплата!A:C").ExecuteAsync();
            var values = response.Values;
            int price;

            if (values == null || !values.Any())
            {
                Console.WriteLine("No data found.");

                return null;
            }

            foreach (var row in values.Skip(1))
            {

                if (int.TryParse(row[2].ToString(), out price))

                    pricePerHour[row[0].ToString()] = price;
                else
                {
                    pricePerHour[row[0].ToString()] = 222;
                    Console.WriteLine("Не удалось преобразовать цену");
                }
            }
            return pricePerHour;

        }
        public static async Task WriteAsync(WorkRezult workRez)
        {
            var response = await valuesResource.Get(SpreadsheetId, "A:H").ExecuteAsync();
            if (response.Values == null || !response.Values.Any())
            {
                Console.WriteLine("No data found.");
                return;
            }
            int wr = response.Values.Count() + 1;
            var valueRange = new ValueRange { Values = new List<IList<object>> { new List<object> { workRez.ID, workRez.name, workRez.project, $"{workRez.tBegin:dd.MM.yy HH:mm}", $"{workRez.tEnd:dd.MM.yy HH:mm}", workRez.timeOfWork, workRez.pricePerHour, $"{workRez.salary:f1}" } } };

            var update = valuesResource.Update(valueRange, SpreadsheetId, $"A{wr}:H{wr}");
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            await update.ExecuteAsync();

        }
        public static async Task WriteUserAsync(string[] userInfo)
        {
            var response = await valuesResource.Get(SpreadsheetId, "Зарплата!A:F").ExecuteAsync();
            if (response.Values == null || !response.Values.Any())
            {
                Console.WriteLine("No data found.");
                return;
            }

            int wr = response.Values.Count() + 1;

            var valueRange = new ValueRange { Values = new List<IList<object>> { new List<object> { userInfo[0], userInfo[1], userInfo[2] } } };

            var update = valuesResource.Update(valueRange, SpreadsheetId, $"Зарплата!A{wr}:F{wr}");
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            await update.ExecuteAsync();

        }
    }
}

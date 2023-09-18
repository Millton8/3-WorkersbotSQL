
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data;
using System.Configuration;
using kpworkersbotsql;




// with PostgreSQL
namespace kpworkersbot
{

    class Program
    {

        
        private static Dictionary<string, UserInfo> timeofWork = new Dictionary<string, UserInfo>();
        private static List<string> userIdentify = new List<string>();
        private static string AdminID = ConfigurationManager.AppSettings.Get("AdminID");
        private static string groupIdForPostEvents = ConfigurationManager.AppSettings.Get("GroupID");

        static ITelegramBotClient _botClient = new TelegramBotClient(ConfigurationManager.AppSettings.Get("ReleaseKey"));
        public static CancellationTokenSource cts = new CancellationTokenSource();

        public static async Task ListenForMessagesAsync()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            var keyboardEnd =
                new KeyboardButton[][]
 {

                new KeyboardButton[]
            {
                new KeyboardButton("Закончить"),

            },

 };
            var keyboardBegin =
                new KeyboardButton[][]
{
            new KeyboardButton[]
            {
                new KeyboardButton("Начать"),

            },

};

            var rmEnd = new ReplyKeyboardMarkup(keyboardEnd);
            var rmBegin = new ReplyKeyboardMarkup(keyboardBegin);
            var rmtest = new ReplyKeyboardMarkup(keyboardBegin);


            if (update.Type == UpdateType.CallbackQuery)
            {

                var workerProject = update.CallbackQuery.Data;
                var callbackQuery = update.CallbackQuery;
                var messagecb = callbackQuery.Message;
                timeofWork[messagecb.Chat.Id.ToString()] = new UserInfo(update.CallbackQuery.Data.ToString());
                
                await botClient.SendTextMessageAsync(messagecb.Chat, $"Хорошей работы\n" +
                       $"Время начала: \n ⏳{DateTime.Now}", replyMarkup: rmEnd);
                await botClient.SendTextMessageAsync(groupIdForPostEvents, $"🟢{messagecb.Chat.FirstName}\n" +
                    $"{messagecb.Chat.Id}\n" +
                    $"приступил к работе\n\n" +
                       $"Объект: {update.CallbackQuery.Data.ToString() ?? "Нет названия"}\n" +
                       $"Время начала: \n {DateTime.Now}", replyMarkup: null);

            }

            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                var messageText = message.Text;
                if (string.IsNullOrWhiteSpace(messageText))
                {
                    return;
                }

                if (messageText.ToLower() == "начать" || messageText == "/begin")
                {
                    var value = new UserInfo();
                    var projects =new List<string>();
                    if (timeofWork.TryGetValue(message.Chat.Id.ToString(), out value))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы уже работаете на объекте. Сперва закончите работу", replyMarkup: rmEnd);

                    }
                    projects =await sheetsRepo.ReadProjectsAsync();

                    var list = new List<List<InlineKeyboardButton>>();

                    for (int i = 0; i < projects.Count; i++)
                        list.Add(new List<InlineKeyboardButton>(projects.Skip(i).Take(1).Select(s => InlineKeyboardButton.WithCallbackData(s))));
                    var inline = new InlineKeyboardMarkup(list);

                    await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите объект", replyMarkup: new ReplyKeyboardRemove());
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Доступные вам :", replyMarkup: inline);

                }

                if (messageText.ToLower() == "закончить" || messageText == "/thend")
                {
                    var pricePerHour = await sheetsRepo.ReadPricePerHourAsync();
                    var value = new UserInfo();
                    if (timeofWork.TryGetValue(message.Chat.Id.ToString(), out value))
                    {
                        var workRezult = new WorkRezult(message.Chat.Id.ToString(), message.Chat.FirstName.ToString(), timeofWork[message.Chat.Id.ToString()], pricePerHour[message.Chat.Id.ToString()]);
                        var tRezult = workRezult.tEnd - workRezult.tBegin;

                        await botClient.SendTextMessageAsync(message.Chat, $"Молодец, {message.Chat.FirstName}. Время завершения работы\n {workRezult.tEnd}\n\n" +
                                $"Всего вы отработали:\n ⌛{tRezult.Hours}:{tRezult.Minutes}:{tRezult.Seconds}\n" +
                                $"Зарплата:\n 💰{tRezult.TotalHours * pricePerHour[message.Chat.Id.ToString()]:f} ₽" +
                                $"\n\nЕсли есть вопросы пишите директору @AndreyOparev", replyMarkup: rmBegin);
                        await sqlRepo.InsertInDBAsync(workRezult);
                        await sheetsRepo.WriteAsync(workRezult);
                        
                        await botClient.SendTextMessageAsync(groupIdForPostEvents, $"🔴{message.Chat.FirstName}\n" +
                            $"{message.Chat.Id}\n" +
                            $"Объект: {value.project}\n" +
                            $"закончил работу в\n" +
                            $"{DateTime.Now}\n\n" +
                            $"Работал: {tRezult.Hours}:{tRezult.Minutes}:{tRezult.Seconds}\n" +
                            $"Зарплата: {tRezult.TotalHours * pricePerHour[message.Chat.Id.ToString()]:f} ₽", replyMarkup: null);
                        await RemoveObject(message.Chat.Id.ToString());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"Вы еще не начали работать. Нажмите кнопку Начать чтобы приступить к работе", replyMarkup: rmBegin);
                    }

                }

                if (messageText.ToLower() == "быстро")
                {
                    string report = null;                   
                    var listRez = await sqlRepo.FastSelectAsync();
                    if (listRez == null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"Произошла ошибка. Расчет не возможен", replyMarkup: null);
                        return;
                    }
                    foreach (var item in listRez)
                    {
                        report += item.Id + "\n" + item.name + "\n" + $"{item.salary:0}" + " р." + "\n\n";
                    }
                    await botClient.SendTextMessageAsync(message.Chat, $"{report}", replyMarkup: null);

                }

                if (userIdentify.Contains(message.Chat.Id.ToString()))
                {
                    var dataForReport = messageText.ToLower();
                    string report = null;
                    bool check = await CheckStringDateAsync(dataForReport);
                    if (check)
                    {
                        var listRez = await sqlRepo.SelectBetweenTwoDatesAsync(dataForReport);
                        if (listRez == null)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"Произошла ошибка. Расчет не возможен", replyMarkup: null);
                            return;
                        }

                        foreach (var item in listRez)
                        {
                            report += item.Id.ToString() + "\n" + item.name.ToString() + "\n" + item.salary.ToString() + " р." + "\n\n";
                        }
                        await botClient.SendTextMessageAsync(message.Chat, $"Отчет по дате {dataForReport}\n\n" +
                            $"{report ?? "Нет данных за этот период"}", replyMarkup: null);
                        userIdentify.Remove(message.Chat.Id.ToString());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"Вы ввели {dataForReport}\n" +
                            $"Дата в неправильном формате. " +
                            $"Начните заново с ввода кодового слова", replyMarkup: null);
                        userIdentify.Remove(message.Chat.Id.ToString());
                    }

                }
                
                if (messageText.ToLower() == "отчет")
                {
                    userIdentify.Add(message.Chat.Id.ToString());

                    await botClient.SendTextMessageAsync(message.Chat, $"Введите дату в формате\n"
                        + $"дд.мм.гг\n" + $"Например 01.01.23\n\n" +
                        $"Если необходимо сделать поиск в диапозоне дат. Пишите две даты через пробел\n" +
                        $"Например\n" +
                        $"01.09.23 05.09.23", replyMarkup: null);

                }
                
                if (messageText == "/start")
                {

                    string[] userInfo = { message.Chat.Id.ToString(), message.Chat.FirstName.ToString(), "222" };
                    await sheetsRepo.WriteUserAsync(userInfo);
                    await botClient.SendTextMessageAsync(message.Chat, $"Привет, {message.Chat.FirstName}. По прибытию на объект нажмите кнопку Начать.\n" +
                        "Как будете уходить с работы жмите Закончить\n" +
                        "Также доступно меню слева от окна ввода", replyMarkup: rmBegin);

                }


            }
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        public static async Task Main()
        {
            await ListenForMessagesAsync();
            Console.ReadLine();

        }
        private static async Task RemoveObject(string userID)
        {
            timeofWork.Remove(userID);
        }
        private static async Task<bool> CheckStringDateAsync(string dateString)
        {

            string[] twoDatesString = new string[2];
            bool isTwoDate = false;


            foreach (var c in dateString)
            {
                if (c == ' ')
                    isTwoDate = true;
            }
            if (isTwoDate)
            {
                twoDatesString = dateString.Split(' ');
                try
                {
                    Convert.ToDateTime(twoDatesString[0]);
                    Convert.ToDateTime(twoDatesString[1]);


                    return true;
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync("Ошибка формата даты\n" + ex);
                    return false;
                }
            }
            else
            {
                try
                {
                    Convert.ToDateTime(dateString);
                    return true;
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync("Ошибка формата даты\n" + ex);
                    return false;
                }


            }


        }
        
    }
}






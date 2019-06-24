using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static TimerBot.Constants;
using static TimerBot.Functions;

namespace TimerBot
{
    /// <summary>
    /// entry point
    /// </summary>
    public static class Startup
    {
        public static void Main()
        {
            var client = new TelegramBotClient(ApiToken);

            client.StartReceiving();

            client.OnMessage += TimerHandler;

            Console.Read();

            client.StopReceiving();
        }
    }

    internal static class Constants
    {
        internal const string ApiToken = "743943650:AAEuST7OZGd33pVO9j5xhpP1Udyor1DcGXE";
        internal const string TimeSpanFormat = @"hh\:mm\:ss";
        internal const string StartCommand = @"/start";
        internal const string StartAnswer = @"Hello! You can set a timer to wait and get a notification as the timer runs out.
Use the following format: " + TimeSpanFormat;
    }

    internal static class Functions
    {
        public static async void TimerHandler(object obj, MessageEventArgs args)
        {
            var text = args.Message?.Text;

            if (!(obj is TelegramBotClient client))
                return;

            if (String.IsNullOrEmpty(text))
                return;

            if (text == StartCommand)
            {
                await client.SendTextMessageIgnoreAPIErrorsAsync(args.GetChatId(), StartAnswer);
                return;
            }

            if (!TimeSpan.TryParseExact(text, TimeSpanFormat, CultureInfo.InvariantCulture, out var timeSpan))
                return;

            var sec = timeSpan.TotalSeconds;

            var infoMsg = await client.SendTextMessageAsync(args.GetChatId(),
                $@"The timer for {timeSpan.ToString(TimeSpanFormat)} is being started...");

            DateTime startTime = DateTime.Now;
            while (true)
            {
                var delayTask = Task.Delay(1000);

                var leftSeconds = (DateTime.Now - startTime).TotalSeconds;

                if (leftSeconds >= sec)
                    break;

                var editTask = 
                    client.EditMessageTextAsync(
                        args.GetChatId(), 
                        infoMsg.MessageId, 
                        $"Timer for {timeSpan.ToString(TimeSpanFormat)} is running! {Environment.NewLine}" + 
                        $"Current remaining time: {timeSpan - new TimeSpan(0, 0, seconds: (int)leftSeconds)}");

                Task.WaitAll(editTask, delayTask);
            }

            var editAfterRunTask = 
                client.EditMessageTextAsync(
                    args.GetChatId(), 
                    infoMsg.MessageId,
                    $"Timer for {timeSpan.ToString(TimeSpanFormat)} has run out!"
                    );

            var sendTask = 
                client.SendTextMessageAsync(
                    args.GetChatId(), 
                    $"@{args.GetSenderUserName()} The timer has ran out!");

            await Task.WhenAll(editAfterRunTask, sendTask);
        }
    }

    #region extensions
    public static class MessageEventArgsExtensions
    {
        public static long GetChatId(this MessageEventArgs args)
            => args.Message.Chat.Id;

        public static string GetSenderUserName(this MessageEventArgs args)
    => args.Message.From.Username;
    }

    public static class TelegramBotClientExtensions
    {
        public static async Task<Message> SendTextMessageIgnoreAPIErrorsAsync(this TelegramBotClient client,
            ChatId chatId,
            string text,
            ParseMode parseMode = ParseMode.Default,
            bool disableWebPagePreview = false,
            bool disableNotification = false,
            int replyToMessageId = 0,
            IReplyMarkup
            replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            Message result = null;
            try
            {
                result = await client.SendTextMessageAsync(chatId,
                    text,
                    parseMode,
                    disableWebPagePreview,
                    disableNotification,
                    replyToMessageId,
                    replyMarkup,
                    cancellationToken);
            }
            catch (Exception e)
            {
                if (!(e is Telegram.Bot.Exceptions.ApiRequestException))
                    throw;
            }

            return result;
        }
    }
    #endregion
}

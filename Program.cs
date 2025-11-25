using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Data.Sqlite;
using PEPCHABUILD.Services;
using PEPCHABUILD.Models;
using Microsoft.VisualBasic;
using Telegram.Bot.Exceptions;
using Microsoft.Extensions.Configuration;
using PEPCHABUILD.MessageSender;



CustomerAdder boban = new();
string connectionString = "Data Source=mydatabase.db";
string connectionStringF = "Data Source=failedcust.db";
boban.DataBaseInit(connectionString);
boban.DataBaseInit(connectionStringF);

var token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(token!, cancellationToken: cts.Token);
var me = await bot.GetMe();
long channelId = -1002955744885;
long[] admins = [308924853];

bot.OnError += OnError;
bot.OnMessage += OnMessage;
bot.OnUpdate += OnUpdate;


Console.WriteLine($"@{me.Username} is running... Press Ctrl+C to terminate");

var customers = boban.GetFirst1000Customers(connectionString);

using var waitHandle = new ManualResetEventSlim(false);

Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("Shutting down...");
    e.Cancel = true;
    waitHandle.Set();
};

// Ждем сигнала завершения
waitHandle.Wait();
cts.Cancel();

async Task OnError(Exception exception, HandleErrorSource source)
{
    Console.WriteLine(exception.Message);
}

async Task OnMessage(Message msg, UpdateType type)
{
    if (msg == null) return;
    if (msg.Chat == null || msg.From == null) return;

    string textMes = (msg.Text ?? string.Empty).Trim();
    long chatId = msg.Chat.Id;
    string userName = msg.From.Username ?? "";
    var c = boban.FindCustomerByChatId(chatId, connectionString);

    if (msg.Chat.Type == ChatType.Private)
    {
        if (IsAdmin(chatId))
        {
            switch (textMes)
            {
                case var s when s.StartsWith("/sendchal"):
                    {
                        var button = new InlineKeyboardMarkup([[InlineKeyboardButton.WithUrl("Взяти участь!", "https://t.me/CandyYarn_Bot?start=group_invite")]]);
                        await bot.SendMessage(channelId, "✨ Вітаю! Я — бот CandyYarn, і я допоможу вам взяти участь у нашій святковій акції 🧶\nНатисніть кнопку нижче — і отримайте свій чарівний номерок 🎫\nА коли настане час, я надішлю магічний каталог з найкращими цінами першій тисячі учасників ✨",
                         replyMarkup: button);

                        break;
                    }
                case var s when s.StartsWith("/sendcatalog"):
                    {
                        MessageSender sender = new();
                        var customers = boban.GetFirst1000Customers(connectionString);
                        string textMess = "Example of link...";
                        
                        _= Task.Run(async () => await sender.SendMultiple(bot, boban, customers, textMess, connectionStringF));
                        
                        break;
                    }
            }
        }
        else
        {
            switch (textMes)
            {
                case var s when s.StartsWith("/start"):
                    {
                        if (c != null)
                        {
                            await bot.SendMessage(msg.Chat, $"🎉 Ви вже берете участь у нашій акції!\nВаш номерок — {c.UserAssignedNumber}🧾\nКоли настане потрібний час — я надішлю вам магічний каталог з найкращими цінами ✨");

                            return;
                        }
                        Console.WriteLine(msg.Chat);
                        var keyboard = new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("🎫 Тиць!", "get_number")]]);
                        await bot.SendMessage(msg.Chat, "🧶 Вітаємо у чарівному світі CandyYarn!\nНатисніть кнопку нижче, щоб отримати свій щасливий номерок 🎫✨", replyMarkup: keyboard);

                        break;
                    }
                default:
                    {
                        if (c != null)
                        {
                            await bot.SendMessage(msg.Chat, $"🎉 Ви вже берете участь у нашій акції!\nВаш номерок — {c.UserAssignedNumber}🧾\nКоли настане потрібний час — я надішлю вам магічний каталог з найкращими цінами ✨");
                            return;
                        }
                        var keyboard = new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("🎫 Тиць!", "get_number")]]);
                        await bot.SendMessage(msg.Chat, $"Натисніть кнопку нижче, щоб отримати свій щасливий номерок 🎫✨", replyMarkup: keyboard);

                        break;
                    }
            }
        }
    }
}

async Task OnUpdate(Update update)
{
    if (update.Type == UpdateType.ChatMember)
    {
        if (update.ChatMember?.NewChatMember.Status == ChatMemberStatus.Member && update.ChatMember.Chat.Id != channelId)
        {
            var newMember = update.ChatMember.NewChatMember.User;
            var chat = update.ChatMember.Chat;

            if (chat.Id == channelId)
            {
                return;
            }

            if (!newMember.IsBot)
            {
                await boban.NewGroupMemberInvit(newMember, chat.Id, bot);
            }
        }
    }

    if (update is { CallbackQuery: { } query })
    {
        if (query.Data == "get_number")
        {
            await bot.AnswerCallbackQuery(query.Id, $"Оброблюю ваш запит… ✨");

            long chatId = query.From.Id;
            string? userName = "@" + query.From.Username;
            int num = 0;
            var c = boban.FindCustomerByChatId(chatId, connectionString);

            if (c == null)
            {
                num = boban.CreateCustomer(userName, chatId, connectionString);
                await bot.SendMessage(query.Message!.Chat, $"🎉 Готово! Ви успішно зареєструвалися в акції!\nВаш номерок - {num}🧾✨\nЯк тільки почнеться розсилка — я одразу надішлю вам магічний каталог з найкращими цінами на улюблені товари для рукоділля 🧶💖");
            }
            else
            {
                await bot.SendMessage(query.Message!.Chat, $"🧵 Ви вже берете участь у нашій акції!\nВаш номерок — {c.UserAssignedNumber}🧾\nЯ повідомлю вам, коли настане час отримати магічний каталог ✨");
            }
        }
    }
}

bool IsAdmin(long chatId)
{
    return admins.Contains(chatId);
}


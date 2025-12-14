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
    string textToSend = "";

    if (msg.Chat.Type == ChatType.Private)
    {
        if (IsAdmin(chatId))
        {
            switch (textMes)
            {
                case var s when s.StartsWith("/sendchal"):
                    {
                        var button = new InlineKeyboardMarkup([[InlineKeyboardButton.WithUrl("Взяти участь!", "https://t.me/CandyYarn_Bot?start=group_invite")]]);
                        textToSend = "✨Привіт!\n👋Я — бот магазину Candy Yarn 🧶\nНатискайте кнопку нижче — і я буду надсилати вам новинки, акції, знижки та розпродажі для в’язання 💛";

                        await bot.SendMessage(channelId, textToSend,replyMarkup: button);

                        break;
                    }
                case var s when s.StartsWith("/sendcatalog"):
                    {
                        MessageSender sender = new();
                        var customers = boban.GetCustomers(connectionString);
                        textToSend ="Усі знижки починають діяти -  28.11 на Чорну П’ятницю🔥\n\n👉Слідкуйте за всіма новинами у нашому Телеграм-каналі:\nhttps://t.me/+dmZPQ3u1k_hkZTdi";

                        _ = Task.Run(async () => await sender.SendMultiple(bot, boban, customers, textToSend, connectionStringF));

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
                            textToSend = $"💛 Ви вже підписані на оновлення Candy Yarn 🧶\n Всі новини від Candy-Yarn будуть приходити вам першими 🧶";
                            await bot.SendMessage(msg.Chat, textToSend);

                            return;
                        }

                        Console.WriteLine(msg.Chat);

                        var keyboard = new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("🧶 Підписатися", "get_number")]]);
                        textToSend = "Привіт! 👋\n\nЯ — бот магазину Candy-Yarn🧶\nНатискайте кнопку нижче, і ви першими отримуватимете всі новини та анонси нашого магазину 💛✨";

                        await bot.SendMessage(msg.Chat, textToSend, replyMarkup: keyboard);

                        break;
                    }
                default:
                    {
                        if (c != null)
                        {
                            textToSend = $"💛 Ви вже з нами!\nВсі новини від Candy-Yarn будуть приходити вам першими 🧶";

                            await bot.SendMessage(msg.Chat, textToSend);
                            return;
                        }

                        var keyboard = new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("🧶 Підписатися!", "get_number")]]);
                        textToSend = $"Натискайте кнопку нижче, щоб отримувати новинки, акції, знижки та розпродажі від Candy Yarn 🧶";
                        await bot.SendMessage(msg.Chat, textToSend, replyMarkup: keyboard);

                        break;
                    }
            }
        }
    }
}

async Task OnUpdate(Update update)
{
    string textToSend = "";

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
                textToSend = $"🎉 Готово! Ви успішно підписалися на новини від Candy-Yarn!\nЯк тільки будуть цікаві новини - ви першими про них дізнаєтесь!🧶💖";
                await bot.SendMessage(query.Message!.Chat, textToSend);
            }
            else
            {
                textToSend = $"💛 Ви вже підписані на оновлення Candy Yarn 🧶\n Всі новини від Candy-Yarn будуть приходити вам першими 🧶";
                await bot.SendMessage(query.Message!.Chat, textToSend);
            }
        }
    }
}

bool IsAdmin(long chatId)
{
    return admins.Contains(chatId);
}


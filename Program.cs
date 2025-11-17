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



CustomerAdder boban = new();
boban.DataBaseInit();
var token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(token, cancellationToken: cts.Token);
var me = await bot.GetMe();
long channelId = -1002955744885;
long[] admins = [308924853, 493034507];

bot.OnError += OnError;
bot.OnMessage += OnMessage;
bot.OnUpdate += OnUpdate;


Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
var customers = boban.GetFirst1000Customers();
Console.ReadLine();
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
    var c = boban.FindCustomerByChatId(chatId);

    if (msg.Chat.Type != ChatType.Private)
    {
        if (textMes.Contains("@CandyYarn_Bot") && userName == "BelAkoRm")
        {
            if (textMes.Contains("bob"))
            {
                int sentCount = 0;
                int failCount = 0;
                string failedCustId = "";

                var customers = boban.GetFirst1000Customers();
                string textMess = "бобан";

                for (int i = 0; i < customers.Count; i++)
                {
                    var cust = customers[i];

                    try
                    {
                        await bot.SendMessage(cust.ChatId, textMess);
                        sentCount++;

                        if (i % 30 == 0 && i > 0)
                        {
                            await Task.Delay(1000);
                        }
                        else
                        {
                            await Task.Delay(150);
                        }
                    }
                    catch (ApiRequestException ex) when (ex.ErrorCode == 429)
                    {
                        failedCustId += $"-{cust.ChatId}";
                        await bot.SendMessage(308924853, "⏳ Превышен лимит, жду 10 секунд...");
                        await Task.Delay(10000);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                        failedCustId += $"-{cust.ChatId}";
                        failCount++;
                        await Task.Delay(100);
                    }
                }

                await bot.SendMessage(308924853, $"Done! \nSuccsess sent: {sentCount}\nFailed sent: {failCount}\nList of Id Failed: {failedCustId}");

                return;
            }

            var button = new InlineKeyboardMarkup([[InlineKeyboardButton.WithUrl("Взяти участь!", "https://t.me/CandyYarn_Bot?start=group_invite")]]);

            await bot.SendMessage(msg.Chat, "✨ Вітаю! Я — бот CandyYarn, і я допоможу вам взяти участь у нашій святковій акції 🧶\nНатисніть кнопку нижче — і отримайте свій чарівний номерок 🎫\nА коли настане час, я надішлю магічний каталог з найкращими цінами першій тисячі учасників ✨",
             replyMarkup: button);
        }

        return;
    }

    if (textMes.Contains("/sendchal") && IsAdmin(chatId))
    {
        var button = new InlineKeyboardMarkup([[InlineKeyboardButton.WithUrl("Взяти участь!", "https://t.me/CandyYarn_Bot?start=group_invite")]]);
        await bot.SendMessage(channelId, "✨ Вітаю! Я — бот CandyYarn, і я допоможу вам взяти участь у нашій святковій акції 🧶\nНатисніть кнопку нижче — і отримайте свій чарівний номерок 🎫\nА коли настане час, я надішлю магічний каталог з найкращими цінами першій тисячі учасників ✨",
         replyMarkup: button);
        return;
    }

    if (c != null)
    {
        await bot.SendMessage(msg.Chat, $"🎉 Ви вже берете участь у нашій акції!\nВаш номерок — {c.UserAssignedNumber}🧾\nКоли настане потрібний час — я надішлю вам магічний каталог з найкращими цінами ✨");
        return;
    }

    if (textMes.Contains("/start") || textMes.Contains("номер"))
    {
        Console.WriteLine(msg.Chat);

        var keyboard = new InlineKeyboardMarkup(
       [
            [
                InlineKeyboardButton.WithCallbackData("🎫 Тиць!", "get_number")
            ]
        ]);

        await bot.SendMessage(msg.Chat, "🧶 Вітаємо у чарівному світі CandyYarn!\nНатисніть кнопку нижче, щоб отримати свій щасливий номерок 🎫✨", replyMarkup: keyboard);
    }
    else
    {
        await bot.SendMessage(msg.Chat, "Щоб отримати свій номерок для участі в акції, просто напишіть: \"Хочу номер\" 🧵💗");
    }
}

async Task OnUpdate(Update update)
{
    if (update.Type == UpdateType.ChatMember)
    {
        if (update.ChatMember?.NewChatMember.Status == ChatMemberStatus.Member)
        {
            var newMember = update.ChatMember.NewChatMember.User;
            var chat = update.ChatMember.Chat;

            Console.WriteLine("goooogaa123");

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
            var c = boban.FindCustomerByChatId(chatId);

            if (c == null)
            {
                num = boban.CreateCustomer(userName, chatId);
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


namespace PEPCHABUILD.MessageSender;

using Telegram.Bot;
using PEPCHABUILD.Models;
using PEPCHABUILD.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;

public class MessageSender
{
    public async Task SendMultiple(TelegramBotClient bot, CustomerAdder boban, List<Customer> customers, string textMess, string connectionStringF)
    {
        Random ran = new();

        int sentCount = 0;
        int failedCount = 0;
        int ex429 = 0;
        int del = 10000;
        int maxdel = 30000;

        for (int i = 0; i < customers.Count; i++)
        {
            var cust = customers[i];
            string? userName = cust.UserName;
            long chatId = cust.ChatId;
            bool failed = false;
            int random = ran.Next(150, 801);

            try
            {
                await bot.SendMessage(cust.ChatId, textMess);
                sentCount++;

                if (i % 150 == 0 && i > 0)
                {
                    await Task.Delay(10000);
                }
                else
                {
                    await Task.Delay(random);
                }
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                failed = true;
                failedCount++;
             
                int? retryAfter = ex.Parameters?.RetryAfter;

                if (retryAfter.HasValue)
                {
                    await Task.Delay(retryAfter.Value * 1000);
                }
                else
                {
                    if(ex429 >= 1 && del != maxdel)
                    {
                        del += 5000;
                    }
                    await Task.Delay(del);
                }

                ex429++;
            }
            catch (Exception exception)
            {
                failed = true;
                failedCount++;
                Console.WriteLine(cust.ToString());
                Console.WriteLine(exception.Message);
                await Task.Delay(10000);
            }

            if (failed)
            {
                boban.CreateCustomer(userName!, chatId, connectionStringF);
            }
        }

        await bot.SendMessage(308924853, $"Done! \nSuccsess sent: {sentCount}\nFailed sent: {failedCount}");
    }
}
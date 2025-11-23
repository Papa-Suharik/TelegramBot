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
        int sentCount = 0;
        int failedCount = 0;
        

        for (int i = 0; i < customers.Count; i++)
        {
            var cust = customers[i];
            string? userName = "@" + cust.UserName;
            long chatId = cust.ChatId;
            bool failed = false;

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
                failed = true;
                failedCount++;
                await Task.Delay(10000);
            }
            catch (Exception exception)
            {
                failed = true;
                failedCount++;
                Console.WriteLine(exception.ToString());
                await Task.Delay(100);
            }

            if (failed)
            {
                boban.CreateCustomer(userName, chatId, connectionStringF);
            }
        }

        await bot.SendMessage(308924853, $"Done! \nSuccsess sent: {sentCount}\nFailed sent: {failedCount}");
    }
}
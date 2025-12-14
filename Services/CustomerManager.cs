namespace PEPCHABUILD.Services;

using Telegram.Bot;
using Microsoft.Data.Sqlite;
using PEPCHABUILD.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class CustomerAdder
{   
    public void DataBaseInit(string connectionString)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var tableCommand = connection.CreateCommand();
            tableCommand.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Customers (
                AssignedNumber INTEGER PRIMARY KEY AUTOINCREMENT,
                UserName TEXT NOT NULL,
                ChatID INTEGER,
                CreatedTime DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            ";
            tableCommand.ExecuteNonQuery();

            Console.WriteLine("База данных инициализирована.");
        }
    }
    public int CreateCustomer(string UserName, long ChatId, string connectionString)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText =
                "INSERT INTO Customers (UserName, ChatId) VALUES ($UserName, $ChatId)";
            insertCommand.Parameters.AddWithValue("$UserName", UserName ?? "");
            insertCommand.Parameters.AddWithValue("$ChatId", ChatId);

            insertCommand.ExecuteNonQuery();
        }

        var c = FindCustomerByChatId(ChatId, connectionString);

        if (c == null)
        {
            Console.WriteLine($"Ошибка: пользователь с ChatId {ChatId} не найден после создания");
            return -1; // или другое значение, указывающее на ошибку
        }
        
        return c.UserAssignedNumber;
    }
    public Customer? FindCustomerByChatId(long chatId, string connectionString)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT AssignedNumber, UserName, ChatId, CreatedTime FROM Customers where ChatId = $ChatId";
            selectCommand.Parameters.AddWithValue("$ChatId", chatId);

            using (var reader = selectCommand.ExecuteReader())
            {
                if (reader.Read())
                {
                    int assignedNumT = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    string userNameT = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    long chatIdT = reader.IsDBNull(2) ? 0L : reader.GetInt64(2);
                    DateTime CreatedTime = reader.GetDateTime(3);

                    return new Customer(userNameT, chatId, assignedNumT, CreatedTime);
                }
            }

        }
        return null;
    }
    public async Task NewGroupMemberInvit(User newUser, long groupChatId, TelegramBotClient bot)
    {
        var wellcomeMessage = $"🌸 Вітаємо вас, @{newUser.FirstName}!\nЩоб взяти участь у нашій святковій акції — напишіть мені у особисті 🧵✨";

        var keyboard = new InlineKeyboardMarkup(
    [
        [
            InlineKeyboardButton.WithUrl(
                "👉 Клацай сюди",
                "https://t.me/CandyYarn_Bot?start=group_invite")
        ]
    ]);

        await bot.SendMessage(groupChatId, wellcomeMessage, replyMarkup: keyboard);
    }

    public List<Customer> GetCustomers(string connectionString)
    {
        var customers = new List<Customer>();

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ChatId, UserName, AssignedNumber, CreatedTime
                FROM Customers
                ORDER BY CreatedTime ASC";
            
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    long ChatId = reader.GetInt64(0);
                    string UserName = reader.GetString(1);
                    int UserAssignedNumber = reader.GetInt32(2);
                    DateTime CreatedTime = reader.GetDateTime(3);

                    Customer currentCust = new(UserName, ChatId, UserAssignedNumber, CreatedTime);
                    customers.Add(currentCust);
                }
            }

            return customers;
        }
    }

    async Task SendToChannel(long channelId, string message, TelegramBotClient bot)
    {
        await bot.SendMessage(channelId, message);
    }
}
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace msngrDAL
{
    //Реализация репозитория для записи и чтения сообщений в/из БД
    public class MessagesRepository : IMessagesRepository
    {
        private string _connectionString;

        public MessagesRepository(
            string connectionString)
        {
            _connectionString = connectionString;
        }

        //Метод проверки существования базы данных 
        public void CheckDBExistence()
        {
            try
            {
                var modifiedConnectionString = Regex.Replace(_connectionString, "Initial.*?;", "Initial catalog=master;");
                using (var connection = new SqlConnection(modifiedConnectionString))
                using (var command = connection.CreateCommand())
                {
                    HelperMethods.WriteInColor(ConsoleColor.Yellow, DateTime.Now.ToString("hh:mm:ss") + $" Проверяем подключение к БД, таймаут {connection.ConnectionTimeout} с.");
                    for (int i = 1; i < 11; i++)
                    {
                        HelperMethods.WriteInColor(ConsoleColor.Yellow, DateTime.Now.ToString("hh:mm:ss") +
                                                    $" Проверка наличия подключения к серверу БД, попытка {i} из 10");
                        try
                        {
                            connection.Open();
                            if (connection.State == System.Data.ConnectionState.Open)
                            {
                                HelperMethods.WriteInColor(ConsoleColor.Yellow, DateTime.Now.ToString("hh:mm:ss") + 
                                                    " Подключились к серверу БД. Проверяем наличие БД...");
                                break;
                            }
                            else connection.Close();
                        }
                        catch (Exception ex)
                        {
                            if (i == 10) throw ex;
                            connection.Close();
                            HelperMethods.WriteInColor(ConsoleColor.Red, DateTime.Now.ToString("hh:mm:ss") + 
                                                    $" {i} попытка не удалась. Ждём 10 секунд...");
                            //Пауза перед повторным подключением
                            Thread.Sleep(10000);
                        }
                    }
                    command.CommandText = $"SELECT * FROM master.dbo.sysdatabases WHERE name = 'MsngrDB'";
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            connection.Close();
                            HelperMethods.WriteInColor(ConsoleColor.Yellow, 
                                        DateTime.Now.ToString("hh:mm:ss") + " " + "База не обнаружена");
                            CreateDatabase(modifiedConnectionString);
                        }
                        else HelperMethods.WriteInColor(ConsoleColor.Yellow, DateTime.Now.ToString("hh:mm:ss") + " " + 
                                                        "База уже существует. Продолжаем работу..."); ;
                    }
                }
            }
            catch (Exception ex)
            {
                HelperMethods.WriteInColor(ConsoleColor.Red, "Ошибка при проверке наличия базы данных: ");
                Console.Write(ex.Message, "\n Стек вызовов: ", ex.StackTrace);
            }
        }

        //Метод создания базы данных при её отсутствии
        private void CreateDatabase(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    Console.WriteLine("Начинаю создание базы данных...");
                    var commandText = new StringBuilder().AppendJoin("",
                                        "CREATE DATABASE MsngrDB GO ",
                                        "USE MsngrDB GO ",
                                        "CREATE TABLE Messages(MessageID INT IDENTITY (1, 1) PRIMARY KEY, ",
                                        "MessageText NVARCHAR(130), MessageTime DATETIME NOT NULL, ",
                                        "OrdinalNo INT NOT NULL)").ToString();
                    foreach (var cmd in commandText.Split("GO"))
                    {
                        command.CommandText = cmd;
                        command.ExecuteNonQuery();
                    }
                    HelperMethods.WriteInColor(ConsoleColor.Green, "База успешно создана!");
                }
            }
            catch (Exception ex)
            {
                HelperMethods.WriteInColor(ConsoleColor.Red, "Ошибка при попытке создания базы данных: ");
                Console.WriteLine(ex.Message, "\n Стек вызовов: ", ex.StackTrace);
            }
        }

        //Запись сообщения в базу данных
        public async Task SendMessage(Message message)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                await connection.OpenAsync();
                var text = message.Text;
                var time = message.DateAndTime.ToUniversalTime();
                var ordinalNo = message.OrdinalNo;
                command.CommandText = "INSERT INTO Messages (MessageText, MessageTime, OrdinalNo)" +
                                        "VALUES (@text, @time, @ordinalNo)";
                command.Parameters.AddWithValue("@text", text);
                command.Parameters.AddWithValue("@time", time);
                command.Parameters.AddWithValue("@ordinalNo", ordinalNo);
                await command.ExecuteNonQueryAsync();
            }
        }

        //Получение истории в диапазоне даты/времени от 'from' до 'to'
        public async Task<IEnumerable<Message>> GetHistoryInRange(DateTime from, DateTime to)
        {
            List<Message> messages = new List<Message>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                await connection.OpenAsync();
                command.CommandText = "SELECT * FROM Messages WHERE MessageTime > @from AND MessageTime <  @to";
                command.Parameters.AddWithValue("@from", from.ToUniversalTime());
                command.Parameters.AddWithValue("@to", to.ToUniversalTime());
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var text = reader.GetString(reader.GetOrdinal("MessageText"));
                        var datetime = reader.GetDateTime(reader.GetOrdinal("MessageTime"));
                        var id = reader.GetInt32(reader.GetOrdinal("MessageID"));
                        var ordinalNo = reader.GetInt32(reader.GetOrdinal("OrdinalNo"));
                        messages.Add(new Message { ID = id, DateAndTime = datetime, Text = text, OrdinalNo = ordinalNo });
                    }
                }
            }
            return messages.OrderBy(m => m.DateAndTime).ToList();
        }

        //Получение истории сообщений за intervalInMinutes минут от текущего времени
        public async Task<IEnumerable<Message>> GetHistory(int intervalInMinutes)
        {
            List<Message> messages = new List<Message>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                await connection.OpenAsync();
                var timeSpan = DateTime.UtcNow - TimeSpan.FromMinutes(intervalInMinutes);
                command.CommandText = "SELECT * FROM Messages WHERE MessageTime > @timeSpan";
                command.Parameters.AddWithValue("@timeSpan", timeSpan);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var text = reader.GetString(reader.GetOrdinal("MessageText"));
                        var datetime = reader.GetDateTime(reader.GetOrdinal("MessageTime"));
                        var id = reader.GetInt32(reader.GetOrdinal("MessageID"));
                        var ordinalNo = reader.GetInt32(reader.GetOrdinal("OrdinalNo"));
                        messages.Add(new Message { ID = id, DateAndTime = datetime, Text = text, OrdinalNo = ordinalNo });
                    }
                }
            }
            return messages.OrderBy(m => m.DateAndTime).ToList();
        }

        //Получение порядкового номера от последнего сообщения, добавленного в БД
        public async Task<int> GetLastOrdinalNo()
        {
            int ordinalNo = 0;
            using (var connection = new SqlConnection(_connectionString))
            using (var command = connection.CreateCommand())
            {
                await connection.OpenAsync();
                command.CommandText = "SELECT OrdinalNo FROM Messages " +
                                      "WHERE OrdinalNo = (SELECT MAX(OrdinalNo) FROM Messages)";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ordinalNo = reader.GetInt32(reader.GetOrdinal("OrdinalNo"));
                    }
                }
            }
            return ordinalNo;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.TelegramBot
{
    internal class TelegrammBotInit
    {
        public async Task StartTelegrammBotInitAsync()
        {
            var pathInfo = new
            {
                toDoUserFileName = "Data\\toDoUsers.json",
                toDoItemFolderName = "Data\\toDoItems",
                fileIndex = "Data\\fileIndex.json",
                fileListData = "Data\\fileListData.json"
            };

            #region Получение токена
            string token = string.Empty;

            // получение токена ТГ из переменной
            //string? token = Environment.GetEnvironmentVariable("TelegramBotTokenOTUSBasic", EnvironmentVariableTarget.User);

            //получение токена ТГ из консоли
            //Console.WriteLine("Ведите токен для ТГ бота");
            //token = Console.ReadLine();

            //получение токена ТГ из файла
            using (StreamReader reader = new StreamReader("C:\\Users\\Alkesandr\\Desktop\\tgtoken.txt"))
            {
                token = reader.ReadToEnd();
                Console.WriteLine(token);
            }
            #endregion
        }
    }
}

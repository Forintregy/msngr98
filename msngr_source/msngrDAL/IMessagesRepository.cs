using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace msngrDAL
{
    //Интерфейс репозитория с публичными методами записи и полуения данных в/из БД
    public interface IMessagesRepository
    {
        Task SendMessage(Message message);
        Task<IEnumerable<Message>> GetHistory(int intervalInMinutes);
        Task<IEnumerable<Message>> GetHistoryInRange(DateTime from, DateTime to);
        Task<int> GetLastOrdinalNo();
        void CheckDBExistence();
    }
}

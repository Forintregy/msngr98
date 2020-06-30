using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using msngrDAL;
using System;
using System.Threading.Tasks;

namespace msngrAPI.Hubs
{
    //Хаб SignalR, обеспечивает подключение клиентов к веб-сокету для получения сообщений
    public class MessagesHub : Hub
    {
        private ILogger<MessagesHub> _logger;
        private IMessagesRepository _repository;

        public int Count { get; set; }

        public MessagesHub(ILogger<MessagesHub> logger, IMessagesRepository repository)
        {

            _logger = logger;
            _repository = repository;
        }

        //Отправка сообщения всем подключенным клиентам
        public async Task SendMessage(string text)
        {
            try
            {
                await Clients.All.SendAsync("ReceiveMessage", text);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(DateTime.Now.ToString("hh:mm:ss") + ' ' + 
                                    $"Ошибка отправки сообщения клиентам: {ex.Message} " + 
                                    $"Стек вызовов: {ex.StackTrace}");
            }
        }

        //Подключение к веб-сокету
        public override async Task OnConnectedAsync()
        {
            try
            {
                var count = await _repository.GetLastOrdinalNo();
                await base.OnConnectedAsync();
                await Clients.Caller.SendAsync("UpdateCount", count);
                _logger.LogInformation(DateTime.Now.ToString("hh:mm:ss") + ' ' + 
                                    $"К чату присоединился пользователь с ID: {Context.ConnectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(DateTime.Now.ToString("hh:mm:ss") + ' ' + 
                                    $"Ошибка подключения к сокету: {ex.Message} " +
                                    $"Стек вызовов: {ex.StackTrace}");
            }
        }
    }
}

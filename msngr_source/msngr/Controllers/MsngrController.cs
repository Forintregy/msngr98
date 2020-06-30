using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using msngrAPI.ViewModels;
using msngrAPI.Hubs;
using msngrDAL;
using Microsoft.Extensions.Logging;

namespace msngrAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class MsngrController : ControllerBase
    {
        private IHubContext<MessagesHub> _hubContext;
        private IMessagesRepository _repository;
        private ILogger<MsngrController> _logger;

        public MsngrController(IHubContext<MessagesHub> hubContext, IMessagesRepository repository, ILogger<MsngrController> logger)
        {
            _hubContext = hubContext;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// POST-метод для отправки сообщения на сервер
        /// </summary>
        /// <param name="newMessage">
        /// Сообщение в json-формате {"text": "Текст сообщения", "ordinalNo": номер сообщения, начиная с 1}
        /// </param>
        /// <remarks>
        /// Пример сообщения: 
        /// {
        ///     "OrdinalNo": "1234",
        ///     "Text": "Not hello, not world"
        /// }
        /// </remarks>
        /// <returns>Новое сообщение номер [ordinalNo] с текстом [text]</returns>
        /// <response code="200">Сообщение успешно отправлено</response>
        /// <response code="400">Сообщение не должно быть пустым и его номер не может быть меньше 1</response>
        /// <response code="503">Ошибка БД</response>
        [HttpPost]
        [Route("SendMessage")]
        public async Task<IActionResult> SendMessage(MessageViewModel newMessage)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var datetime = DateTime.Now;
            try
            {
                var messageToDB = new Message
                {
                    Text = newMessage.Text,
                    DateAndTime = datetime,
                    OrdinalNo = newMessage.OrdinalNo
                };
                await _repository.SendMessage(messageToDB);
                var message = System.Text.Json.JsonSerializer.Serialize(messageToDB);
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                return Ok($"Сообщение успешно отправлено");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Ошибка добавления сообщения в БД: {ex.Message}. \n Стек вызовов: {ex.StackTrace}";
                _logger.Log(LogLevel.Critical, DateTime.Now.ToString("hh:mm:ss") + ' ' + errorMessage);
                return StatusCode(503, errorMessage);
            }
        }

        /// <summary>
        /// GET-метод, возвращающий историю сообщений за последние intervalInMinutes минут
        /// </summary>
        /// <param name="intervalInMinutes">
        /// Время в минутах от текущего момента, за которое необходимо сгенерировать историю сообщений 
        /// от текущего времени. Может оказаться проще и удобнее метода GetHistoryInRange
        /// </param>
        /// <returns></returns>
        /// <remarks>
        /// Пример запросе: 
        /// GetHistory/10
        /// </remarks>
        /// <returns>История сообщений за intervalInMinutes минут</returns>
        /// <response code="200">История получена</response>
        /// <response code="400">Время запроса не должно быть меньше или равным нулю</response> 
        /// <response code="204">Сообщения за это время отсутствуют</response> 
        /// <response code="503">Ошибка БД</response> 
        [HttpGet]
        [Route("GetHistory/{intervalInMinutes}")]
        public async Task<IActionResult> GetHistory(int intervalInMinutes=10)
        {
            if (intervalInMinutes <= 0) return BadRequest("Время запроса не должно быть меньше или равным нулю");
            try
            {
                var history = await _repository.GetHistory(intervalInMinutes);
                if (history.Any()) return Ok(history);
                else return NoContent();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Ошибка чтения истории сообщений из БД: {ex.Message}. \n Стек вызовов: {ex.StackTrace}";
                _logger.Log(LogLevel.Critical, DateTime.Now.ToString("hh:mm:ss") + ' ' + errorMessage);
                return StatusCode(503, errorMessage);
            }            
        }

        /// <summary>
        /// GET-метод получения истории в промежутке (местного) времени, формат запроса {от}+{до} в формате ГГГГ-ММ-ДД чч:мм:чч.
        /// Также подходит json-формат даты/времени. Возвращает дату в формате UTC
        /// </summary>
        /// <param name="from">Время начала истории</param>
        /// <param name="to">Время окончания истории</param>
        /// <returns></returns>
        /// <remarks>
        /// Пример запроса: 
        /// GetHistoryInrange/2020-06-16 10:00:00+2020-06-16 12:00:00
        /// </remarks>
        /// <returns>История сообщений от [from] до [to]</returns>
        /// <response code="200">История получена</response>
        /// <response code="400">Время начала запроса не может быть больше времени окончания</response> 
        /// <response code="204">Сообщения за это время отсутствуют</response> 
        /// <response code="503">Ошибка БД</response> 
        [HttpGet]
        [Route("GetHistoryInRange/{from}+{to}")]
        public async Task<IActionResult> GetHistoryInRange(DateTime from, DateTime to)
        {
            if (from > to) return BadRequest("Время начала запроса не может быть больше времени окончания");
            try
            {
                var history = await _repository.GetHistoryInRange(from,to);
                if (history.Any()) return Ok(history);
                else return NoContent();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Ошибка чтения истории сообщений из БД: {ex.Message}. \n Стек вызовов: {ex.StackTrace}";
                _logger.Log(LogLevel.Critical, DateTime.Now.ToString("hh:mm:ss") + ' ' + errorMessage);
                return StatusCode(503, errorMessage);
            }
        }
    }
}

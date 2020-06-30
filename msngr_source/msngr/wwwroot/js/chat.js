"use strict";

//Подключение к сокету
var connection = new signalR
	.HubConnectionBuilder()
	.withUrl("/Hubs/Messages")
	.configureLogging(signalR.LogLevel.Warning)
	.withAutomaticReconnect()
	.build();

//Число сообщений в чате; обновляется при подключении клиента к чату
var messagesCount = 0;

//Первый клиент
//Отправка строки в метод POST на стороне Web API
document.getElementById("sendButton").addEventListener("click", function (event) {
	var url = "/api/msngr/sendmessage";
	var textbox = document.getElementById("messageInput");
	var message = textbox.value;
	var msg = { "Text": message, "OrdinalNo": ++messagesCount };
	$.ajax({
		type: "POST",
		url: url,
		data: JSON.stringify(msg),
		contentType: "application/json",
		dataType: "json"
	}).done(function (response) {
		console.log(response);
	}).fail(function (response) {
		console.log('Ошибка отправки сообщения: ' + response);
	});
	event.preventDefault();
	textbox.value = '';
	showCharCount('');
	textbox.focus();
});

//Второй клиент ("слушает" вебсокет по адресу, указанному в connection (/Hubs/Messages))
//Отображает сообщения в реальном времени по сигналу от веб-сокета SignalR
connection.on("ReceiveMessage", function (message) {
	var formattedMessage = JSON.parse(message);
	var text = formattedMessage.Text;
	var ordinalNo = formattedMessage.OrdinalNo;
	var date = new Date(formattedMessage.DateAndTime);
	console.log("Новое сообщение #" + ordinalNo + ' ' + date);
	var messageBox = formatMessageBox(text, date, ordinalNo);
	document.getElementById("messagesList").appendChild(messageBox);
	$('ul#messagesList').scrollTop($('ul#messagesList')[0].scrollHeight);
});

//Третий клиент (вызывается автоматически при запуске страницы)
//Функция получения истории сообщений в интервале от 'from' до 'to'
function getHistory(from, to) {
	var url = "/api/msngr/gethistoryinrange/" + from.toJSON() + '+' + to.toJSON();
	console.log(from.toJSON() + ' ' + to.toJSON());
	$.ajax({
		type: 'GET',
		url: url,
		success: function (data) {
			for (var i = 0; i < data.length; i++) {
				var text = data[i].text;
				var date = new Date(data[i].dateAndTime) + ' UTC';
				var ordinalNo = data[i].ordinalNo;
				console.log('Из истории прочитано сообщение #' + ordinalNo + ', время: ' + formatDateTimeLocal(date));
				var messageBox = formatMessageBox(text, date, ordinalNo);
				document.getElementById("messagesList").appendChild(messageBox);
				$('ul#messagesList').scrollTop($('ul#messagesList')[0].scrollHeight);
			}
		}
	}).fail(function (response) {
		console.log('Ошибка получения истории: ' + response);
	})
}

//Блокировка кнопок до установления подключения
document.getElementById("sendButton").disabled = true;
document.getElementById("showHistory").disabled = true;
document.getElementById("messagesList").style.background = "#c0c0c0";

//Функция форматирования даты/времени в читабельный вид
function formatDateTimeLocal(date) {
	var d = new Date(date),
		month = '' + (d.getMonth() + 1),
		day = '' + d.getDate(),
		year = d.getFullYear(),
		hour = '' + d.getHours(),
		minutes = '' + d.getMinutes(),
		seconds = '' + d.getSeconds();

	if (month.length < 2) month = '0' + month;
	if (day.length < 2) day = '0' + day;
	if (hour.length < 2) hour = '0' + hour;
	if (minutes.length < 2) minutes = '0' + minutes;
	if (seconds.length < 2) seconds = '0' + seconds;

	return [hour, minutes, seconds].join(':') + ' ' +
			[day, month, year].join('.');
}

//Функция вычитания минут и возвращения нового объекта Date
function subtractMinutes(date, minutes) {
	var dt = new Date(date);
	var result = new Date(dt.setMinutes(dt.getMinutes() - minutes));
	return result;
}

//Функция визуального форматирования сообщения
function formatMessageBox(text, date, ordinalNo) {
	var msgUl = document.createElement("li");
	msgUl.className = "window";
	msgUl.style = "margin:20px";
	var msgHeader = document.createElement("p");
	msgHeader.className = "title-bar title-bar-text";
	msgHeader.style = "margin-right: 0px";
	var msgBody = document.createElement("p");
	msgHeader.textContent = "#" + ordinalNo + " | " + formatDateTimeLocal(date);
	msgBody.textContent = text;
	msgUl.appendChild(msgHeader);
	msgUl.appendChild(msgBody);
	return msgUl;
}

//Разблокировка отправки, получение истории за последние 10 минут
connection.start().then(function () {
	var from = subtractMinutes(new Date(), 10);
	console.log('from:' + from);
	var to = new Date();
	console.log('to:' + to);
	getHistory(from, to);
	document.getElementById("sendButton").disabled = false;
	document.getElementById("showHistory").disabled = false;
	console.log('Успешное подключение к веб-сокету!');
	document.getElementById("messagesList").style.background = "#fff";
}).catch(function (err) {
	return console.error(err.toString());
});

//Обработчик события просмотра истории сообщений
document.getElementById("showHistory")
		.addEventListener("click", function ()
		{
			var log = document.getElementById("messagesList");
			log.innerHTML = "";
			var from = subtractMinutes(new Date(), 3600);
			var to = new Date();
			getHistory(from, to);
		})

//Обработчик количества оставшихся в сообщении символов
function showCharCount(value) {
	var length = value.length;
	document.getElementById("lettersCount").innerHTML = 128 - length;
}

//Функция обновления числа сообщений при подключении к веб-сокету
connection.on("UpdateCount", function (newCount) {
	messagesCount = newCount;
})
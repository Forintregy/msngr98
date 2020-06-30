using System;
using System.ComponentModel.DataAnnotations;

namespace msngrDAL
{
    //Модель сообщения
    public class Message
    {
        [Required]
        public int ID { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        public DateTime DateAndTime { get; set; }
        [Required]
        [Range(0,int.MaxValue)]
        public int OrdinalNo { get; set; }
    }
}

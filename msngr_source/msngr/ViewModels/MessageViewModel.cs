using msngrAPI.CustomValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace msngrAPI.ViewModels
{
    public class MessageViewModel
    {
        [Required]
        [MinValue(1)]
        public int OrdinalNo { get; set; }
        [Required]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "Длина сообщения должна лежать в диапазоне от 1 до 128 символов")]
        public string Text { get; set; }
    }
}

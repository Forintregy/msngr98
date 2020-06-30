using System.ComponentModel.DataAnnotations;

namespace msngrAPI.CustomValidation
{
    //Дополнительные аттрибуты для валидации полей сущностей
    public class MinValueAttribute : ValidationAttribute
    {
        private readonly int _minValue;

        public MinValueAttribute(int minValue)
        {
            _minValue = minValue;
            ErrorMessage = "Значение не может быть нулевым или отрицательным!";
        }

        public override bool IsValid(object value)
        {
            return (int)value >= _minValue;
        }

 
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace msngrDAL
{
    public static class MessagesDBPreparation
    {
        public static void PrepareDB(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
               PrepareDB(serviceScope.ServiceProvider.GetService<IMessagesRepository>());
            }
        }

        //Вызов метода проверки и (при необходимости) создания новой БД
        private static void PrepareDB(IMessagesRepository repository)
        {
            repository.CheckDBExistence();
        }
    }
}

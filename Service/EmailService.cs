using System.Net.Mail;
using System.Net;
using Newtonsoft.Json;
using System.Text;

namespace Inventree_App.Service
{
    public class EmailService : IEmailService
    {
        //private readonly string fromEmail = "kameshwaran595@gmail.com";
        //private readonly string appPassword = "Kamesh@123"; 

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var httpClient = new HttpClient();
            var url = "https://script.google.com/macros/s/AKfycbwrC8CTDS_8SHhJ6wRPeM5CtsrF005uq9gtWRB9YCHVBDe4f7TV1hTI6BxzPIaQQgwV/exec"; // your script URL

            var payload = new
            {
                to = toEmail,
                subject = subject,
                body = body
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();
        }
        //public async Task SendIndentMailViaAppScript(string toEmail, string subject, string htmlBody)
        //{
        
        //}
    }
}

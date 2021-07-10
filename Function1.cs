using System;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Text;
using Newtonsoft.Json;

namespace TSLADailySummary
{
    public class Account
    {
        public string email { get; set; }
        public string stock { get; set; }
    }

    public static class TSLADailySummary
    {
        // Definir as variáveis.
        static string FILE_NAME = "tesla_data.json";
        static string CONTAINER_NAME = "container-output-api";
        
        // Variável de environment na Azure > Function > Configuration > Application settings.
        static string CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureConnectionString", EnvironmentVariableTarget.Process);
        static string API_KEY = Environment.GetEnvironmentVariable("AzureAPIkey", EnvironmentVariableTarget.Process);

        static string API_URI = "https://apidojo-yahoo-finance-v1.p.rapidapi.com/market/v2/get-quotes?region=US&symbols=";

        // Código da function.
        [FunctionName("TSLADailySummary")]
        public static async System.Threading.Tasks.Task RunAsync([TimerTrigger("0 0 0/1 * * 1-5")]TimerInfo myTimer, ILogger log)
        {
            //static string json = @"{'Email': 'james@example.com','Active': true}";
            //static Account account = JsonConvert.DeserializeObject<Account>(json);
            //log.LogInformation(account.Email);





            log.LogInformation("UPDATE 8");

            // Referência do blob client.
            BlobServiceClient BSC = new BlobServiceClient(CONNECTION_STRING);

            // Referência do container.
            var containerClient0 = BSC.GetBlobContainerClient("container-formdata");

            // Referência do blob.
            BlobClient blobClient0 = containerClient0.GetBlobClient("formdata-result.json");





            


            var response = await blobClient0.DownloadAsync();
            var line = (dynamic)null;
            using (var streamReader = new StreamReader(response.Value.Content))
            {
                while (!streamReader.EndOfStream)
                {
                    line = await streamReader.ReadLineAsync();
                }
            }

            Account account = JsonConvert.DeserializeObject<Account>(line);

            log.LogInformation(account.stock);



            /*
            var response = await blobClient0.DownloadAsync();
            var line = (dynamic)null;
            using (var streamReader = new StreamReader(response.Value.Content))
            {
                while (!streamReader.EndOfStream)
                {
                    line = await streamReader.ReadLineAsync();
                    Console.WriteLine(line);
                }
            }
            */





            string URI_LINK_CONCANATED = API_URI + account.stock;






            // Log da execução.
            log.LogInformation($"Function triggered at: {DateTime.Now}");

            // Ir buscar os dados à API do YahooFinance.
            var APIclient = new HttpClient();
            var APIrequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(URI_LINK_CONCANATED),
                Headers =
                {
                    { "x-rapidapi-key", API_KEY },
                    { "x-rapidapi-host", "apidojo-yahoo-finance-v1.p.rapidapi.com" },
                },
            };
            using (var APIresponse = await APIclient.SendAsync(APIrequest))
            {
                // Confirmar se o pedido foi feito com sucesso.
                APIresponse.EnsureSuccessStatusCode();

                // Passar a resposta para a variável, em formato string.
                // estava var antes, confirmar se funciona assim
                string fileContent = await APIresponse.Content.ReadAsStringAsync();

                // Referência do blob client.
                //BlobServiceClient BSC = new BlobServiceClient(CONNECTION_STRING);

                // Referência do container.
                var containerClient = BSC.GetBlobContainerClient(CONTAINER_NAME);

                // Referência do blob.
                BlobClient blobClient = containerClient.GetBlobClient(FILE_NAME);

                // Converter para bytes e guardar na memória.
                using (MemoryStream memoryStreamFileContent = new MemoryStream(Encoding.UTF8.GetBytes(fileContent)))
                {
                    // Fazer upload do blob com overwrite.
                    await blobClient.UploadAsync(memoryStreamFileContent, true);

                    // Log de finalização.
                    log.LogInformation($"Function finalized at: {DateTime.Now}");
                }
            }
        }
    }
}
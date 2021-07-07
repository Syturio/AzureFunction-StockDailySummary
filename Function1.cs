using System;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Text;

namespace TSLADailySummary
{
    public static class TSLADailySummary
    {
        // Definir as vari�veis.
        static string FILE_NAME = "tesla_data.json";
        static string CONTAINER_NAME = "container-output-api";

        // Vari�vel de environment na Azure > Function > Configuration > Application settings.
        static string CONNECTION_STRING = Environment.GetEnvironmentVariable("AzureConnectionString", EnvironmentVariableTarget.Process);
        static string API_KEY = Environment.GetEnvironmentVariable("AzureAPIkey", EnvironmentVariableTarget.Process);

        // C�digo da function.
        [FunctionName("TSLADailySummary")]
        public static async System.Threading.Tasks.Task RunAsync([TimerTrigger("0 0 0/1 * * 1-5")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("UPDATE 4");

            // Log da execu��o.
            log.LogInformation($"Function triggered at: {DateTime.Now}");

            // Ir buscar os dados � API do YahooFinance.
            var APIclient = new HttpClient();
            var APIrequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://apidojo-yahoo-finance-v1.p.rapidapi.com/market/v2/get-quotes?region=US&symbols=TSLA"),
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

                // Passar a resposta para a vari�vel, em formato string.
                // estava var antes, confirmar se funciona assim
                string fileContent = await APIresponse.Content.ReadAsStringAsync();

                // Refer�ncia do blob client.
                BlobServiceClient BSC = new BlobServiceClient(CONNECTION_STRING);

                // Refer�ncia do container.
                var containerClient = BSC.GetBlobContainerClient(CONTAINER_NAME);

                // Refer�ncia do blob.
                BlobClient blobClient = containerClient.GetBlobClient(FILE_NAME);

                // Converter para bytes e guardar na mem�ria.
                using (MemoryStream memoryStreamFileContent = new MemoryStream(Encoding.UTF8.GetBytes(fileContent)))
                {
                    // Fazer upload do blob com overwrite.
                    await blobClient.UploadAsync(memoryStreamFileContent, true);

                    // Log de finaliza��o.
                    log.LogInformation($"Function finalized at: {DateTime.Now}");
                }
            }
        }
    }
}
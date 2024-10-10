using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Globalization;


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var encoding = Encoding.UTF8;

Console.OutputEncoding = encoding;

Console.WriteLine("Получает Итоги(zreports.xml) и покупки(purchases.xml) используя API кассового сервера Set10 (FiscalInfoExport)");
Console.WriteLine();
Console.WriteLine("За доработкой / корректировкой скрипта обращайтесь t.me/lamorez");
Console.WriteLine();

// Запрос IP-адреса и даты у пользователя
Console.Write("Введите IP-адрес сервера: ");
string ip = Console.ReadLine();

Console.Write("Введите дату операционного дня (в формате ГГГГ-ММ-ДД): ");
string dateOperDay = Console.ReadLine();

await GetZreportsByOperday(ip, dateOperDay);
await GetPurchasesByOperday(ip, dateOperDay);

async Task GetZreportsByOperday(string host, string operday)
{
    string url = $"http://{host}:8090/SET-ERPIntegration/FiscalInfoExport";

    // SOAP-запрос
    string soapEnvelope = $@"
        <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:plug='http://plugins.operday.ERPIntegration.crystals.ru/'>
           <soapenv:Header/>
           <soapenv:Body>
              <plug:getZReportsByOperDay>
                 <dateOperDay>{operday}</dateOperDay>
              </plug:getZReportsByOperDay>
           </soapenv:Body>
        </soapenv:Envelope>";

    // Создаем HttpClient для отправки запроса
    using HttpClient client = new();
    // Настройка заголовков запроса
    HttpRequestMessage request = new(HttpMethod.Post, url);
    request.Headers.Add("SOAPAction", "getZReportsByOperDay");
    request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

    // Отправляем запрос и получаем ответ
    HttpResponseMessage response = await client.SendAsync(request);
    string responseContent = await response.Content.ReadAsStringAsync();

    // Парсинг XML-ответа для извлечения Base64
    string base64Data = ExtractBase64FromResponse(responseContent);

    if (!string.IsNullOrEmpty(base64Data))
    {
        // Декодирование Base64 в байты
        byte[] zReportBytes = Convert.FromBase64String(base64Data);

        // Сохранение данных в файл как XML
        File.WriteAllBytes("zreport.xml", zReportBytes);

        Console.WriteLine("Z-отчет успешно сохранен как zreport.xml");
    }
    else
    {
        Console.WriteLine("Не удалось извлечь Z-отчет.");
    }
}

async Task GetPurchasesByOperday(string host, string operday)
{
    string url = $"http://{host}:8090/SET-ERPIntegration/FiscalInfoExport";

    // SOAP-запрос
    string soapEnvelope = $@"
        <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:plug='http://plugins.operday.ERPIntegration.crystals.ru/'>
           <soapenv:Header/>
           <soapenv:Body>
              <plug:getPurchasesByOperDay>
                 <dateOperDay>{operday}</dateOperDay>
              </plug:getPurchasesByOperDay>
           </soapenv:Body>
        </soapenv:Envelope>";

    // Создаем HttpClient для отправки запроса
    using HttpClient client = new();
    // Настройка заголовков запроса
    HttpRequestMessage request = new(HttpMethod.Post, url);
    request.Headers.Add("SOAPAction", "getPurchasesByOperDay");
    request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

    // Отправляем запрос и получаем ответ
    HttpResponseMessage response = await client.SendAsync(request);
    string responseContent = await response.Content.ReadAsStringAsync();

    // Парсинг XML-ответа для извлечения Base64
    string base64Data = ExtractBase64FromResponse(responseContent);

    if (!string.IsNullOrEmpty(base64Data))
    {
        // Декодирование Base64 в байты
        byte[] purchaseDataBytes = Convert.FromBase64String(base64Data);

        // Сохранение данных в файл как XML
        File.WriteAllBytes("purchases.xml", purchaseDataBytes);

        Console.WriteLine("Чеки успешно сохранены как purchases.xml");
    }
    else
    {
        Console.WriteLine("Не удалось извлечь чеки.");
    }
}

// Метод для извлечения Base64 данных из XML-ответа
string ExtractBase64FromResponse(string responseContent)
{
    XmlDocument doc = new();
    doc.LoadXml(responseContent);

    // Пытаемся найти элемент <return>, который содержит Base64 данные
    XmlNode? base64Node = doc.GetElementsByTagName("return").Item(0);

    if (base64Node != null)
    {
        return base64Node.InnerText;
    }
    return string.Empty;
}
using System.Net.Http.Json;
using Subscriber.DTOs;

bool authStatus = false;
int? userId = default;
while (!authStatus)
{
    Console.WriteLine("Enter your user id:");
    var input = Console.ReadLine();
    if (Int32.TryParse(input, out int id))
    {
        authStatus = true;
        userId = id;
    }
}

if (authStatus)
{
    Console.WriteLine("Press ESC to stop");
    do
    {
        var client = new HttpClient();
        Console.WriteLine("Listening...");
        while (!Console.KeyAvailable)
        {
            List<int> ackIds = await GetMessagesAsync(client, userId);

            Thread.Sleep(2000);
            if (ackIds.Any())
            {
                await AckMessagesAsync(client, ackIds, userId);
            }
        }

    } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
}

static async Task<List<int>> GetMessagesAsync(HttpClient client, int? userId)
{
    if (userId is null) throw new ArgumentException("User ID is null");
    
    List<int> ackIds = new List<int>();
    List<MessageReadDto>? newMessages = new List<MessageReadDto>();

    try
    {
        newMessages =
            await client.GetFromJsonAsync<List<MessageReadDto>>($"http://localhost:5019/api/subscriptions/{userId}/messages");
    }
    catch (Exception e)
    {
        return ackIds;
    }

    if (newMessages is not null)
    {
        foreach (var message in newMessages)
        {
            Console.WriteLine($"{message.Id} - {message.TopicMessage} - {message.MessageStatus}");
            ackIds.Add(message.Id);
        }
    }

    return ackIds;
}

static async Task AckMessagesAsync(HttpClient client, List<int> ackIds, int? userId)
{
    if (userId is null) throw new ArgumentException("User ID is null");
    var response = await client.PostAsJsonAsync($"http://localhost:5019/api/subscriptions/{userId}/messages", ackIds);
    var returnMessage = await response.Content.ReadAsStringAsync();

    Console.WriteLine(returnMessage) ;
}
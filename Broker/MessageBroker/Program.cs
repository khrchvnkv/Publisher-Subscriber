using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")); 
});

var app = builder.Build();

app.UseHttpsRedirection();

// Create topic
app.MapPost("api/topics", async (AppDbContext context, Topic topic) =>
{
    await context.Topics.AddAsync(topic);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{topic.Id}", topic);
});

// Return All topics
app.MapGet("api/topics", async (AppDbContext context) =>
{
    var topics = await context.Topics.ToListAsync();
    return Results.Ok(topics);
});

// Publish message
app.MapPost("api/topics/{id}/messages", async (AppDbContext context, int id, Message messageModel) =>
{
    bool topicContains = await context.Topics.AnyAsync(t => t.Id == id);
    if (!topicContains) return Results.NotFound("Topic not found");

    var subscriptions = context.Subscriptions.Where(s => s.TopicId == id);
    if (!subscriptions.Any()) return Results.NotFound("There are no subscription for this topic");

    foreach (var subscription in subscriptions)
    {
        var message = new Message()
        {
            TopicMessage = messageModel.TopicMessage,
            SubscriptionId = subscription.Id,
            ExpiresAfter = messageModel.ExpiresAfter,
            MessageStatus = messageModel.MessageStatus
        };

        await context.Messages.AddAsync(message);
    }

    await context.SaveChangesAsync();

    return Results.Ok("Messages has been published ");
});

// Create subscription
app.MapPost("api/topics/{id}/subscriptions", async (AppDbContext context, int id , Subscription subscriptionModel) =>
{
    bool topicContains = await context.Topics.AnyAsync(t => t.Id == id);
    if (!topicContains) return Results.NotFound("Topic not found");

    subscriptionModel.TopicId = id;

    await context.Subscriptions.AddAsync(subscriptionModel);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{id}/subscriptions/{subscriptionModel.Id}", subscriptionModel); 
});

// Get Subscriber messages
app.MapGet("api/subscriptions/{id}/messages", async (AppDbContext context, int id) =>
{
    bool subscriptionContains = await context.Subscriptions.AnyAsync(s => s.Id == id);
    if (!subscriptionContains) return Results.NotFound("Subscription not found");

    var messages = context.Messages.Where(m => m.SubscriptionId == id &&
                                               m.MessageStatus != Message.Status.SENT.ToString());

    if (!messages.Any()) return Results.NotFound("No new messages");

    foreach (var message in messages)
    {
        message.MessageStatus = Message.Status.REQUESTED.ToString();
    }

    await context.SaveChangesAsync(); 

    return Results.Ok(messages);  
});

// Ack messages for subscriber
app.MapPost("api/subscriptions/{id}/messages", async (AppDbContext context, int id, int[] confirmations) =>
{
    bool subscriptionContains = await context.Subscriptions.AnyAsync(s => s.Id == id);
    if (!subscriptionContains) return Results.NotFound("Subscription not found");

    if (!confirmations.Any()) return Results.BadRequest("");

    int count = 0;
    foreach (var confirmation in confirmations)
    {
        var message = await context.Messages.FirstOrDefaultAsync(m => m.Id == confirmation);
        if (message is not null)
        {
            message.MessageStatus = Message.Status.SENT.ToString();
            count++;
        }
    }
    if (count > 0) await context.SaveChangesAsync();
    return Results.Ok($"Acknowledged {count}/{confirmations.Length} messages");
});

app.Run();
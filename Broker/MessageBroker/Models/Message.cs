using System.ComponentModel.DataAnnotations;

namespace MessageBroker.Models
{
    public class Message
    {
        public enum Status
        {
            NEW,
            REQUESTED,
            SENT
        }
        
        [Key] 
        public int Id { get; set; }
        
        [Required] 
        public string TopicMessage { get; set; }

        public int SubscriptionId { get; set; }

        [Required]
        public DateTime ExpiresAfter { get; set; } = DateTime.Now.AddDays(1);

        public string MessageStatus { get; set; } = Status.NEW.ToString();
    }
}
using System.Text.Json.Serialization;

namespace InstagramFeedPlanner.Models;

public class Feed(Guid id, string name)
{
    public Guid Id { get; set; } = id;

    public string Name { get; set; } = name;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public List<Post>? Posts { get; set; } = null;

    public void Rename(string name) => Name = name;
}
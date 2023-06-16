namespace lab10.Models;

public class GameBrowserModel
{
    public class TaggedGame
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags = new List<string>();

        public TaggedGame(string name, string description, List<string> tags)
        {
            Name = name;
            Description = description;
            Tags = tags;
        }
    }

    public List<TaggedGame> AllGames = new List<TaggedGame>();
    public List<TaggedGame> DisplayedGames = new List<TaggedGame>();
}
using Microsoft.AspNetCore.Mvc.Rendering;

namespace lab10.Models;

public class RentalManagerModel
{
    public string GameToAdd { get; set; }
    public List<(string, int)> AllBoardgames = new List<(string, int)>();
    public List<SelectListItem> BoardgameList = new List<SelectListItem>();
    public List<(string, int)> RentalGames = new List<(string, int)>();
    
    public void AllBoardgamesToSelectList()
    {
        foreach (var boardgame in AllBoardgames)
        {
            BoardgameList.Add(new SelectListItem { Value = boardgame.Item2.ToString(), Text = boardgame.Item1 });
        }
    }
}
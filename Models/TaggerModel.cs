using Microsoft.AspNetCore.Mvc.Rendering;

namespace lab10.Models;

public class TaggerModel
{
    public string Tag { get; set; }
    public string Boardgame { get; set; }

    public List<SelectListItem> TagList = new List<SelectListItem>();
    public List<SelectListItem> BoardgameList = new List<SelectListItem>();
}
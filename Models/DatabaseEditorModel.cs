using Microsoft.AspNetCore.Mvc.Rendering;

namespace lab10.Models;

public class DatabaseEditorModel
{
    public class TableData
    {
        public string TableName;
        public List<string> ColumnNames;
        public List<List<string>> Rows;
    }

    public List<SelectListItem> TableList = new List<SelectListItem>
    {
        new SelectListItem { Value = "Users", Text = "Users" },
        new SelectListItem { Value = "Boardgames", Text = "Boardgames" },
        new SelectListItem { Value = "Tags", Text = "Tags" },
        new SelectListItem { Value = "GameTags", Text = "GameTags" },
        new SelectListItem { Value = "Rental", Text = "Rental" },
        new SelectListItem { Value = "RentedGames", Text = "RentedGames" }
    };

    public string SelectedTable { get; set; }
    public TableData SelectedData { get; set; }

    public void ProcessRawData(List<List<string>> data, string tableName = null)
    {
        SelectedData = new TableData
        {
            ColumnNames = data[0],
            TableName = tableName
        };
        data.RemoveAt(0);
        SelectedData.Rows = data;
    }
}
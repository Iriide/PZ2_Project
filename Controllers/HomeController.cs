using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using lab10.Models;

namespace lab10.Controllers;

//[Route("/")]
public class LoginController : Controller
{
    private readonly string data_base_name = "db.db";
    private readonly ILogger<LoginController> _logger;

    public LoginController(ILogger<LoginController> logger)
    {
        _logger = logger;
    }

    [Route("/")]
    public IActionResult Index()
    {
        ViewData["Username"] = HttpContext.Session.GetString("Username");

        return View();
    }

    /* --------------------------------- Log out -------------------------------- */
    [Route("/logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    /* --------------------------------- Log In --------------------------------- */
    [Route("/login")]
    public IActionResult Login()
    {
        ViewData["Username"] = HttpContext.Session.GetString("Username");
        return View();
    }


    [HttpPost]
    [Route("/login")]
    public IActionResult Login(IFormCollection form)
    {
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            String username = form["username"].ToString();
            String password = form["password"].ToString();

            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Users WHERE username = @Username AND password = @Password;";
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", MD5Hash(password));
            Console.WriteLine(MD5Hash(password));

            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                ViewData["Username"] = username;
                ViewData["Message"] = "Login successful";
                HttpContext.Session.SetString("Username", username);
            }
            else
            {
                ViewData["Message"] = "Invalid username or password";
            }

        }
        
        // What??
        if (!string.IsNullOrEmpty((string?)TempData["Username"]))
        {
            ViewData["Message"] = "You have to be logged in to see this view";
        }
        return View();
    }

    /* -------------------------------- Register -------------------------------- */
    [Route("/register")]
    public IActionResult Register()
    {
        ViewData["Username"] = HttpContext.Session.GetString("Username");
        return View();
    }

    [HttpPost]
    [Route("/register")]
    public IActionResult Register(IFormCollection form)
    {
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))

        {

            String username = form["username"].ToString();
            String password = form["password1"].ToString();

            connection.Open();
            if (password != form["password2"].ToString())
            {
                ViewData["Message"] = "Passwords do not match";
                return View();
            }

            // Check if username already exists
            var commandCheckIfRegistered = connection.CreateCommand();
            commandCheckIfRegistered.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @Username;";
            commandCheckIfRegistered.Parameters.AddWithValue("@Username", username);
            var reader = commandCheckIfRegistered.ExecuteReader();
            if (reader.Read())
            {
                if (reader.GetInt32(0) > 0)
                {
                    ViewData["Message"] = "Username already exists";
                    return View();
                }
            }

            // Register user

            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (Username, Password, IsAdmin) VALUES (@Username, @Password, User);";
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", MD5Hash(password));

            command.ExecuteNonQuery();

            ViewData["Message"] = "Registration successful";
        }

        return View();
    }

    public IActionResult DataBaseEditor()
    {
        DatabaseEditorModel model = new DatabaseEditorModel();
        return View(model);
    }
    
    [HttpPost]
    public IActionResult DataBaseEditor(IFormCollection form)
    {
        DatabaseEditorModel model = new DatabaseEditorModel();
        string target = form["SelectedTable"].ToString();
        List<List<string>> formData = new List<List<string>>();
        using(var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            switch (target)
            {
                case "Users":
                    formData.Add(new List<string>() { "id", "username", "password", "role" });
                    break;

                case "Boardgames":
                    formData.Add(new List<string>() { "id", "title", "description" });
                    break;

                case "Tags":
                    formData.Add(new List<string>() { "id", "tag_name" });
                    break;

                case "GameTags":
                    formData.Add(new List<string>() { "tag_id", "game_id" });
                    break;

                case "Rental":
                    formData.Add(new List<string>() { "rental_game", "game_id", "boardgame_id" });
                    break;

                case "RentedGames":
                    formData.Add(new List<string>() { "rented_game", "rented_by", "rented_from", "rented_to" });
                    break;
            }
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM " + target + ";";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var rowData = new String[reader.FieldCount];
                for(int i = 0; i < reader.FieldCount; i++)
                {
                    rowData[i] = reader.GetString(i);
                } 
                formData.Add(rowData.ToList());
            }

            model.ProcessRawData(formData, target);
            model.SelectedTable = target;
        }
        return View("DataBaseBrowser", model);
    }

    private string MD5Hash(string input)
    {
        using (var md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }




    /* -------------------------------- View data ------------------------------- */
    [Route("/data")]
    public IActionResult Data()
    {
        ViewData["username"] = HttpContext.Session.GetString("Username");
        if(string.IsNullOrEmpty((string?)ViewData["username"]))
            return View();

        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Games";
            var reader = command.ExecuteReader();

            List<String[]> data = new List<String[]>();
            while (reader.Read())
            {
                var rowData = new String[] { reader.GetInt32(0).ToString(), reader.GetString(1), reader.GetString(2), reader.GetString(3) };
                data.Add(rowData);
            }
            
            ViewData["Games"] = data;
        }
        return View();
    }

    /* -------------------------------- Search data -------------------------------- */
    [HttpPost]
    [Route("/data")]
    public IActionResult Data(IFormCollection form)
    {
        ViewData["username"] = HttpContext.Session.GetString("Username");
        if(string.IsNullOrEmpty(ViewData["username"]?.ToString()))
            return View();
        
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Games WHERE Name LIKE @Name;";
            command.Parameters.AddWithValue("@Name", "%" + form["search"].ToString() + "%");
            var reader = command.ExecuteReader();

            List<String[]> data = new List<String[]>();
            while (reader.Read())
            {
                var rowData = new String[] { reader.GetInt32(0).ToString(), reader.GetString(1), reader.GetString(2), reader.GetString(3) };
                data.Add(rowData);
            }
            
            ViewData["Games"] = data;
        }
        return View();
    }




    /* ---------------------------------- Error --------------------------------- */
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult SendRequestToDatabase(List<string> newValues)
    {
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            // add the newValues to the database, at newValues[0] is the table name, if any of the values is empy insert NULL
            // first obtain the column names
            List<string> columnNames;
            switch (newValues[0])
            {
                case "Users":
                    columnNames = new List<string>() { "id", "username", "password", "role" };
                    break;
                case "Boardgames":
                    columnNames = new List<string>() { "id", "title", "description" };
                    break;
                case "Tags":
                    columnNames = new List<string>() { "id", "tag_name" };
                    break;
                case "GameTags":
                    columnNames = new List<string>() { "tag_id", "game_id" };
                    break;
                case "Rental":
                    columnNames = new List<string>() { "rental_game", "game_id", "boardgame_id" };
                    break;
                case "RentedGames":
                    columnNames = new List<string>() { "rented_game", "rented_by", "rented_from", "rented_to" };
                    break;
                default:   
                    columnNames = new List<string>() { "id", "username", "password", "role" };
                    break;
            }
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO " + newValues[0] + "(";
            for (int i = 0; i < columnNames.Count; i++)
            {
                command.CommandText += columnNames[i];
                if (i != columnNames.Count - 1)
                    command.CommandText += ", ";
            }
            command.CommandText += ") VALUES (";
            for (int i = 1; i < newValues.Count; i++)
            {
                if (newValues[i] is null)
                    command.CommandText += "NULL";
                else
                    command.CommandText += "'" + newValues[i] + "'";
                if (i != newValues.Count - 1)
                    command.CommandText += ", ";
            }
            command.CommandText += ");";
            // execute the command, if it fails add the error to ViewBag
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
            }
            
        }
        return RedirectToAction("DataBaseEditor");
    }
    
    public IActionResult DeleteRow(List<string> columnValues, string tableName)
    {
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            List<string> columnNames = tableName switch
            {
                "Users" => new List<string>() { "id", "username", "password", "role" },
                "Boardgames" => new List<string>() { "id", "title", "description" },
                "Tags" => new List<string>() { "id", "tag_name" },
                "GameTags" => new List<string>() { "tag_id", "game_id" },
                "Rental" => new List<string>() { "rental_game", "game_id", "boardgame_id" },
                "RentedGames" => new List<string>() { "rented_game", "rented_by", "rented_from", "rented_to" },
                _ => new List<string>() { "id", "username", "password", "role" }
            };
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM " + tableName + " WHERE ";
            for (int i = 0; i < columnNames.Count; i++)
            {
                command.CommandText += columnNames[i] + " = ";
                if (columnValues[i] is null)
                    command.CommandText += "NULL";
                else
                    command.CommandText += "'" + columnValues[i] + "'";
                if (i != columnNames.Count - 1)
                    command.CommandText += " AND ";
            }
            command.CommandText += ";";
            // execute the command, if it fails add the error to ViewBag
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
            }
        }
        return RedirectToAction("DataBaseEditor");
    }
}
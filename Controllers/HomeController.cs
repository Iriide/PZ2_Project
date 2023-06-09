﻿using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using lab10.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
        ViewData["Admin"] = HttpContext.Session.GetString("Admin");

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

    public void CheckAdmin(string username)
    {
        using var connection = new SqliteConnection("Data Source=" + data_base_name);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users WHERE username = @Username AND role = 'admin';";
        command.Parameters.AddWithValue("@Username", username);
        var admin = command.ExecuteScalar();
        if (admin != null)
        {
            if ((long)admin > 0)
            {
                HttpContext.Session.SetString("Admin", "true");
            }
        }
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
                CheckAdmin(username);
                if (HttpContext.Session.GetString("Admin") == "true")
                {
                    ViewData["Admin"] = "true";
                }
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

    public IActionResult Tagger()
    {
        TaggerModel model = new TaggerModel();
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Boardgames;";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                model.BoardgameList.Add(new SelectListItem()
                {
                    Value = reader.GetString(0),
                    Text = reader.GetString(1),
                });
            }

            connection.Close();
            connection.Open();
            command.CommandText = "SELECT * FROM Tags;";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                model.TagList.Add(new SelectListItem()
                {
                    Value = reader.GetString(0),
                    Text = reader.GetString(1)
                });
            }
        }

        return View(model);
    }

    [HttpPost]
    public IActionResult TaggerAdd(IFormCollection form)
    {
        var target_game_id = form["Boardgame"].ToString();
        var target_tag_id = form["Tag"].ToString();

        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO GameTags (tag_id, game_id) VALUES (@TagId, @BoardgameId);";
            command.Parameters.AddWithValue("@BoardgameId", target_game_id);
            command.Parameters.AddWithValue("@TagId", target_tag_id);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                // add to ViewBag
                ViewBag.Error = e.Message;
            }
        }

        return RedirectToAction("Tagger");
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
            commandCheckIfRegistered.CommandText = "SELECT COUNT(*) FROM Users WHERE username = @Username;";
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
            command.CommandText = "INSERT INTO Users (username, password, role) VALUES (@Username, @Password, 'user');";
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", MD5Hash(password));

            command.ExecuteNonQuery();

            ViewData["Message"] = "Registration successful";
        }

        return View();
    }

    [Route("/databaseeditor")]
    public IActionResult DataBaseEditor()
    {
        DatabaseEditorModel model = new DatabaseEditorModel();
        return View(model);
    }

    [HttpPost]
    [Route("/databaseeditor")]
    public IActionResult DataBaseEditor(IFormCollection form)
    {
        DatabaseEditorModel model = new DatabaseEditorModel();
        string target = form["SelectedTable"].ToString();
        List<List<string>> formData = new List<List<string>>();
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
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
                    formData.Add(new List<string>() { "rental_game", "game_id" });
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
                for (int i = 0; i < reader.FieldCount; i++)
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
    [Route("/browser")]
    public IActionResult Browser()
    {
        ViewData["username"] = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty((string?)ViewData["username"]))
            return View();

        GameBrowserModel model = new GameBrowserModel();

        using (var connection = new SqliteConnection("Data Source=" + data_base_name)) 
        {
            connection.Open();
            var command = connection.CreateCommand();
            // get all tags and add them to model
            command.CommandText = "SELECT * FROM Tags;";
            var reader = command.ExecuteReader();
            // add them to model.AllTags which is a list of (string, int) tuples
            while (reader.Read())
            {
                model.AllTags.Add((reader.GetString(1), reader.GetInt32(0)));
            }
        } 

        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Boardgames";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                using var connection2 = new SqliteConnection("Data Source=" + data_base_name);
                connection2.Open();
                var command2 = connection2.CreateCommand();
                command2.CommandText =
                    "SELECT tag_name FROM Tags INNER JOIN GameTags ON Tags.id = GameTags.tag_id WHERE GameTags.game_id = @GameId;";
                command2.Parameters.AddWithValue("@GameId", reader.GetInt32(0));
                var reader2 = command2.ExecuteReader();
                List<string> tags = new List<string>();
                while (reader2.Read())
                {
                    tags.Add(reader2.GetString(0));
                }

                model.AllGames.Add(new GameBrowserModel.TaggedGame(reader.GetString(1), reader.GetString(2), tags));
            }
        }

        if (TempData["searchFilter"] != null || TempData["tagsFilter"] != null)
        {
            var searchFilter = TempData["searchFilter"].ToString();
            var tagsFilter = TempData["tagsFilter"].ToString().Split(',');
            List<GameBrowserModel.TaggedGame> filteredGames = new List<GameBrowserModel.TaggedGame>();
            foreach (var game in model.AllGames)
            {
                if (searchFilter is not null && !game.Name.ToLower().Contains(searchFilter.ToLower())) continue;
                bool containsAllTags = true;
                if (tagsFilter is not [""])
                {
                    if (tagsFilter.Any(tag => !game.Tags.Contains(tag)))
                    {
                        containsAllTags = false;
                    }
                }

                if (containsAllTags)
                {
                    filteredGames.Add(game);
                }
            }
            model.DisplayedGames = filteredGames;
            
        }else model.DisplayedGames = model.AllGames;

        return View(model);
    }

    /* -------------------------------- Search data -------------------------------- */
    [HttpPost]
    [Route("/browser")]
    public IActionResult Browser(IFormCollection form)
    {
        ViewData["username"] = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(ViewData["username"]?.ToString()))
            return View();

        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Boardgames WHERE title LIKE @Name;";
            command.Parameters.AddWithValue("@Name", "%" + form["search"].ToString() + "%");
            var reader = command.ExecuteReader();

            List<String[]> data = new List<String[]>();
            while (reader.Read())
            {
                var rowData = new String[] { reader.GetInt32(0).ToString(), reader.GetString(1), reader.GetString(2) };
                data.Add(rowData);
            }

            ViewData["Games"] = data;
        }

        return View();
    }

    public IActionResult FilterResults(IFormCollection form)
    {
        TempData["searchFilter"] = form["search"].ToString();
        TempData["tagsFilter"] = form["selectedTags"].ToString();
        return RedirectToAction("Browser");
    }

    public IActionResult RentalManager()
    {
        RentalManagerModel model = new RentalManagerModel();
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Boardgames";
            var reader = command.ExecuteReader();
            // add all the boardgames to the model.AllBoardgames list (string, int) tuples
            while (reader.Read())
            {
                model.AllBoardgames.Add((reader.GetString(1), reader.GetInt32(0)));
            }
            model.AllBoardgamesToSelectList();
        }

        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Rental";
            var reader = command.ExecuteReader();
            // the games are received as rental_game_id and game_id. We need to change the game_id to the game name using the model.AllBoardgames list
            while (reader.Read())
            {
                var rentalGameId = reader.GetInt32(0);
                var gameId = reader.GetInt32(1);
                var gameName = model.AllBoardgames.Find(x => x.Item2 == gameId).Item1;
                model.RentalGames.Add((gameName, rentalGameId));
            }
        }
        return View(model);
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
                    columnNames = new List<string>() { "rental_game", "game_id" };
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
                if (newValues[0] == "Users" && i == 3)
                {
                    if (newValues[i] is null)
                        command.CommandText += "NULL";
                    else
                        command.CommandText += "'" + MD5Hash(newValues[i]) + "'";
                    if (i != newValues.Count - 1)
                        command.CommandText += ", ";
                }
                else
                {
                    if (newValues[i] is null)
                        command.CommandText += "NULL";
                    else
                        command.CommandText += "'" + newValues[i] + "'";
                    if (i != newValues.Count - 1)
                        command.CommandText += ", ";
                }
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
                "Rental" => new List<string>() { "rental_game", "game_id" },
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

    public IActionResult RemoveTag(IFormCollection form)
    {
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            // we have the tag name and thr game name, we need to find the tag id and the game id
            command.CommandText = "SELECT id FROM Tags WHERE tag_name = '" + form["tagName"] + "';";
            var reader = command.ExecuteReader();
            reader.Read();
            int tag_id = reader.GetInt32(0);
            reader.Close();
            command.CommandText = "SELECT id FROM Boardgames WHERE title = '" + form["boardGameName"] + "';";
            reader = command.ExecuteReader();
            reader.Read();
            int game_id = reader.GetInt32(0);
            reader.Close();
            // now we have the tag id and the game id, we can delete the row from GameTags
            command.CommandText = "DELETE FROM GameTags WHERE tag_id = " + tag_id + " AND game_id = " + game_id + ";";
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

        return RedirectToAction("Browser");
    }

    public IActionResult AddRentalGame(IFormCollection form)
    {
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Rental(game_id) VALUES ('" + form["GameToAdd"] + "');";
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
            }
        }

        return RedirectToAction("RentalManager");
    }

    public IActionResult RentGame(IFormCollection form)
    {
        GameRentingViewModel model = new GameRentingViewModel();
        var targetGame = form["elementId"].ToString();
        if(TempData["GameId"] != null && targetGame == "")
        {
            targetGame = TempData["GameId"].ToString();
            ViewBag.Error = "Invalid date!";
        }
        else
        {
            TempData["GameId"] = targetGame;
        }
        model.GameId = Convert.ToInt32(targetGame);

        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            // select all games from RentedGames where the rented_game id is equal to the id of the game we want to rent
            command.CommandText = "SELECT * FROM RentedGames WHERE rented_game = " + targetGame + ";";
            var reader = command.ExecuteReader();
            // if the reader has any rows add them to the model
            while (reader.Read())
            {
                var user_id = reader.GetInt32(1);
                string user_name;
                using (var connection2 = new SqliteConnection("Data Source=" + data_base_name))
                {
                    connection2.Open();
                    var command2 = connection2.CreateCommand();
                    command2.CommandText = "SELECT username FROM Users WHERE id = " + user_id + ";";
                    var reader2 = command2.ExecuteReader();
                    reader2.Read();
                    user_name = reader2.GetString(0);
                    reader2.Close();
                }
                model.Reservations.Add(new Reservation()
                {
                    ReservationId = Convert.ToInt32(reader.GetString(0)),
                    User = user_name,
                    StartDate = Convert.ToDateTime(reader.GetString(2)),
                    FinishDate = Convert.ToDateTime(reader.GetString(3)),
                });
            }
        }

        return View(model);
    }

    public IActionResult ReserveGame(IFormCollection form)
    {
        //check if there isnt already a reservation for this game in this time
        var StartDate = Convert.ToDateTime(form["StartDate"]);
        var FinishDate = Convert.ToDateTime(form["FinishDate"]);
        var targetGame = form["gameId"].ToString();
        
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM RentedGames WHERE rented_game = " + targetGame + ";";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var startDate = Convert.ToDateTime(reader.GetString(2).ToString());
                var finishDate = Convert.ToDateTime(reader.GetString(3));
                if (StartDate >= startDate && StartDate <= finishDate)
                {
                    ViewBag.Error = "There is already a reservation for this game in this time";
                    return RedirectToAction("RentGame", new { elementId = targetGame });
                }
                if (FinishDate >= startDate && FinishDate <= finishDate)
                {
                    ViewBag.Error = "There is already a reservation for this game in this time";
                    return RedirectToAction("RentGame", new { elementId = targetGame });
                }
            }
            // if there is no reservation for this game in this time, add the reservation
            reader.Close();
            // get the id of the user from session 
            var username = HttpContext.Session.GetString("Username");
            command.CommandText = "SELECT id FROM Users WHERE username = '" + username + "';";
            reader = command.ExecuteReader();
            reader.Read();
            var userId = reader.GetInt32(0);
            reader.Close();
            // add the reservation
            var startDateString = StartDate.ToString("yyyy-MM-dd");
            var finishDateString = FinishDate.ToString("yyyy-MM-dd");
            command.CommandText = "INSERT INTO RentedGames(rented_game, rented_by, rented_from, rented_to) VALUES (" + targetGame + ", " + userId + ", '" + startDateString + "', '" + finishDateString + "');";
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
            }
        }
        
        

        return RedirectToAction("RentalManager");
    }

    public IActionResult CancelReservation(int reservationid, DateTime startDate, DateTime endDate)
    {
        using (var connection = new SqliteConnection("Data Source=" + data_base_name))
        {
            connection.Open();
            var command = connection.CreateCommand();
            // delete the reservation that has the id we got from the form and has the right date
            command.CommandText = "DELETE FROM RentedGames WHERE rented_game = " + reservationid + " AND rented_from = '" + startDate.ToString("yyyy-MM-dd") + "' AND rented_to = '" + endDate.ToString("yyyy-MM-dd") + "';";
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                ViewBag.Error = e.Message;
            }
            
        }
        return RedirectToAction("RentalManager");
    }
}
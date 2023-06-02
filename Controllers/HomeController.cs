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
            command.CommandText = "SELECT * FROM Users WHERE Username = @Username AND Password = @Password;";
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
            command.CommandText = "INSERT INTO Users (Username, Password) VALUES (@Username, @Password);";
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", MD5Hash(password));

            command.ExecuteNonQuery();

            ViewData["Message"] = "Registration successful";
        }

        return View();
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
}
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;


// dotnet add package Microsoft.EntityFrameworkCore.Sqlite && dotnet new mvc -o Mvc && dotnet dev-certs https --trust

internal class Program
{
    private static void Main(string[] args)
    {
        /* -------------------------------------------------------------------------- */
        /*                                  DataBase                                  */
        /* -------------------------------------------------------------------------- */
        var connectionStringBuilder = new SqliteConnectionStringBuilder();
        connectionStringBuilder.DataSource = "db.db";

        using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            // Create Users table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        id INTEGER PRIMARY KEY,
                        username TEXT,
                        password TEXT,
                        role TEXT
                    );";
                command.ExecuteNonQuery();

                // Create Boardgames table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Boardgames (
                        id INTEGER PRIMARY KEY,
                        title TEXT UNIQUE,
                        description TEXT
                    );";
                command.ExecuteNonQuery();

                // Create Tags table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Tags (
                        id INTEGER PRIMARY KEY,
                        tag_name TEXT UNIQUE
                    );";
                command.ExecuteNonQuery();

                // Create GameTags table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS GameTags (
                        tag_id INTEGER,
                        game_id INTEGER,
                        FOREIGN KEY (tag_id) REFERENCES Tags(id),
                        FOREIGN KEY (game_id) REFERENCES Boardgames(id)
                    );";
                command.ExecuteNonQuery();

                // Create Rental table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Rental (
                        rental_game INTEGER PRIMARY KEY,
                        game_id INTEGER,
                        boardgame_id INTEGER,
                        FOREIGN KEY (game_id) REFERENCES Boardgames(id),
                        FOREIGN KEY (boardgame_id) REFERENCES Boardgames(id)
                    );";
                command.ExecuteNonQuery();

                // Create RentedGames table
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS RentedGames (
                        rented_game INTEGER,
                        rented_by INTEGER,
                        rented_from DATETIME,
                        rented_to DATETIME,
                        FOREIGN KEY (rented_game) REFERENCES Rental(rental_game),
                        FOREIGN KEY (rented_by) REFERENCES Users(id)
                    );";
                command.ExecuteNonQuery();
            
                const string adminQuery = "SELECT * FROM Users WHERE Username = \"admin\";";
                command.CommandText = adminQuery;
                var reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    string insertUser = "INSERT INTO Users (username, password, role) VALUES ('admin','" + MD5Hash("admin") + "', 'admin');";
                    command.CommandText = insertUser;
                    command.ExecuteNonQuery();
                }
                reader.Close();
        }
        
        

        connection.Close();

        ReadData("Data/games.csv", "Boardgames",  new string[] { "title", "description" });
        ReadData("Data/tags.csv", "Tags",  new string[] { "tag_name" });
        //readData("Data/game_tags.csv", "GameTags", false, new string[] { "tag_id", "game_id" });
        //readData("Data/rental.csv", "Rental", true, new string[] { "game_id", "boardgame_id" });

        /* -------------------------------------------------------------------------- */
        /*                                     MVC                                    */
        /* -------------------------------------------------------------------------- */
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        //Session handling
        builder.Services.AddDistributedMemoryCache();

        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromSeconds(1000);
            options.Cookie.HttpOnly = true;//plik cookie jest niedostępny przez skrypt po stronie klienta
            options.Cookie.IsEssential = true;//pliki cookie sesji będą zapisywane dzięki czemu sesje będzie mogła być śledzona podczas nawigacji lub przeładowania strony
        });
        //KONIEC

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        //Session handling
        app.UseSession();
        //KONIEC

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Login}/{action=login}/{id?}");
        app.UseStatusCodePagesWithReExecute("/");

        app.Use(async (ctx, next) =>
        {
            await next();

            if ((ctx.Response.StatusCode == 404 || ctx.Response.StatusCode == 400) && !ctx.Response.HasStarted)
            {
                //Re-execute the request so the user gets the error page
                string originalPath = ctx.Request.Path.Value ?? "";
                ctx.Items["originalPath"] = originalPath;
                ctx.Request.Path = "/login/";
                ctx.Response.Redirect("/login/");
                await next();
            }
        });

        app.Run();
    }
    private static string MD5Hash(string input)
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

    public static void ReadData(string path, string Table, string[] columns)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder();
        connectionStringBuilder.DataSource = "db.db";

        using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        connection.Open();
        StreamReader sr = new StreamReader(path);
        while (!sr.EndOfStream)
        {
            string ? line = sr.ReadLine();
            if(line == null) break;
            string[] values = line.Split(';');
            string insert = "INSERT INTO " + Table + " (";
            for (int i = 0; i < columns.Length; i++)
            {
                insert += columns[i];
                if (i != columns.Length - 1)
                {
                    insert += ", ";
                }
            }
            insert += ") VALUES (";
            for (int i = 0; i < values.Length; i++)
            {
                insert += "\"" + values[i] + "\"";
                if (i != values.Length - 1)
                {
                    insert += ", ";
                }
            }
            insert += ");";
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = insert;
                command.ExecuteNonQuery();
            }catch(SqliteException e)
            {
                Console.WriteLine(e.Message);
            }
            
        }
        connection.Close();


    }

}
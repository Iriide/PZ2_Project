using Microsoft.Data.Sqlite;
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
        var Command = connection.CreateCommand();

        string dropTable = "DROP TABLE IF EXISTS Warehouse; DROP TABLE IF EXISTS IsAvaiable; DROP TABLE IF EXISTS Games; ";
        Command.CommandText = dropTable;
        Command.ExecuteNonQuery();

        // string createTable = "DROP TABLE IF EXISTS Users; CREATE TABLE IF NOT EXISTS Users (id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL, Password TEXT NOT NULL);";
        string createTable = "CREATE TABLE IF NOT EXISTS Users (id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL, Password TEXT NOT NULL);";
        Command.CommandText = createTable;
        Command.ExecuteNonQuery();

        string gamesTable = "CREATE TABLE IF NOT EXISTS Games (id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT NOT NULL, Price REAL NOT NULL);";
        Command.CommandText = gamesTable;
        Command.ExecuteNonQuery();

        string isAvaiableTable = "CREATE TABLE IF NOT EXISTS IsAvaiable (id INTEGER PRIMARY KEY AUTOINCREMENT, GameId INTEGER NOT NULL, Avaiable BOOLEAN NOT NULL, FOREIGN KEY(GameId) REFERENCES Games(id));";
        Command.CommandText = isAvaiableTable;
        Command.ExecuteNonQuery();

        string warehouseTable = "CREATE TABLE IF NOT EXISTS Warehouse (id INTEGER PRIMARY KEY AUTOINCREMENT, GameId INTEGER NOT NULL, Amount INTEGER NOT NULL, FOREIGN KEY(GameId) REFERENCES Games(id));";
        Command.CommandText = warehouseTable;
        Command.ExecuteNonQuery();

        connection.Close();

        readData("Data/games.csv", "Games", new string[] { "Name", "Description", "Price" });
        readData("Data/isAvaiable.csv", "IsAvaiable", new string[] { "GameId", "Avaiable" });
        readData("Data/warehouse.csv", "Warehouse", new string[] { "GameId", "Amount" });

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

    public static void readData(string path, string Table, string[] columns)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder();
        connectionStringBuilder.DataSource = "db.db";

        using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        connection.Open();
        StreamReader sr = new StreamReader(path);
        while (!sr.EndOfStream)
        {
            string ? line = sr.ReadLine();
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
                insert += "'" + values[i] + "'";
                if (i != values.Length - 1)
                {
                    insert += ", ";
                }
            }
            insert += ");";
            var Command = connection.CreateCommand();
            Command.CommandText = insert;
            Command.ExecuteNonQuery();
        }
        connection.Close();


    }

}
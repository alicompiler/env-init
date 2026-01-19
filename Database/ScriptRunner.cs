using Npgsql;

namespace EnvInit.Database;

public class ScriptRunner
{
    private readonly string _connectionString;

    public ScriptRunner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void RunScript(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Script file not found: {filePath}");
        }

        var scriptContent = File.ReadAllText(filePath);

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = scriptContent;
        command.ExecuteNonQuery();
    }
}

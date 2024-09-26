using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;

class MemMonitorWriter
{

    static void Main(string[] args)
    {
        var connectionString = ConfigurationManager.AppSettings["connectionString"]; //Get the conn string from App.config
        var processes = Process.GetProcesses();
        var timestamp = DateTime.UtcNow;
        var computerName = Environment.MachineName;

        WriteMemoryUsage(connectionString, processes, timestamp, computerName);
    }

    static void WriteMemoryUsage(string connectionString, Process[] processes, DateTime timestamp, string computerName)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                var command = CreateInsertCommand(connection, transaction, timestamp, computerName);

                foreach (var process in processes)
                {
                    string processName = process.ProcessName;
                    long memoryUsage = process.WorkingSet64;

                    InsertCommandParameters(command, processName, memoryUsage);
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Inserted: {processName}, {memoryUsage}, {timestamp}, {computerName}");
                }

                transaction.Commit();
            }
        }
    }

    static SqlCommand CreateInsertCommand(SqlConnection connection, SqlTransaction transaction, DateTime timestamp, string computerName)
    {
        var command = new SqlCommand("INSERT INTO ProcessMemoryUsage (ProcessName, MemoryUsage, Timestamp, ComputerName) VALUES (@ProcessName, @MemoryUsage, @Timestamp, @ComputerName)",
            connection)
        { Transaction = transaction };
        // Pre-add parameters for reuse
        command.Parameters.AddWithValue("@ProcessName", null);
        command.Parameters.AddWithValue("@MemoryUsage", 0);
        command.Parameters.AddWithValue("@Timestamp", timestamp);
        command.Parameters.AddWithValue("@ComputerName", computerName);

        return command;
    }

    static void InsertCommandParameters(SqlCommand command, string processName, long memoryUsage)
    {
        command.Parameters["@ProcessName"].Value = processName;
        command.Parameters["@MemoryUsage"].Value = memoryUsage;
    }
}
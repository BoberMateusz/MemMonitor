using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;

class MemMonitorWriter
{

    static void Main(string[] args)
    {
        string connectionString = ConfigurationManager.AppSettings["connectionString"];
        var processes = Process.GetProcesses();
        var timestamp = DateTime.UtcNow;
        var computerName = Environment.MachineName;

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                var command = new SqlCommand("INSERT INTO ProcessMemoryUsage (ProcessName, MemoryUsage, Timestamp, ComputerName) VALUES (@ProcessName, @MemoryUsage, @Timestamp, @ComputerName)", connection) { Transaction = transaction };
                command.Parameters.AddWithValue("@ProcessName", null); // Pre-add parameter for reuse
                command.Parameters.AddWithValue("@MemoryUsage", 0);  // Pre-add parameter for reuse
                command.Parameters.AddWithValue("@Timestamp", timestamp);
                command.Parameters.AddWithValue("@ComputerName", computerName);

                foreach (var process in processes)
                {
                    command.Parameters["@ProcessName"].Value = process.ProcessName;
                    command.Parameters["@MemoryUsage"].Value = process.WorkingSet64;
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Inserted: {process.ProcessName}, {process.WorkingSet64}, {timestamp}, {computerName}");
                }

                transaction.Commit();
            }
        }
    }
}
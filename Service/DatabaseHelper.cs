using Dapper;
using Inventree_App.Context;
using Inventree_App.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace Inventree_App.Service
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;
        private readonly ApplicationContext _context;

        public DatabaseHelper(IConfiguration connectionString, ApplicationContext applicationContext)
        {
            _context = applicationContext;
            _connectionString = connectionString.GetConnectionString("DefaultConnection"); // Get connection string from appsettings.json
        }

        public List<Stocks> GetCustomColumns()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                return _context.Stocks.ToList();
            }
        }

        public string AddColumn(string columnName, string dataType)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(dataType))
                {
                    return "Column name and data type cannot be empty.";
                }

                var existingColumns = GetColumns();
                if (existingColumns.Contains(columnName))
                {
                    return $"Column '{columnName}' already exists.";
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    string query = $"ALTER TABLE stocks ADD COLUMN `{columnName}` {dataType}";

                    connection.Open();
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return $"Column '{columnName}' added successfully!";
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }


        public List<string> GetColumns()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'stocks' AND TABLE_SCHEMA = DATABASE();";
                var columns = connection.Query<string>(query).ToList();
                                               // Get stock data
                return columns; // Matches @model List<Stocks>
            }
        }
    }
}

using Inventree_App.Context;
using Inventree_App.Models;
using OfficeOpenXml;

namespace Inventree_App.Service
{
    public class BackupService : BackgroundService
    {
        private readonly ApplicationContext _context;

        public BackupService( ApplicationContext context)
        {
            _context = context;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every hour
                await CreateBackup();
            }
        }

        private async Task CreateBackup()
        {
            try
            {
                var inventoryData = GetRecentInventoryData();
                string backupFolder = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
                Directory.CreateDirectory(backupFolder); // Ensure directory exists

                string fileName = "Latest_InventoryBackup.xlsx"; // Fixed filename for updating
                string filePath = Path.Combine(backupFolder, fileName);

                using (var package = new ExcelPackage())
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    if (File.Exists(filePath))
                    {
                        // Load existing file
                        var existingFile = new FileInfo(filePath);
                        using (var existingPackage = new ExcelPackage(existingFile))
                        {
                            var worksheet = existingPackage.Workbook.Worksheets.FirstOrDefault() ?? existingPackage.Workbook.Worksheets.Add("Inventory Backup");
                            int rowCount = worksheet.Dimension?.Rows ?? 1;

                            // Append new data below existing data
                            worksheet.Cells[rowCount + 1, 1].LoadFromCollection(inventoryData, true);

                            existingPackage.Save();
                        }
                    }
                    else
                    {
                        // Create a new file if it doesn't exist
                        var worksheet = package.Workbook.Worksheets.Add("Inventory Backup");
                        worksheet.Cells.LoadFromCollection(inventoryData, true);
                        await File.WriteAllBytesAsync(filePath, package.GetAsByteArray());
                    }
                }

                Console.WriteLine($"Backup updated: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Backup failed: {ex.Message}");
            }
        }

        private List<Stocks> GetRecentInventoryData()
        {
            // Replace with actual database fetching logic
            return _context.Stocks.ToList();

        }
    }
}

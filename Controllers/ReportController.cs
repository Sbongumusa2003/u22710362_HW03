using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using u22710362_HW03.Models;
using Newtonsoft.Json;

namespace u22710362_HW03.Controllers
{
    public class ReportController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // GET: Report
        public async Task<ActionResult> Index()
        {
            // Get report data - Popular Products Report
            var popularProducts = await db.order_items
                .GroupBy(oi => oi.product_id)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalOrders = g.Count()
                })
                .OrderByDescending(x => x.TotalOrders)
                .Take(10)
                .ToListAsync();

            var reportData = new List<ReportDataItem>();
            foreach (var item in popularProducts)
            {
                var product = await db.products
                    .Include(p => p.brands)
                    .Include(p => p.categories)
                    .FirstOrDefaultAsync(p => p.product_id == item.ProductId);

                if (product != null)
                {
                    reportData.Add(new ReportDataItem
                    {
                        Label = product.product_name,
                        Value = item.TotalOrders,
                        Brand = product.brands?.brand_name,
                        Category = product.categories?.category_name
                    });
                }
            }

            ViewBag.ReportData = JsonConvert.SerializeObject(reportData);

            // Get saved files
            var filesPath = Server.MapPath("~/Reports");
            if (!Directory.Exists(filesPath))
            {
                Directory.CreateDirectory(filesPath);
            }

            var files = Directory.GetFiles(filesPath)
                .Select(f => new FileInfo(f))
                .Select(fi => new SavedFileInfo
                {
                    FileName = fi.Name,
                    FileSize = fi.Length,
                    CreatedDate = fi.CreationTime,
                    FilePath = fi.FullName
                })
                .OrderByDescending(f => f.CreatedDate)
                .ToList();

            ViewBag.SavedFiles = files;

            return View();
        }

        // GET: Report/GetSalesReport
        public async Task<ActionResult> GetSalesReport()
        {
            var salesData = await db.order_items
                .Include(oi => oi.products.brands) // Eager loading
                .GroupBy(oi => oi.products.brands.brand_name)
                .Select(g => new
                {
                    Brand = g.Key,
                    TotalSales = g.Sum(oi => oi.quantity * oi.list_price * (1 - oi.discount))
                })
                .OrderByDescending(x => x.TotalSales)
                .ToListAsync();

            return Json(salesData, JsonRequestBehavior.AllowGet);
        }

        // GET: Report/GetCustomerReport
        public async Task<ActionResult> GetCustomerReport()
        {
            var customerData = await db.orders
                .Where(o => o.customer_id.HasValue)
                .GroupBy(o => o.customer_id)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.OrderCount)
                .Take(10)
                .ToListAsync();

            var result = new List<object>();
            foreach (var item in customerData)
            {
                var customer = await db.customers.FindAsync(item.CustomerId);
                if (customer != null)
                {
                    result.Add(new
                    {
                        CustomerName = customer.first_name + " " + customer.last_name,
                        OrderCount = item.OrderCount
                    });
                }
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // POST: Report/SaveReport
        [HttpPost]
        public ActionResult SaveReport(string fileName, string fileType, string reportHtml, string description)
        {
            try
            {
                var filesPath = Server.MapPath("~/Reports");
                if (!Directory.Exists(filesPath))
                {
                    Directory.CreateDirectory(filesPath);
                }

                var fullFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.{fileType}";
                var filePath = Path.Combine(filesPath, fullFileName);

                if (fileType.ToLower() == "html")
                {
                    System.IO.File.WriteAllText(filePath, reportHtml);
                }
                else if (fileType.ToLower() == "txt")
                {
                    // Strip HTML tags for text file
                    var plainText = System.Text.RegularExpressions.Regex.Replace(reportHtml, "<.*?>", string.Empty);
                    System.IO.File.WriteAllText(filePath, plainText);
                }

                // Save description if provided (bonus feature)
                if (!string.IsNullOrEmpty(description))
                {
                    var descriptionPath = Path.Combine(filesPath, fullFileName + ".description.txt");
                    System.IO.File.WriteAllText(descriptionPath, description);
                }

                return Json(new { success = true, message = "Report saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving report: " + ex.Message });
            }
        }

        // GET: Report/DownloadFile
        public ActionResult DownloadFile(string fileName)
        {
            try
            {
                var filesPath = Server.MapPath("~/Reports");
                var filePath = Path.Combine(filesPath, fileName);

                if (System.IO.File.Exists(filePath))
                {
                    var fileBytes = System.IO.File.ReadAllBytes(filePath);
                    var contentType = fileName.EndsWith(".html") ? "text/html" : "text/plain";
                    return File(fileBytes, contentType, fileName);
                }

                return HttpNotFound("File not found");
            }
            catch (Exception ex)
            {
                return Content("Error downloading file: " + ex.Message);
            }
        }

        // POST: Report/DeleteFile
        [HttpPost]
        public ActionResult DeleteFile(string fileName)
        {
            try
            {
                var filesPath = Server.MapPath("~/Reports");
                var filePath = Path.Combine(filesPath, fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);

                    // Delete description file if exists
                    var descriptionPath = filePath + ".description.txt";
                    if (System.IO.File.Exists(descriptionPath))
                    {
                        System.IO.File.Delete(descriptionPath);
                    }

                    return Json(new { success = true, message = "File deleted successfully!" });
                }

                return Json(new { success = false, message = "File not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting file: " + ex.Message });
            }
        }

        // GET: Report/GetFileDescription
        public ActionResult GetFileDescription(string fileName)
        {
            try
            {
                var filesPath = Server.MapPath("~/Reports");
                var descriptionPath = Path.Combine(filesPath, fileName + ".description.txt");

                if (System.IO.File.Exists(descriptionPath))
                {
                    var description = System.IO.File.ReadAllText(descriptionPath);
                    return Json(new { success = true, description = description }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, description = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Helper classes
    public class ReportDataItem
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
    }

    public class SavedFileInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedDate { get; set; }
        public string FilePath { get; set; }
    }
}
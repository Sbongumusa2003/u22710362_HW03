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

        public async Task<ActionResult> Index()
        {
            try
            {
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
                            Brand = product.brands != null ? product.brands.brand_name : "N/A",
                            Category = product.categories != null ? product.categories.category_name : "N/A"
                        });
                    }
                }

                ViewBag.ReportData = JsonConvert.SerializeObject(reportData);
                ViewBag.TotalProducts = await db.products.CountAsync();
                ViewBag.TotalOrders = await db.orders.CountAsync();
                ViewBag.TotalCustomers = await db.customers.CountAsync();
                var filesPath = Server.MapPath("~/Reports");
                if (!Directory.Exists(filesPath))
                {
                    Directory.CreateDirectory(filesPath);
                }

                var files = Directory.GetFiles(filesPath)
                    .Where(f => !f.EndsWith(".description.txt"))
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
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading report: " + ex.Message;
                ViewBag.ReportData = "[]";
                ViewBag.SavedFiles = new List<SavedFileInfo>();
                ViewBag.TotalProducts = 0;
                ViewBag.TotalOrders = 0;
                ViewBag.TotalCustomers = 0;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult SaveReport(string fileName, string fileType, string reportHtml, string description)
        {
            try
            {
                var filesPath = Server.MapPath("~/Reports");
                if (!Directory.Exists(filesPath))
                {
                    Directory.CreateDirectory(filesPath);
                }
                fileName = fileName?.Trim();
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "Report";
                }
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
                var fullFileName = $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.{fileType}";
                var filePath = Path.Combine(filesPath, fullFileName);
                if (fileType.ToLower() == "html")
                {
                    System.IO.File.WriteAllText(filePath, reportHtml);
                }
                else if (fileType.ToLower() == "txt")
                {
                    var plainText = System.Text.RegularExpressions.Regex.Replace(reportHtml, "<.*?>", string.Empty);
                    plainText = System.Net.WebUtility.HtmlDecode(plainText);
                    plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ");
                    plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s*\n\s*", "\n");
                    System.IO.File.WriteAllText(filePath, plainText);
                }
                if (!string.IsNullOrEmpty(description) && !string.IsNullOrWhiteSpace(description))
                {
                    var descriptionPath = Path.Combine(filesPath, fullFileName + ".description.txt");
                    System.IO.File.WriteAllText(descriptionPath, description);
                }

                TempData["SuccessMessage"] = "Report saved successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error saving report: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

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

                TempData["ErrorMessage"] = "File not found";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error downloading file: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFile(string fileName)
        {
            try
            {
                var filesPath = Server.MapPath("~/Reports");
                var filePath = Path.Combine(filesPath, fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);

                    var descriptionPath = filePath + ".description.txt";
                    if (System.IO.File.Exists(descriptionPath))
                    {
                        System.IO.File.Delete(descriptionPath);
                    }

                    TempData["SuccessMessage"] = "File deleted successfully!";
                    return RedirectToAction("Index");
                }

                TempData["ErrorMessage"] = "File not found";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting file: " + ex.Message;
                return RedirectToAction("Index");
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
}
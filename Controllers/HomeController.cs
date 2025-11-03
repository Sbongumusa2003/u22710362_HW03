using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using u22710362_HW03.Models;

namespace u22710362_HW03.Controllers
{
    public class HomeController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        public async Task<ActionResult> Index(int? brandFilter, int? categoryFilter)
        {
            try
            {
                ViewBag.Brands = await db.brands.OrderBy(b => b.brand_name).ToListAsync();
                ViewBag.Categories = await db.categories.OrderBy(c => c.category_name).ToListAsync();
                ViewBag.Stores = await db.stores.OrderBy(s => s.store_name).ToListAsync();

                ViewBag.BrandFilter = brandFilter;
                ViewBag.CategoryFilter = categoryFilter;

                var productsQuery = db.products
                    .Include(p => p.brands)
                    .Include(p => p.categories)
                    .AsQueryable();

                if (brandFilter.HasValue && brandFilter.Value > 0)
                {
                    productsQuery = productsQuery.Where(p => p.brand_id == brandFilter.Value);
                }

                if (categoryFilter.HasValue && categoryFilter.Value > 0)
                {
                    productsQuery = productsQuery.Where(p => p.category_id == categoryFilter.Value);
                }

                var model = new HomeViewModel
                {
                    Staffs = await db.staffs
                        .Include(s => s.stores)
                        .Include(s => s.staffs2)
                        .OrderBy(s => s.last_name)
                        .ThenBy(s => s.first_name)
                        .ToListAsync(),
                    Customers = await db.customers
                        .OrderBy(c => c.last_name)
                        .ThenBy(c => c.first_name)
                        .ToListAsync(),
                    Products = await productsQuery
                        .OrderBy(p => p.product_name)
                        .ToListAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading data: " + ex.Message;
                return View(new HomeViewModel
                {
                    Staffs = new List<staffs>(),
                    Customers = new List<customers>(),
                    Products = new List<products>()
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateStaff([Bind(Include = "first_name,last_name,email,phone,active,store_id,manager_id")] staffs staff)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var storeExists = await db.stores.AnyAsync(s => s.store_id == staff.store_id);
                    if (!storeExists)
                    {
                        TempData["ErrorMessage"] = "Invalid store selected";
                        return RedirectToAction("Index");
                    }
                    if (staff.manager_id.HasValue)
                    {
                        var managerExists = await db.staffs.AnyAsync(s => s.staff_id == staff.manager_id.Value);
                        if (!managerExists)
                        {
                            TempData["ErrorMessage"] = "Invalid manager selected";
                            return RedirectToAction("Index");
                        }
                    }
                    db.staffs.Add(staff);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Staff member created successfully!";
                    return RedirectToAction("Index");
                }
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", errors);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating staff: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateCustomer([Bind(Include = "first_name,last_name,phone,email,street,city,state,zip_code")] customers customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (!string.IsNullOrEmpty(customer.email) && !customer.email.Contains("@"))
                    {
                        TempData["ErrorMessage"] = "Invalid email format";
                        return RedirectToAction("Index");
                    }
                    db.customers.Add(customer);
                    await db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Customer created successfully!";
                    return RedirectToAction("Index");
                }

                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", errors);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating customer: " + ex.Message;
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
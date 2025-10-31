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

        // GET: Home
        public async Task<ActionResult> Index()
        {
            ViewBag.Brands = await db.brands.ToListAsync();
            ViewBag.Categories = await db.categories.ToListAsync();
            ViewBag.Stores = await db.stores.ToListAsync();

            var model = new HomeViewModel
            {
                Staffs = await db.staffs.Include(s => s.stores).ToListAsync(),
                Customers = await db.customers.ToListAsync(),
                Products = await db.products.Include(p => p.brands).Include(p => p.categories).ToListAsync()
            };

            return View(model);
        }

        // POST: Home/CreateStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateStaff([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staff)
        {
            if (ModelState.IsValid)
            {
                db.staffs.Add(staff);
                await db.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // POST: Home/CreateCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateCustomer([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customer)
        {
            if (ModelState.IsValid)
            {
                db.customers.Add(customer);
                await db.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // GET: Home/FilterProducts
        public async Task<ActionResult> FilterProducts(int? brandId, int? categoryId)
        {
            var products = db.products.Include(p => p.brands).Include(p => p.categories).AsQueryable();

            if (brandId.HasValue && brandId.Value > 0)
            {
                products = products.Where(p => p.brand_id == brandId.Value);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = products.Where(p => p.category_id == categoryId.Value);
            }

            var result = await products.Select(p => new
            {
                product_id = p.product_id,
                product_name = p.product_name,
                brand_name = p.brands.brand_name,
                category_name = p.categories.category_name,
                model_year = p.model_year,
                list_price = p.list_price
            }).ToListAsync();

            return Json(result, JsonRequestBehavior.AllowGet);
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
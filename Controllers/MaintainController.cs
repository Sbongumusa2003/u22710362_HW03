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
    public class MaintainController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        // GET: Maintain
        public async Task<ActionResult> Index()
        {
            ViewBag.Brands = await db.brands.ToListAsync();
            ViewBag.Categories = await db.categories.ToListAsync();
            ViewBag.Stores = await db.stores.ToListAsync();
            ViewBag.Managers = await db.staffs.ToListAsync();

            var model = new HomeViewModel
            {
                Staffs = await db.staffs.Include(s => s.stores).ToListAsync(),
                Customers = await db.customers.ToListAsync(),
                Products = await db.products.Include(p => p.brands).Include(p => p.categories).ToListAsync()
            };

            return View(model);
        }

        // GET: Maintain/GetStaff/5
        [HttpGet]
        public async Task<ActionResult> GetStaff(int id)
        {
            try
            {
                var staff = await db.staffs.FindAsync(id);
                if (staff == null)
                {
                    return Json(new { success = false, message = "Staff not found" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new
                {
                    success = true,
                    staff_id = staff.staff_id,
                    first_name = staff.first_name,
                    last_name = staff.last_name,
                    email = staff.email,
                    phone = staff.phone,
                    active = staff.active,
                    store_id = staff.store_id,
                    manager_id = staff.manager_id
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Maintain/EditStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditStaff([Bind(Include = "staff_id,first_name,last_name,email,phone,active,store_id,manager_id")] staffs staff)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(staff).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Staff updated successfully!" });
                }
                return Json(new { success = false, message = "Validation failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Maintain/DeleteStaff/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteStaff(int id)
        {
            try
            {
                staffs staff = await db.staffs.FindAsync(id);
                if (staff != null)
                {
                    db.staffs.Remove(staff);
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Staff deleted successfully!" });
                }
                return Json(new { success = false, message = "Staff not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: Maintain/GetCustomer/5
        [HttpGet]
        public async Task<ActionResult> GetCustomer(int id)
        {
            try
            {
                var customer = await db.customers.FindAsync(id);
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new
                {
                    success = true,
                    customer_id = customer.customer_id,
                    first_name = customer.first_name,
                    last_name = customer.last_name,
                    email = customer.email,
                    phone = customer.phone,
                    street = customer.street,
                    city = customer.city,
                    state = customer.state,
                    zip_code = customer.zip_code
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Maintain/EditCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditCustomer([Bind(Include = "customer_id,first_name,last_name,phone,email,street,city,state,zip_code")] customers customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(customer).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Customer updated successfully!" });
                }
                return Json(new { success = false, message = "Validation failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Maintain/DeleteCustomer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            try
            {
                customers customer = await db.customers.FindAsync(id);
                if (customer != null)
                {
                    db.customers.Remove(customer);
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Customer deleted successfully!" });
                }
                return Json(new { success = false, message = "Customer not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: Maintain/GetProduct/5
        [HttpGet]
        public async Task<ActionResult> GetProduct(int id)
        {
            try
            {
                var product = await db.products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" }, JsonRequestBehavior.AllowGet);
                }
                return Json(new
                {
                    success = true,
                    product_id = product.product_id,
                    product_name = product.product_name,
                    brand_id = product.brand_id,
                    category_id = product.category_id,
                    model_year = product.model_year,
                    list_price = product.list_price
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Maintain/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProduct([Bind(Include = "product_id,product_name,brand_id,category_id,model_year,list_price")] products product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(product).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Product updated successfully!" });
                }
                return Json(new { success = false, message = "Validation failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Maintain/DeleteProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            try
            {
                products product = await db.products.FindAsync(id);
                if (product != null)
                {
                    db.products.Remove(product);
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Product deleted successfully!" });
                }
                return Json(new { success = false, message = "Product not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
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
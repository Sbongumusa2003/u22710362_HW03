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
            try
            {
                // Populate ViewBag for dropdown filters and modals
                ViewBag.Brands = await db.brands.OrderBy(b => b.brand_name).ToListAsync();
                ViewBag.Categories = await db.categories.OrderBy(c => c.category_name).ToListAsync();
                ViewBag.Stores = await db.stores.OrderBy(s => s.store_name).ToListAsync();

                // Create the view model with all necessary data
                var model = new HomeViewModel
                {
                    // Load staff with eager loading for related entities
                    Staffs = await db.staffs
                        .Include(s => s.stores)
                        .Include(s => s.staffs2) // Manager relationship
                        .OrderBy(s => s.last_name)
                        .ThenBy(s => s.first_name)
                        .ToListAsync(),

                    // Load customers
                    Customers = await db.customers
                        .OrderBy(c => c.last_name)
                        .ThenBy(c => c.first_name)
                        .ToListAsync(),

                    // Load products with brands and categories
                    Products = await db.products
                        .Include(p => p.brands)
                        .Include(p => p.categories)
                        .OrderBy(p => p.product_name)
                        .ToListAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error (you can use a logging framework)
                ViewBag.ErrorMessage = "Error loading data: " + ex.Message;
                return View(new HomeViewModel
                {
                    Staffs = new List<staffs>(),
                    Customers = new List<customers>(),
                    Products = new List<products>()
                });
            }
        }

        // POST: Home/CreateStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateStaff([Bind(Include = "first_name,last_name,email,phone,active,store_id,manager_id")] staffs staff)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validate that store exists
                    var storeExists = await db.stores.AnyAsync(s => s.store_id == staff.store_id);
                    if (!storeExists)
                    {
                        return Json(new { success = false, message = "Invalid store selected" });
                    }

                    // Validate manager if provided
                    if (staff.manager_id.HasValue)
                    {
                        var managerExists = await db.staffs.AnyAsync(s => s.staff_id == staff.manager_id.Value);
                        if (!managerExists)
                        {
                            return Json(new { success = false, message = "Invalid manager selected" });
                        }
                    }

                    // Add staff to database
                    db.staffs.Add(staff);
                    await db.SaveChangesAsync();

                    return Json(new { success = true, message = "Staff member created successfully!" });
                }

                // Collect validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new
                {
                    success = false,
                    message = "Validation failed: " + string.Join(", ", errors)
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error creating staff: " + ex.Message
                });
            }
        }

        // POST: Home/CreateCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateCustomer([Bind(Include = "first_name,last_name,phone,email,street,city,state,zip_code")] customers customer)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validate email format (basic check)
                    if (!string.IsNullOrEmpty(customer.email) && !customer.email.Contains("@"))
                    {
                        return Json(new { success = false, message = "Invalid email format" });
                    }

                    // Add customer to database
                    db.customers.Add(customer);
                    await db.SaveChangesAsync();

                    return Json(new { success = true, message = "Customer created successfully!" });
                }

                // Collect validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new
                {
                    success = false,
                    message = "Validation failed: " + string.Join(", ", errors)
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error creating customer: " + ex.Message
                });
            }
        }

        // GET: Home/FilterProducts
        [HttpGet]
        public async Task<ActionResult> FilterProducts(int? brandId, int? categoryId)
        {
            try
            {
                // Start with all products
                var products = db.products
                    .Include(p => p.brands)
                    .Include(p => p.categories)
                    .AsQueryable();

                // Apply brand filter if provided and not 0
                if (brandId.HasValue && brandId.Value > 0)
                {
                    products = products.Where(p => p.brand_id == brandId.Value);
                }

                // Apply category filter if provided and not 0
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    products = products.Where(p => p.category_id == categoryId.Value);
                }

                // Order results
                products = products.OrderBy(p => p.product_name);

                // Execute query and project to anonymous type for JSON
                var result = await products.Select(p => new
                {
                    product_id = p.product_id,
                    product_name = p.product_name,
                    brand_name = p.brands.brand_name,
                    brand_id = p.brand_id,
                    category_name = p.categories.category_name,
                    category_id = p.category_id,
                    model_year = p.model_year,
                    list_price = p.list_price
                }).ToListAsync();

                return Json(new
                {
                    success = true,
                    products = result
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error filtering products: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Home/GetStaffOrders/{staffId}
        [HttpGet]
        public async Task<ActionResult> GetStaffOrders(int staffId)
        {
            try
            {
                var orders = await db.orders
                    .Where(o => o.staff_id == staffId)
                    .Include(o => o.customers)
                    .OrderByDescending(o => o.order_date)
                    .Take(10) // Limit to most recent 10 orders
                    .Select(o => new
                    {
                        order_id = o.order_id,
                        customer_name = o.customers.first_name + " " + o.customers.last_name,
                        order_date = o.order_date,
                        order_status = o.order_status
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    orders = orders
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error loading staff orders: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Home/GetCustomerOrders/{customerId}
        [HttpGet]
        public async Task<ActionResult> GetCustomerOrders(int customerId)
        {
            try
            {
                var orders = await db.orders
                    .Where(o => o.customer_id == customerId)
                    .Include(o => o.stores)
                    .OrderByDescending(o => o.order_date)
                    .Take(10) // Limit to most recent 10 orders
                    .Select(o => new
                    {
                        order_id = o.order_id,
                        store_name = o.stores.store_name,
                        order_date = o.order_date,
                        order_status = o.order_status
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    orders = orders
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error loading customer orders: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Home/GetProductDetails/{productId}
        [HttpGet]
        public async Task<ActionResult> GetProductDetails(int productId)
        {
            try
            {
                var product = await db.products
                    .Include(p => p.brands)
                    .Include(p => p.categories)
                    .Where(p => p.product_id == productId)
                    .Select(p => new
                    {
                        product_id = p.product_id,
                        product_name = p.product_name,
                        brand_name = p.brands.brand_name,
                        brand_id = p.brand_id,
                        category_name = p.categories.category_name,
                        category_id = p.category_id,
                        model_year = p.model_year,
                        list_price = p.list_price
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Product not found"
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    product = product
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error loading product details: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
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
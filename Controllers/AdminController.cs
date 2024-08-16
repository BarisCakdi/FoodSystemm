using Dapper;
using FoodSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Reflection;

namespace FoodSystem.Controllers
{
    public class AdminController : Controller
    {

        public bool CheckLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return false;
            }
            return true;
        }
        public IActionResult Index(string? MessageCssClass, string? Message)
        {
            if (!CheckLogin())
            {
                ViewBag.Message = "Bro?? Login ol!!";
                ViewBag.MessageCssClass = "alert-danger";
                return View("Message");
            }
            ViewBag.SepetUrunSayisi = GetSepetUrunSayisi();
            ViewData["username"] = HttpContext.Session.GetString("username");

            int? userId = HttpContext.Session.GetInt32("Id");

            if (userId == null)
            {
                ViewBag.mesaj = "Kullanıcı kimliği bulunamadı. Lütfen tekrar giriş yapın.";
                return View("index");
            }

            ViewData["Title"] = "Admin Panel";
            using var connection = new SqlConnection(connectionString);
            var products = connection.Query<Product>("SELECT * FROM products").ToList();
            var categories = connection.Query<Category>("SELECT * FROM category").ToList();
            var viewModel = new ProductCategoryModel
            {
                products = products,
                categories = categories,
            };

            ViewBag.Message = Message;
            ViewBag.MessageCssClass = MessageCssClass;
            return View(viewModel);
        }
        [HttpPost]
        public IActionResult Add(Product model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Ürün eklenirken bir hata oluştu";
                ViewBag.MessageCssClass = "alert-danger";
                return View("Message");
            }

            if (model.Img != null && model.Img.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Img.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                model.Img.CopyTo(fileStream);
                model.ImgPath = fileName;
            }
            model.CreatedDate = DateTime.Now;
            using var connection = new SqlConnection(connectionString);
            var sql = "INSERT INTO products (Name, Description, Price, Stock, ImgPath, CreatedDate, CategoryId) VALUES (@Name,@Description, @Price, @Stock, @ImgPath, @CreatedDate, @CategoryId)";
            var data = new
            {
                model.Name,
                model.Description,
                model.Price,
                model.Stock,
                model.ImgPath,
                model.CreatedDate,
                model.CategoryId,
            };
            var rowsAffected = connection.Execute(sql, data);
            ViewData["username"] = HttpContext.Session.GetString("username");
            ViewBag.Message = "Ürün eklendi.";
            ViewBag.MessageCssClass = "alert-success";
            return View("Message");
        }

        [HttpPost]
        public IActionResult Edit(Product model, string ExistingImgPath)
        {
            if (model.Img != null && model.ImgPath.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Img.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                model.Img.CopyTo(fileStream);
                model.ImgPath = fileName;
            }
            else
            {
                model.ImgPath = ExistingImgPath;
            }

            using var connection = new SqlConnection(connectionString);

            var sqlUpdate = "UPDATE products SET Name=@Name, Description=@Description, Price=@Price, Stock=@Stock, ImgPath=@ImgPath, CategoryId=@CategoryId WHERE Id = @Id";
            var param = new
            {
                model.Name,
                model.Description,
                model.Price,
                model.Stock,
                model.ImgPath,
                model.CategoryId,
                model.Id
            };

            var affectedRows = connection.Execute(sqlUpdate, param);
            ViewData["username"] = HttpContext.Session.GetString("username");

            ViewBag.Message = "Ürün güncellendi.";
            ViewBag.MessageCssClass = "alert-success";
            return View("Message");
        }

        public IActionResult Delete(int id)
        {
            using var connection = new SqlConnection(connectionString);
            var sql = "DELETE from products WHERE Id = @id";
            var rowAffected = connection.Execute(sql, new { id });
            return RedirectToAction("index");

        }

        [HttpPost]
        public IActionResult AddCategry(Category model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Eksik veya hatalı işlem yaptın";
                return View("Message");
            }
            using var connection = new SqlConnection(connectionString);
            var cate = "INSERT INTO category (Name) VALUES (@Name)";
            var data = new
            {
                model.Name
            };
            var rowsAffected = connection.Execute(cate, data);
            ViewData["username"] = HttpContext.Session.GetString("username");

            ViewBag.MessageCssClass = "alert-success";
            ViewBag.Message = "Eklendi";
            return View("Message");
        }

        public IActionResult DelCate(int id)
        {
            using var connection = new SqlConnection(connectionString);
            var sql = "DELETE FROM category WHERE Id = @Id";
            var rowsAffected = connection.Execute(sql, new { id });
            return RedirectToAction("index");
        }

        private int GetSepetUrunSayisi()
        {
            using var connection = new SqlConnection(connectionString);
            var userId = HttpContext.Session.GetInt32("Id");

            var sepetUrunSayisi = connection.Query<int>("SELECT COUNT(UrunAdet) FROM sepet WHERE UserId = @UserId", new { UserId = userId }).FirstOrDefault();
            return sepetUrunSayisi;
        }





    }
}

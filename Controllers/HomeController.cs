using Dapper;
using FoodSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FoodSystem.Controllers
{
    public class HomeController : Controller
    {
        public bool CheckLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return false;
            }
            return true;
        }
        public IActionResult Login(string? redirectUrl)
        {
            ViewBag.AuthError = TempData["AuthError"] as string;
            ViewBag.RedirectUrl = redirectUrl;
            return View(new Register());
        }

        [HttpPost]
        [Route("/giris")]
        public IActionResult Giris(Register model)
        {
            if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.Password))
            {
                TempData["AuthError"] = "Kullanıcı adı veya şifre boş olamaz";
                return RedirectToAction("login");
            }

            using var connection = new SqlConnection(connectionString);
            var sql = "SELECT * FROM users WHERE UserName = @UserName AND Password = @Password";
            var user = connection.QuerySingleOrDefault<Register>(sql, new { model.UserName, Password = Helper.Hash(model.Password) });

            if (user != null)
            {
                // UserName ve Id oturuma kaydediliyor
                HttpContext.Session.SetString("username", user.UserName);
                HttpContext.Session.SetInt32("Id", user.Id);

                if (!string.IsNullOrEmpty(model.RedirectUrl))
                {
                    return Redirect(model.RedirectUrl);
                }

                return RedirectToAction("Index");
            }

            TempData["AuthError"] = "Kullanıcı adı veya şifre hatalı";
            return RedirectToAction("login");
        }

        public IActionResult Cikis()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("login");
        }
        public IActionResult Register()
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            ViewBag.AuthError = TempData["AuthError"] as string;
            return View(new Register());
        }

        [HttpPost]
        [Route("/kayit")]
        public IActionResult Kayit(Register model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Eksik alan bulunuyor!";
                return View("register", model);
            }
            if (!string.Equals(model.Password, model.RepeatPass))
            {
                ViewBag.ErrorMessage = "Şifreler uyuşmuyor!";
                return View("register", model);
            }

            using var connection = new SqlConnection(connectionString);
            var login = connection.QueryFirstOrDefault<Register>("SELECT * FROM users WHERE UserName = @UserName", new { model.UserName });
            if (login != null)
            {
                ViewBag.ErrorMessage = "Kullanıcı adı mevcut";
                return View("register", model);
            }
            var checkmail = connection.QueryFirstOrDefault<Register>("SELECT * FROM users WHERE Email = @Email", new {model.Email});
            if (checkmail != null)
            {
                ViewBag.ErrorMessage = "Bu Mail Kullanılmakta";
                return View("register", model);
            }
            model.CreatedDate = DateTime.Now;
            var sql = "INSERT INTO users (UserName, LastName, Password, Email, PhoneNumber, Address, CreatedDate) VALUES (@UserName, @LastName, @Password, @Email, @PhoneNumber, @Address, @CreatedDate)";
            var data = new
            {
                model.UserName,
                model.LastName,
                Password = Helper.Hash(model.Password),
                model.Email,
                model.PhoneNumber,
                model.Address,
                model.CreatedDate
                
            };
            var rowsAffected = connection.Execute(sql, data);

            


            ViewBag.SuccessMessage = "Kayıt bilgileri mail adresinize gönderilmiştir";
            ViewBag.Message = $"Hoş geldin '{model.UserName}' giriş yapıp sipariş oluşturabilirsin ";
            ViewBag.MessageCssClass = "alert-success";
            return View("Message");


        }
        public int? UserIdGetir(string username)
        {
            using var connection = new SqlConnection(connectionString);
            var sql = "SELECT Id FROM users WHERE UserName = @username";
            var userId = connection.QueryFirstOrDefault<int?>(sql, new { UserName = username });
            return userId;
        }
        public IActionResult Profile(string? username)
        {
            ViewData["username"] = HttpContext.Session.GetString("username");

            if (!CheckLogin())
            {
                ViewBag.Message = "Bro?? Login ol!!";
                ViewBag.MessageCssClass = "alert-danger";
                return View("Message");
            }
            ViewBag.SepetUrunSayisi = GetSepetUrunSayisi();

            int? userId = UserIdGetir(username);

            if (userId == null || userId != HttpContext.Session.GetInt32("Id"))// kullanıcı yoksa bunu gösteriyorm
            {
                ViewBag.MessageCssClass = "alert-alert";
                ViewBag.Message = "Böyle bir kullanıcı yok!.";
                return View("Message");
            }
            using var connection = new SqlConnection(connectionString);
            var user = connection.QueryFirstOrDefault<Register>( "SELECT * FROM users WHERE UserName = @UserName", new {UserName = username});
            
            return View(user);
        }

        [HttpPost]
        [Route("UserEdit/{id}")]
        public IActionResult UserEdit(Register model)
        {
            if (string.IsNullOrEmpty(model.UserName))
            {
                return RedirectToAction("Profile", new { model.UserName });
            }

            using var connection = new SqlConnection(connectionString);

            // Kullanıcı adı kontrolü
            var login = connection.QueryFirstOrDefault<Register>("SELECT * FROM users WHERE UserName = @UserName", new {model.UserName});
            if (login != null)
            {
                ViewData["username"] = HttpContext.Session.GetString("username");
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Bu İsim mevcut.";
                return View("Message");
            }

            var sql = "UPDATE users SET UserName = @UserName WHERE Id = @Id";
            var param = new
            {
                model.UserName,
                model.Password,
                model.ImgPath,
                model.Id,
            };
            var rowAffected = connection.Execute(sql, param);

            // Session güncellemesi
            HttpContext.Session.SetString("username", model.UserName);
            ViewData["username"] = HttpContext.Session.GetString("username");
            ViewBag.Message = "isminiz güncellenmiştir.";
            ViewBag.MessageCssClass = "alert-success";
            return View("Message");
        }

        [HttpPost]
        [Route("PassEdit/{id}")]
        public IActionResult PassEdit(Register model)
        {
            if (model.Password != model.RepeatPass)
            {
                ViewData["username"] = HttpContext.Session.GetString("username");
                ViewBag.MessageCssClass = "alert-danger";
                ViewBag.Message = "Şifreler uyuşmuyor.";
                return View("Message",model);

            }
            if (string.IsNullOrEmpty(model.Password))
            {
                return RedirectToAction("UserEdit", new { model.UserName });
            }

            using var connection = new SqlConnection(connectionString);
            var sql = "UPDATE users SET Password = @Password WHERE Id = @Id";
            model.Password = Helper.Hash(model.Password);
            var data = new
            {
                model.Password,
                model.Id
            };
            var rowAffected = connection.Execute(sql, data);
            ViewData["username"] = HttpContext.Session.GetString("username");

            ViewBag.Message = "Şifreniz güncellenmiştir.";
            ViewBag.MessageCssClass = "alert-success";
            return View("Message");
        }
        [HttpPost]
        [Route("FotoEdit/{id}")]
        public IActionResult FotoEdit(Register model)
        {
            if (model.Img == null || model.Img.Length == 0)
            {
                return RedirectToAction("UserEdit", new { model.UserName });
            }

            using var connection = new SqlConnection(connectionString);
            var sql = "UPDATE users SET ImgPath = @ImgPath WHERE Id = @Id";
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Img.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            model.Img.CopyTo(fileStream); 
            model.ImgPath = fileName;

            var data = new
            {
                model.ImgPath,
                model.Id
            };
            var rowAffected = connection.Execute(sql, data);
            ViewData["username"] = HttpContext.Session.GetString("username");

            ViewBag.Message = "Fotorafınız güncellenmiştir.";
            ViewBag.MessageCssClass = "alert-success";
            return View("Message");
        }

        [HttpPost]
        [Route("AdresEdit/{id}")]
        public IActionResult AdresEdit(Register model)
        {
            using var connection = new SqlConnection(connectionString);
                var sql = "UPDATE users SET Address = @Address WHERE Id = @Id";
            var data = new
            {
                model.Address,
                model.Id,
            };

            var rowAffected = connection.Execute(sql, data);
            ViewData["username"] = HttpContext.Session.GetString("username");

            ViewBag.Message = "Adresiniz güncellenmiştir.";
            ViewBag.MessageCssClass = "alert-success";
            return View("Message");
        }
        private int GetSepetUrunSayisi()
        {
            using var connection = new SqlConnection(connectionString);
            var userId = HttpContext.Session.GetInt32("Id");

            var sepetUrunSayisi = connection.Query<int>("SELECT COUNT(UrunAdet) FROM sepet WHERE UserId = @UserId", new { UserId = userId }).FirstOrDefault();
            return sepetUrunSayisi;
        }
        public IActionResult Index(string? MessageCssClass, string? Message, int? categoryId)
        {
            ViewData["Title"] = "Ana Sayfa";
            ViewData["username"] = HttpContext.Session.GetString("username");
            ViewData["Id"] = HttpContext.Session.GetInt32("Id");
            ViewBag.SepetUrunSayisi = GetSepetUrunSayisi();
            int? userId = HttpContext.Session.GetInt32("Id");


            List<Product> products;
            using var connection = new SqlConnection(connectionString);

            if (categoryId.HasValue)
            {
                products = connection.Query<Product>("SELECT * FROM products WHERE CategoryId = @CategoryId", new { CategoryId = categoryId.Value }).ToList();
            }
            else
            {
                products = connection.Query<Product>("SELECT * FROM products").ToList();
            }

            var categories = connection.Query<Category>("SELECT * FROM category").ToList();

            var viewModel = new ProductCategoryModel
            {
                products = products,
                categories = categories,
                SelectCateId = categoryId,
            };

            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Message = Message;
            ViewBag.MessageCssClass = MessageCssClass;

            return View(viewModel);
        }

        public IActionResult Categories(int id)
        {
            using var connection = new SqlConnection(connectionString);
            var sql = "SELECT * FROM products WHERE Id = @Id";
            var post = connection.QuerySingleOrDefault<Product>(sql, new { id });
            return View(post);
        }
        public IActionResult Menu()
        {
            return View();
        }
       public IActionResult About()
        {
            return View();
        }

     
    }
}

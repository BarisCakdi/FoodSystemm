using Dapper;
using FoodSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FoodSystem.Controllers
{
    public class SepetController : Controller
    {
        public bool CheckLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return false;
            }
            return true;
        }
        private int GetSepetUrunSayisi()
        {
            using var connection = new SqlConnection(connectionString);
            var userId = HttpContext.Session.GetInt32("Id");

            var sepetUrunSayisi = connection.Query<int>("SELECT COUNT(UrunAdet) FROM sepet WHERE UserId = @UserId", new { UserId = userId }).FirstOrDefault();
            return sepetUrunSayisi;
        }

        public IActionResult Index2()
        {
            if (!CheckLogin())
            {
                ViewBag.Message = "Bro?? Login ol!!";
                ViewBag.MessageCssClass = "alert-danger";
                return View("Message");
            }
            ViewBag.SepetUrunSayisi = GetSepetUrunSayisi();

            ViewData["username"] = HttpContext.Session.GetString("username");
            var userId = HttpContext.Session.GetInt32("Id");

            if (userId == null)
            {
                ViewBag.mesaj = "Kullanıcı kimliği bulunamadı. Lütfen tekrar giriş yapın.";
                return View("index");
            }
            ViewData["Title"] = "Sepet";

            using var connection = new SqlConnection(connectionString);
            var sepetItems = connection.Query<Sepet>("SELECT * FROM sepet WHERE UserId = @UserId", new { UserId = userId }).ToList();
            var totalAmount = sepetItems.Sum(sepetItems => sepetItems.UrunFiyat * sepetItems.UrunAdet);
            ViewData["TotalAmount"] = totalAmount;
            if (!sepetItems.Any())
            {
                ViewBag.Message = "Sepetinizde ürün bulunmamaktadır.";
                return View("Message");
            }

            return View("Index2",sepetItems);
        }

        [HttpPost]
        public IActionResult Sepet(int Id)
        {
            var userId = HttpContext.Session.GetInt32("Id");
            var userName = HttpContext.Session.GetString("username");

            if (userId == null || string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "home");
            }

            using var connection = new SqlConnection(connectionString);

            var product = connection.QueryFirstOrDefault<Product>("SELECT * FROM products WHERE Id = @Id", new { Id });

            if (product.Stock == 0)
            {
                ViewBag.Message = "Ürün stokta kalmamıştır.";
                ViewBag.MessageCssClass = "alert-danger";
                return View("Message");
            }

            var existingItem = connection.QueryFirstOrDefault<Sepet>("SELECT * FROM sepet WHERE UrunId = @Id AND UserId = @UserId", new { Id, UserId = userId });
            if (existingItem != null)
            {
                existingItem.UrunAdet++;
                var sqlUpdate = "UPDATE sepet SET UrunAdet = @UrunAdet WHERE UrunId = @UrunId AND UserId = @UserId";
                connection.Execute(sqlUpdate, new { existingItem.UrunAdet, UrunId = Id, UserId = userId });
            }
            else
            {
                var sqlInsert = "INSERT INTO sepet (UrunId, UrunAd, UrunFiyat, UrunAdet, UserId) VALUES (@UrunId, @UrunAd, @UrunFiyat, @UrunAdet, @UserId)";
                var data = new
                {
                    UrunId = Id,
                    UrunAd = product.Name,
                    UrunFiyat = product.Price,
                    UrunAdet = 1,
                    UserId = userId
                };
                connection.Execute(sqlInsert, data);
            }

            product.Stock--;
            var sqlStock = "UPDATE products SET Stock = @Stock WHERE Id = @Id";
            connection.Execute(sqlStock, new { product.Stock, Id });

            return RedirectToAction("Index", "Home", new { MessageCssClass = "alert-success", Message = $"{product.Name} sepete eklenmiştir." });
        }

        public IActionResult SepetSil(int id)
        {
            using var connection = new SqlConnection(connectionString);
            var item = connection.QueryFirstOrDefault<Sepet>("SELECT * FROM sepet WHERE Id = @Id", new { Id = id });
            if (item != null)
            {
                var product = connection.QueryFirstOrDefault<Product>("SELECT * FROM products WHERE Id = @Id", new { Id = item.UrunId });
                if (product != null)
                {
                    if (item.UrunAdet > 1)
                    {
                        item.UrunAdet--;
                        var sqlUpdateSepet = "UPDATE sepet SET UrunAdet = @UrunAdet WHERE Id = @Id";
                        connection.Execute(sqlUpdateSepet, new { item.UrunAdet, Id = id });
                        product.Stock++;
                        var sqlUpdateProduct = "UPDATE products SET Stock = @Stock WHERE Id = @Id";
                        connection.Execute(sqlUpdateProduct, new { product.Stock, product.Id });
                    }
                    else
                    {
                        var sqlDelete = "DELETE FROM sepet WHERE Id = @Id";
                        connection.Execute(sqlDelete, new { Id = id });

                        product.Stock++;
                        var sqlUpdateProduct = "UPDATE product SET Stock = @Stock WHERE Id = @Id";
                        connection.Execute(sqlUpdateProduct, new { product.Stock, product.Id });
                    }
                }
            }
            return RedirectToAction("index2");
        }

        [HttpPost]
        public IActionResult Odeme(int id)
        {
            var userId = HttpContext.Session.GetInt32("Id");
            using var connection = new SqlConnection(connectionString);
            var sqlDeleteCart = "DELETE FROM sepet WHERE UserId = @UserId";
            connection.Execute(sqlDeleteCart, new { UserId = userId });
            ViewBag.Message = "Ürün stokta kalmamıştır.";
            ViewBag.MessageCssClass = "alert-danger";
            return View("Message");
        }
    }
}

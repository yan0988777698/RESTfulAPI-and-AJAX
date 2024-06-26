﻿using Microsoft.AspNetCore.Mvc;
using projAJAX.DTO;
using projRESTfulAPIandAJAX.Models;
using System.Collections;
using System.Net;
using System.Text;
using System.Text.Unicode;
using System.Xml.Linq;

namespace projRESTfulAPIandAJAX.Controllers
{
    public class ApiController : Controller
    {
        private readonly MyDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        public ApiController(MyDbContext dbContext, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _env = env;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult ReturnContent()
        {
            System.Threading.Thread.Sleep(3000);
            return Content("Hello Fetch!!", "text/plain", Encoding.UTF8);
        }
        public IActionResult ReturnJson()
        {
            var cities = _dbContext.Addresses.Select(x => x.City).Distinct();
            return Json(cities);
        }
        //圖片上傳與儲存
        public IActionResult Register(Member member, IFormFile img)
        {
            string info = $"{img.FileName} - {img.Length} - {img.ContentType}";
            string path = Path.Combine(_env.WebRootPath, "uploads", img.FileName);
            byte[] data;
            //圖片存入資料夾
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                img.CopyTo(stream);
            }
            //圖片轉成二進位
            using (MemoryStream memoryStream = new MemoryStream())
            {
                img.CopyTo(memoryStream);
                data = memoryStream.ToArray();
            }
            //會員資料寫入資料庫
            member.FileName = img.FileName;
            member.FileData = data;
            _dbContext.Members.Add(member);
            _dbContext.SaveChanges();
            return Content(info, "text/plain", Encoding.UTF8);
        }
        public IActionResult CheckUsername(string name)
        {
            bool check = _dbContext.Members.Any(x => x.Name.Equals(name));
            return Content(check.ToString(), "text/plain", Encoding.UTF8);
        }
        //接收JSON檔案時，需要[FromBody]+類別(其屬性與JSON鍵值對應)
        [HttpPost]
        public IActionResult GetSpot([FromBody] CSpotDTO cSpotDTO)
        {
            var spots = cSpotDTO.categoryId == 0 ?
                _dbContext.SpotImagesSpots :
                _dbContext.SpotImagesSpots.Where(x => x.CategoryId == cSpotDTO.categoryId);
            if (!string.IsNullOrEmpty(cSpotDTO.keyword))
            {
                spots = spots.Where(
                    x => x.SpotTitle.Contains(cSpotDTO.keyword) ||
                    x.SpotDescription.Contains(cSpotDTO.keyword));
            }
            int totalCount = spots.Count();
            int pageSize = cSpotDTO.pageSize;
            int page = cSpotDTO.page;
            int totalPages = (int)Math.Ceiling((decimal)totalCount / pageSize);
            spots = spots.Skip((page - 1) * pageSize).Take(pageSize);
            switch (cSpotDTO.sortBy)
            {
                case "spotTitle":
                    spots = cSpotDTO.sortType == "asc" ? spots.OrderBy(x => x.SpotTitle) : spots.OrderByDescending(x => x.SpotTitle);
                    break;
                default:
                    spots = cSpotDTO.sortType == "asc" ? spots.OrderBy(x => x.SpotId) : spots.OrderByDescending(x => x.SpotId);
                    break;
            }

            CSpotPagingDTO pagingDTO = new CSpotPagingDTO();
            pagingDTO.totalPages = totalPages;
            pagingDTO.totalCount = totalCount;
            pagingDTO.spotsResult = spots;
            return Json(pagingDTO);
        }
        [HttpPost]
        public IActionResult GetCategories()
        {
            var categories = _dbContext.Categories;
            return Json(categories);
        }
        //讀取圖片
        public IActionResult ReturnImage(int? id = 1)
        {
            Member? member = _dbContext.Members.Find(id);
            if (member == null)
                return NotFound();
            byte[]? img = member.FileData;
            return File(img, "image/jpeg");
        }
        //取得不重複縣市名稱
        public IActionResult GetCity()
        {
            var cities = _dbContext.Addresses.Select(x => x.City).Distinct();
            return Json(cities);
        }
        //依據選擇的縣市，取得不重複鄉鎮區
        public IActionResult GetDistinction(string city)
        {
            var distinctions = _dbContext.Addresses.Where(x => x.City.Equals(city)).Select(x => x.SiteId).Distinct();
            return Json(distinctions);
        }
        //依據選擇的縣市及鄉鎮區，取得路名
        public IActionResult GetRoad(string distinction)
        {
            var roads = _dbContext.Addresses.Where(x => x.SiteId.Equals(distinction)).Select(x => x.Road);
            return Json(roads);
        }

    }
}

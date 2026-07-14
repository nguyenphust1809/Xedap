using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using System.Linq;

namespace Xedap.Controllers
{
    public class StoresController : Controller
    {
        private readonly DataContext _dataContext;

        public StoresController(DataContext context)
        {
            _dataContext = context;
        }

        public IActionResult Index()
        {
            var stores = _dataContext.Stores.ToList();
            return View(stores);
        }

        [HttpPost]
        public IActionResult Create(Store store)
        {
            if (ModelState.IsValid)
            {
                // ✅ Tự động gán Location từ Lat & Lng
                if (store.Latitude != 0 && store.Longitude != 0)
                {
                    store.Location = new Point(store.Longitude, store.Latitude) { SRID = 4326 };
                }

                _dataContext.Stores.Add(store);
                _dataContext.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(store);
        }
    }
}

using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Area("Admin")]
[Route("Admin/Contact")]
[Authorize(Roles = "Admin")]
public class ContactController : Controller
{
    private readonly DataContext _dataContext;
    private readonly IWebHostEnvironment _webHostEnviroment;

    public ContactController(DataContext context, IWebHostEnvironment webHostEnviroment)
    {
        _dataContext = context;
        _webHostEnviroment = webHostEnviroment;
    }

    // GET /Admin/Contact/Index
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var contact = await _dataContext.Contact.ToListAsync();
        return View(contact);
    }

    // GET /Admin/Contact/Edit
    [HttpGet("Edit")]
    public async Task<IActionResult> Edit()
    {
        var contact = await _dataContext.Contact.FirstOrDefaultAsync();
        if (contact == null) return RedirectToAction(nameof(Index));
        return View(contact);
    }

    // POST /Admin/Contact/Edit
    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ContactModel contact)
    {
        var existed = await _dataContext.Contact.FirstOrDefaultAsync();
        if (existed == null) return RedirectToAction(nameof(Index));

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Upload ảnh nếu có
        if (contact.ImageUpload != null)
        {
            string uploadsDir = Path.Combine(_webHostEnviroment.WebRootPath, "media/logo");
            string imageName = Guid.NewGuid() + "_" + contact.ImageUpload.FileName;
            string filePath = Path.Combine(uploadsDir, imageName);
            await using var fs = new FileStream(filePath, FileMode.Create);
            await contact.ImageUpload.CopyToAsync(fs);
            existed.LogoImg = imageName;
        }

        existed.Name = contact.Name;
        existed.Email = contact.Email;
        existed.Phone = contact.Phone;
        existed.Description = contact.Description;
        existed.Map = contact.Map;

        await _dataContext.SaveChangesAsync();

        TempData["success"] = "Cập nhật thông tin thành công";
        return RedirectToAction(nameof(Index));
    }
}

using Xedap.Areas.Admin.Repository;
using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Xedap.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUserModel> _userManage;
        private readonly SignInManager<AppUserModel> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly DataContext _dataContext;
        public AccountController(UserManager<AppUserModel> userManage, SignInManager<AppUserModel> signInManager, DataContext context, IEmailSender emailSender)
        {
            _dataContext = context;
            _userManage = userManage;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        // -------------------- LOGIN --------------------
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }
        public async Task<IActionResult> UpdateAccount()
        {
            if ((bool)!User.Identity?.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var user = await _userManage.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if(user == null) {
                return NotFound();
            }

            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateInfoAccount(AppUserModel user)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);




            var userById = await _userManage.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userById == null)
            {
                return NotFound();
            }
            else
            {
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var passwordHash = passwordHasher.HashPassword(userById, user.PasswordHash);
                userById.PasswordHash = passwordHash;

                _dataContext.Update(userById);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật thông tin thành công";
            }
            return RedirectToAction("UpdateAccount", "Account");
        }


        public async Task<IActionResult> NewPass(AppUserModel user, string token)
        {
            var checkuser = await _userManage.Users
                .Where(u => u.Email == user.Email)
                .Where(u => u.Token == user.Token).FirstOrDefaultAsync();
            if (checkuser != null)
            {
                ViewBag.Email = checkuser.Email;
                ViewBag.Token = token;
            }
            else
            {
                TempData["error"] = "Email not found or token is not right";
                return RedirectToAction("ForgetPass", "Account");

            }
            return View();
        }
        public async Task<IActionResult> ForgetPass(string returnUrl)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            var result = await _signInManager.PasswordSignInAsync(
                loginVM.UserName,
                loginVM.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                return Redirect(loginVM.ReturnUrl ?? "/");
            }

            // Hiển thị lỗi khi đăng nhập thất bại
            ModelState.AddModelError(string.Empty, "Invalid username and password");
            return View(loginVM);
        }

        // -------------------- REGISTER --------------------
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        public async Task<IActionResult> History()
        {
            if ((bool)!User.Identity?.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var Orders = await _dataContext.Orders.Where(od => od.UserName == userEmail).OrderByDescending(od => od.Id).ToListAsync();
            ViewBag.UserEmail = userEmail;
            return View(Orders);
        }
        public async Task<IActionResult> CancelOrder(string ordercode)
        {
            if ((bool)!User.Identity?.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var order = await _dataContext.Orders.Where(o => o.OrderCode == ordercode).FirstAsync();
                order.Status = 6;
                _dataContext.Update(order);
                await _dataContext.SaveChangesAsync();

            }
            catch (Exception)
            {
                return BadRequest("An error occurred while canceling the order");
            }
            return RedirectToAction("History", "Account");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserModel user)
        {
            if (ModelState.IsValid)
            {
                AppUserModel newUser = new AppUserModel
                {
                    UserName = user.UserName,
                    Email = user.Email
                };

                IdentityResult result = await _userManage.CreateAsync(newUser, user.Password);

                if (result.Succeeded)
                {
                    TempData["success"] = "Tạo user thành công";
                    return RedirectToAction("Login", "Account");
                }

                // GÁN LỖI VÀO ĐÚNG FIELD
                foreach (IdentityError error in result.Errors)
                {
                    if (error.Code.Contains("DuplicateUserName"))
                    {
                        ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại");
                    }
                    else if (error.Code.Contains("DuplicateEmail"))
                    {
                        ModelState.AddModelError("Email", "Email đã tồn tại");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            return View(user);
        }


        // -------------------- LOGOUT --------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> SendMailForgotPass(AppUserModel user)
        {
            var checkMail = await _userManage.Users.FirstOrDefaultAsync(u => u.Email == user.Email);

            if (checkMail == null)
            {
                TempData["error"] = "Email not found";
                return RedirectToAction("ForgetPass", "Account");
            }
            else
            {
                // tạo token
                string token = Guid.NewGuid().ToString();

                // cập nhật token cho user
                checkMail.Token = token;
                _dataContext.Update(checkMail);
                await _dataContext.SaveChangesAsync();

                var receiver = checkMail.Email;
                var subject = "Change password for user " + checkMail.Email;

                var message =
                    "Click on link to change password: " +
                    "<a href=\"" +
                    $"{Request.Scheme}://{Request.Host}/Account/NewPass?email={checkMail.Email}&token={token}" +
                    "\">Click here</a>";

                await _emailSender.SendEmailAsync(receiver, subject, message);
            }

            TempData["success"] = "An email has been sent to your registered email address with password reset instructions.";
            return RedirectToAction("ForgetPass", "Account");
        }
      
        public async Task<IActionResult> UpdateNewPassword(AppUserModel user, string token)
        {
            // Kiểm tra người dùng theo email và token
            var checkUser = await _userManage.Users
                .Where(u => u.Email == user.Email)
                .Where(u => u.Token == token)
                .FirstOrDefaultAsync();

            if (checkUser != null)
            {
                // Tạo token mới để thay thế token cũ
                string newToken = Guid.NewGuid().ToString();

                // Hash mật khẩu mới
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var passwordHash = passwordHasher.HashPassword(checkUser, user.PasswordHash);

                // Cập nhật thông tin người dùng
                checkUser.PasswordHash = passwordHash;
                checkUser.Token = newToken;

                await _userManage.UpdateAsync(checkUser);

                TempData["success"] = "Password updated successfully.";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                TempData["error"] = "Email not found or token is not correct.";
                return RedirectToAction("ForgetPass", "Account");
            }
        }
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManage.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user); // Truyền AppUserModel tới view
        }
        [HttpGet]
        public async Task<IActionResult> OrderDetails(string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode))
                return NotFound();

            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Lấy thông tin đơn hàng (chỉ của user hiện tại)
            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserName == userEmail);

            if (order == null)
                return NotFound();

            // Lấy chi tiết đơn hàng
            var details = await _dataContext.OrdersDetails
                .Include(d => d.Product)
                .Where(d => d.OrderCode == orderCode && d.UserName == userEmail)
                .ToListAsync();

            ViewBag.Order = order;

            return View(details); // Truyền danh sách chi tiết đơn hàng
        }

    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EcommerceSystem.Models.ViewModels;
using EcommerceSystem.Models.Entities;
using EcommerceSystem.Data;

namespace EcommerceSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Mostrar credenciales de prueba solo en desarrollo
#if DEBUG
            ViewData["ShowTestCredentials"] = "true";
#endif

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Usuario {model.Email} inició sesión exitosamente.");

                    var roles = await _userManager.GetRolesAsync(user);
                    _logger.LogInformation($"Usuario {model.Email} tiene roles: {string.Join(", ", roles)}");

                    // Verificar roles y redirigir
                    if (roles.Contains("Admin") || roles.Contains("Vendedor"))
                    {
                        _logger.LogInformation($"Redirigiendo a Dashboard (Admin/Vendedor)");
                        TempData["Success"] = $"¡Bienvenido, {user.Email}!";
                        return RedirectToAction("Index", "Dashboard");
                    }

                    // Usuario cliente o sin rol específico
                    _logger.LogInformation($"Redirigiendo a Tienda (Cliente)");
                    TempData["Success"] = $"¡Bienvenido, {user.Email}!";

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Tienda");
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning($"Cuenta bloqueada: {model.Email}");
                    ModelState.AddModelError(string.Empty, "Cuenta bloqueada. Intenta más tarde.");
                    return View(model);
                }

                _logger.LogWarning($"Intento de login fallido para: {model.Email}");
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            }

            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor, complete todos los campos correctamente.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                // Verificar si el email ya existe
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    TempData["Error"] = "Este correo electrónico ya está registrado.";
                    return RedirectToAction(nameof(Login));
                }

                // Crear el usuario en AspNetUsers
                var user = new IdentityUser
                {
                    UserName = model.Email, // Usar email como username
                    Email = model.Email,
                    EmailConfirmed = true // Auto-confirmar email (puedes cambiarlo si quieres confirmación por email)
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Usuario {model.Email} creado exitosamente.");

                    // Asignar rol de Cliente por defecto
                    await _userManager.AddToRoleAsync(user, "Cliente");

                    // Crear registro en tabla Clientes
                    var cliente = new Cliente
                    {
                        NombreCompleto = model.NombreCompleto,
                        Email = model.Email,
                        Telefono = model.Telefono,
                        FechaRegistro = DateTime.Now,
                        UsuarioId = user.Id
                    };

                    _context.Clientes.Add(cliente);
                    await _context.SaveChangesAsync();

                    // Iniciar sesión automáticamente
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    TempData["Success"] = "¡Cuenta creada exitosamente! Bienvenido.";
                    return RedirectToAction("Index", "Tienda");
                }

                // Si hubo errores al crear el usuario
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning($"Error al crear usuario: {error.Description}");
                }

                TempData["Error"] = "No se pudo crear la cuenta. " + result.Errors.FirstOrDefault()?.Description;
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al registrar usuario: {ex.Message}");
                TempData["Error"] = "Ocurrió un error al crear la cuenta. Por favor, intente nuevamente.";
                return RedirectToAction(nameof(Login));
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Usuario cerró sesión.");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
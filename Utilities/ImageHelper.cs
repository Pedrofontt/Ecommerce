namespace EcommerceSystem.Utilities
{
    public static class ImageHelper
    {
        public static async Task<string> GuardarImagenAsync(
            IFormFile archivo,
            IWebHostEnvironment env,
            string carpeta = "productos")
        {
            if (archivo == null || archivo.Length == 0)
                return $"/img/{carpeta}/default.jpg";

            // Validar extensión
            string[] extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            string extension = Path.GetExtension(archivo.FileName).ToLower();

            if (!extensionesPermitidas.Contains(extension))
                return $"/img/{carpeta}/default.jpg";

            // Crear carpeta si no existe
            string folder = Path.Combine(env.WebRootPath, "img", carpeta);
            Directory.CreateDirectory(folder);

            // Generar nombre único
            string nombreArchivo = $"{Guid.NewGuid()}{extension}";
            string rutaCompleta = Path.Combine(folder, nombreArchivo);

            // Guardar archivo
            try
            {
                using var stream = new FileStream(rutaCompleta, FileMode.Create);
                await archivo.CopyToAsync(stream);
                return $"/img/{carpeta}/{nombreArchivo}";
            }
            catch
            {
                return $"/img/{carpeta}/default.jpg";
            }
        }

        public static void EliminarImagen(string? ruta, IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(ruta) || ruta.Contains("default"))
                return;

            string rutaCompleta = Path.Combine(env.WebRootPath, ruta.TrimStart('/'));

            if (File.Exists(rutaCompleta))
            {
                try
                {
                    File.Delete(rutaCompleta);
                }
                catch
                {
                    // Ignorar errores
                }
            }
        }
    }
}
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Condominio.Infrastructure.Services
{
    public class GoogleDriveService
    {
        private readonly DriveService _driveService;

        public GoogleDriveService()
        {
            // 1. Leemos el archivo .json que guardaste
            // Asegúrate de que la ruta sea la correcta en tu proyecto
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string credentialPath = Path.Combine(basePath, "Credentials", "Condominio.json");

            var credential = GoogleCredential.FromFile(credentialPath)
                .CreateScoped(DriveService.Scope.DriveReadonly);


            // 2. Inicializamos el servicio (esto se conecta en silencio)
            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "CondominioApp"
            });
        }


        public List<Google.Apis.Drive.v3.Data.File> ObtenerTodasLasCarpetas()
        {
            var request = _driveService.Files.List();

            request.Q = "mimeType = 'application/vnd.google-apps.folder' and trashed = false";

            // ✅ Agregamos "parents" a los campos para saber de quién depende cada carpeta
            request.Fields = "files(id, name, parents)";
            request.PageSize = 1000;

            var result = request.Execute();
            return result.Files.ToList();
        }




        // ✅ Obtener solo los ARCHIVOS de una carpeta
        public List<Google.Apis.Drive.v3.Data.File> ObtenerArchivos(string folderId)
        {
            var request = _driveService.Files.List();
            // Le decimos que busque cosas que NO sean carpetas (archivos) dentro de esa carpeta
            request.Q = $"'{folderId}' in parents and mimeType != 'application/vnd.google-apps.folder' and trashed = false";
            request.Fields = "files(id, name, mimeType)";
            request.PageSize = 100;

            var result = request.Execute();
            return result.Files.ToList();
        }

        // ✅ Descargar el archivo en un Stream
        public async Task<Stream> DescargarArchivo(string fileId)
        {
            var request = _driveService.Files.Get(fileId);
            var stream = new MemoryStream();
            await request.DownloadAsync(stream);
            stream.Position = 0;
            return stream;
        }

        public DriveService ObtenerServicio()
        {
            return _driveService;
        }
    }
}

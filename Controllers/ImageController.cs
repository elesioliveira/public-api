using System.Net;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using rmaesolutions.configInterface;
using rmaesolutions.entities;
using Serilog;

namespace rmaesolutions.Controllers;


[ApiController]
public class ImageController : ControllerBase
{

    private readonly MinioClient minioClient = new();
    private readonly string bucketName = "products";

    /// <summary>
    /// Salva uma imagem no bucket MinIO.
    /// </summary>
    /// <param name="obj">Objeto ImageDTO contendo a imagem codificada em base64.</param>
    /// <returns>O nome do objeto salvo no bucket.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/image
    ///     {
    ///       "b64": "iVBORw0KGgoAAAANSUhEUgAAAAUA..."
    ///     }
    ///
    /// Exemplo de retorno:
    ///
    ///     "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a.png"
    ///
    /// </remarks>
    /// <response code="200">Imagem salva com sucesso.</response>
    /// <response code="400">Se a imagem codificada em base64 não for fornecida.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/image")]
    public async Task<string> SaveImage(ImageDTO obj)
    {

        byte[] decodedBytes = Convert.FromBase64String(obj.B64!);
        string objectName = Guid.NewGuid().ToString() + "." + "png";

        minioClient.WithCredentials(EnvInterface.MinioAccess, EnvInterface.MinioSecret)
            .WithEndpoint(EnvInterface.MinioEndpoint)
            .WithSSL(false)
            .Build();

        try
        {
            PutObjectArgs args = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(new MemoryStream(decodedBytes))
                .WithObjectSize(decodedBytes.Length)
                .WithContentType($"image/png");

            if (obj.B64 != null)
            {
                try
                {
                    await minioClient.PutObjectAsync(args);

                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());

                }

                return objectName;

            }
            else
            {
                return "Image dont have b64";
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return e.ToString();
        }
    }

    /// <summary>
    /// Recupera uma imagem do bucket MinIO.
    /// </summary>
    /// <param name="name">Nome do objeto da imagem.</param>
    /// <returns>A imagem recuperada.</returns>
    /// <remarks>
    /// Exemplo de URL de solicitação:
    ///
    ///     GET /v1/image/a3f1c96d-75b4-4b6a-baf3-61b91c478a9a.png
    ///
    /// </remarks>
    /// <response code="200">Imagem recuperada com sucesso.</response>
    /// <response code="404">Se a imagem não for encontrada.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/image/{name}")]
    public async Task<IActionResult> GetImage(string name)
    {

        minioClient.WithCredentials(EnvInterface.MinioAccess, EnvInterface.MinioSecret)
            .WithEndpoint(EnvInterface.MinioEndpoint)
            .WithSSL(false)
            .Build();

        try
        {

            StatObjectArgs stats = new StatObjectArgs()
                                        .WithBucket(bucketName)
                                        .WithObject(name);

            ObjectStat statsImage = await minioClient.StatObjectAsync(stats);

            MemoryStream fileStream = new();

            GetObjectArgs args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(name)
                .WithCallbackStream((stream) =>
                {

                    stream.CopyTo(fileStream);
                    fileStream.Position = 0;
                });

            await minioClient.GetObjectAsync(args);

            return File(fileStream, statsImage.ContentType);

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }
}
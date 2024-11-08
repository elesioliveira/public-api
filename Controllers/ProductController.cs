using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using rmaesolutions.dto;
using System.Text;

namespace rmaesolutions.Controllers;

[ApiController]
public class ProductController : ControllerBase
{
    /// <summary>
    /// Retorna todos os produtos.
    /// </summary>
    /// <returns>Retorna uma lista de produtos</returns>
    /// <remarks>
    /// Exemplo de resposta:
    /// 
    ///     [
    ///       {
    ///         "uuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "categoryUuid": "4a95f64-5717-4562-b3fc-2c963f66afa6",
    ///         "subCategoryUuid": "5b95f64-5717-4562-b3fc-2c963f66afa6",
    ///         "barcode": "123456789012",
    ///         "partNumber": "PN-123456",
    ///         "skus": ["SKU123", "SKU124"],
    ///         "name": "Produto Exemplo",
    ///         "urlImage": "http://exemplo.com/imagem.png",
    ///         "height": 10.0,
    ///         "width": 5.0,
    ///         "length": 20.0,
    ///         "createdAt": "2024-06-27T11:32:57.64126",
    ///         "updatedAt": "2024-06-27T11:33:05.003075"
    ///       },
    ///       {
    ///         "uuid": "6fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "categoryUuid": "7a95f64-5717-4562-b3fc-2c963f66afa6",
    ///         "subCategoryUuid": "8b95f64-5717-4562-b3fc-2c963f66afa6",
    ///         "barcode": "987654321098",
    ///         "partNumber": "PN-654321",
    ///         "skus": ["SKU125", "SKU126"],
    ///         "name": "Outro Produto Exemplo",
    ///         "urlImage": "http://exemplo.com/imagem2.png",
    ///         "height": 15.0,
    ///         "width": 10.0,
    ///         "length": 25.0,
    ///         "createdAt": "2024-07-01T11:32:57.64126",
    ///         "updatedAt": "2024-07-01T11:33:05.003075"
    ///       }
    ///     ]
    /// </remarks>
    /// <response code="200">Retorna uma lista de produtos</response>
    /// <response code="204">Retorna que não há produtos</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/product/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();


            NpgsqlCommand command = new("SELECT * FROM products WHERE tenantuuid = @tenantuuid", connection);

            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));


            using NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                return NotFound(new { message = "Produtos não encontrado" });

            }

            List<Product> products = [];

            while (reader.Read())
            {
                products.Add(new Product
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    CategoryUuid = reader.IsDBNull(reader.GetOrdinal("categoryuuid")) ? null : reader.GetGuid(reader.GetOrdinal("categoryuuid")),
                    SubCategoryUuid = reader.IsDBNull(reader.GetOrdinal("subcategoryuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subcategoryuuid")),
                    Barcode = reader.IsDBNull(reader.GetOrdinal("barcode")) ? null : reader.GetString(reader.GetOrdinal("barcode")),
                    PartNumber = reader.IsDBNull(reader.GetOrdinal("partnumber")) ? null : reader.GetString(reader.GetOrdinal("partnumber")),
                    SKUs = reader.IsDBNull(reader.GetOrdinal("skus")) ? [] : reader.GetFieldValue<List<string>>(reader.GetOrdinal("skus")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    UrlImage = reader.IsDBNull(reader.GetOrdinal("urlimage")) ? null : reader.GetString(reader.GetOrdinal("urlimage")),
                    Height = reader.IsDBNull(reader.GetOrdinal("height")) ? null : reader.GetDouble(reader.GetOrdinal("height")),
                    Width = reader.IsDBNull(reader.GetOrdinal("width")) ? null : reader.GetDouble(reader.GetOrdinal("width")),
                    Length = reader.IsDBNull(reader.GetOrdinal("length")) ? null : reader.GetDouble(reader.GetOrdinal("length")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }


            return Ok(products);

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }



    /// <summary>
    /// Retorna um produto específico pelo UUID.
    /// </summary>
    /// <param name="uuid">UUID do produto</param>
    /// <returns>Retorna um produto</returns>
    /// <remarks>
    /// Exemplo de resposta:
    /// 
    ///     {
    ///       "uuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "categoryUuid": "4a95f64-5717-4562-b3fc-2c963f66afa6",
    ///       "subCategoryUuid": "5b95f64-5717-4562-b3fc-2c963f66afa6",
    ///       "barcode": "123456789012",
    ///       "partNumber": "PN-123456",
    ///       "skus": ["SKU123", "SKU124"],
    ///       "name": "Produto Exemplo",
    ///       "urlImage": "http://exemplo.com/imagem.png",
    ///       "height": 10.0,
    ///       "width": 5.0,
    ///       "length": 20.0,
    ///       "createdAt": "2024-06-27T11:32:57.64126",
    ///       "updatedAt": "2024-06-27T11:33:05.003075"
    ///     }
    /// </remarks>
    /// <response code="200">Retorna o produto</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/product/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM products WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            reader.Read();

            Product product = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                CategoryUuid = reader.IsDBNull(reader.GetOrdinal("categoryuuid")) ? null : reader.GetGuid(reader.GetOrdinal("categoryuuid")),
                SubCategoryUuid = reader.IsDBNull(reader.GetOrdinal("subcategoryuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subcategoryuuid")),
                Barcode = reader.IsDBNull(reader.GetOrdinal("barcode")) ? null : reader.GetString(reader.GetOrdinal("barcode")),
                PartNumber = reader.IsDBNull(reader.GetOrdinal("partnumber")) ? null : reader.GetString(reader.GetOrdinal("partnumber")),
                SKUs = reader.IsDBNull(reader.GetOrdinal("skus")) ? [] : reader.GetFieldValue<List<string>>(reader.GetOrdinal("skus")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                UrlImage = reader.IsDBNull(reader.GetOrdinal("urlimage")) ? null : reader.GetString(reader.GetOrdinal("urlimage")),
                Height = reader.IsDBNull(reader.GetOrdinal("height")) ? null : reader.GetDouble(reader.GetOrdinal("height")),
                Width = reader.IsDBNull(reader.GetOrdinal("width")) ? null : reader.GetDouble(reader.GetOrdinal("width")),
                Length = reader.IsDBNull(reader.GetOrdinal("length")) ? null : reader.GetDouble(reader.GetOrdinal("length")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
            };

            return Ok(product);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna um Produto pelo código.
    /// </summary>
    /// <param name="code">Código do produto (barcode ou SKU)</param>
    /// <param name="tenantuuid">Código uuid do tenant</param>
    /// <returns>Retorna um Produto</returns>
    /// <remarks>
    /// Exemplo de resposta:
    /// 
    ///     {
    ///         "uuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "tenantUuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "categoryUuid": "4a95f64-5717-4562-b3fc-2c963f66afa6",
    ///         "subCategoryUuid": "5b95f64-5717-4562-b3fc-2c963f66afa6",
    ///         "barcode": "123456789012",
    ///         "partNumber": "PN-123456",
    ///         "skus": ["SKU123", "SKU124"],
    ///         "name": "Produto Exemplo",
    ///         "urlImage": "http://exemplo.com/imagem.png",
    ///         "height": 10.0,
    ///         "width": 5.0,
    ///         "length": 20.0,
    ///         "createdAt": "2024-06-27T11:32:57.64126",
    ///         "updatedAt": "2024-06-27T11:33:05.003075"
    ///     }
    /// </remarks>
    /// <response code="200">Retorna um Produto</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/product/getbycode")]
    public IActionResult GetByCode(string code, string tenantuuid)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            NpgsqlCommand command;

            if (code.Length >= 12 && code.Length <= 13)
            {
                command = new("SELECT * FROM products WHERE barcode = @barcode AND tenantuuid= @tenantuuid", connection);
                command.Parameters.AddWithValue("barcode", code);
                command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
            }
            else
            {
                command = new("SELECT * FROM products WHERE @sku = ANY (SKUs) AND tenantuuid = @tenantuuid", connection);
                command.Parameters.AddWithValue("sku", code);
                command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
            }

            using NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                return NotFound(new { message = "Produto não encontrado" });


            }

            reader.Read();

            Product product = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                CategoryUuid = reader.IsDBNull(reader.GetOrdinal("categoryuuid")) ? null : reader.GetGuid(reader.GetOrdinal("categoryuuid")),
                SubCategoryUuid = reader.IsDBNull(reader.GetOrdinal("subcategoryuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subcategoryuuid")),
                Barcode = reader.IsDBNull(reader.GetOrdinal("barcode")) ? null : reader.GetString(reader.GetOrdinal("barcode")),
                PartNumber = reader.IsDBNull(reader.GetOrdinal("partnumber")) ? null : reader.GetString(reader.GetOrdinal("partnumber")),
                SKUs = reader.IsDBNull(reader.GetOrdinal("skus")) ? [] : reader.GetFieldValue<List<string>>(reader.GetOrdinal("skus")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                UrlImage = reader.IsDBNull(reader.GetOrdinal("urlimage")) ? null : reader.GetString(reader.GetOrdinal("urlimage")),
                Height = reader.IsDBNull(reader.GetOrdinal("height")) ? null : reader.GetDouble(reader.GetOrdinal("height")),
                Width = reader.IsDBNull(reader.GetOrdinal("width")) ? null : reader.GetDouble(reader.GetOrdinal("width")),
                Length = reader.IsDBNull(reader.GetOrdinal("length")) ? null : reader.GetDouble(reader.GetOrdinal("length")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
            };

            return Ok(product);

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Cria ou atualiza um produto.
    /// </summary>
    /// <param name="obj">Objeto ProductDTO contendo as informações do produto.</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada para criação:
    ///
    ///     POST /v1/product/create
    ///     {
    ///       "categoryUuid": "123e4567-e89b-12d3-a456-426614174000",
    ///       "subCategoryUuid": "234e5678-e89b-12d3-a456-426614174000",
    ///       "name": "Product Name",
    ///       "b64": "Base64ImageString",
    ///       "extension": ".jpg",
    ///       "partNumber": "PN12345",
    ///       "barcode": "1234567890123",
    ///       "width": 10.5,
    ///       "height": 5.0,
    ///       "length": 15.0
    ///     }
    ///
    /// Exemplo de entrada para atualização:
    ///
    ///     POST /v1/product/update
    ///     {
    ///       "uuid": "345e6789-e89b-12d3-a456-426614174000",
    ///       "categoryUuid": "123e4567-e89b-12d3-a456-426614174000",
    ///       "subCategoryUuid": "234e5678-e89b-12d3-a456-426614174000",
    ///       "name": "Updated Product Name",
    ///       "b64": "Base64ImageString",
    ///       "extension": ".jpg",
    ///       "partNumber": "PN12345",
    ///       "barcode": "1234567890123",
    ///       "width": 10.5,
    ///       "height": 5.0,
    ///       "length": 15.0
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Produto criado ou atualizado com sucesso.</response>
    /// <response code="400">Produto já cadastrado.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/product/create")]
    [Route("v1/product/update")]
    public async Task<IActionResult> Upsert([FromBody] ProductDTO obj, string tenantuuid)
    {
        try
        {

            string? imageurl = null;

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            await connection.OpenAsync();

            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM products WHERE name = @name AND tenantuuid= @tenantuuid", connection))
                {

                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("Produto já cadastrado");
                    }

                    reader.Close();
                }

                if (obj.B64 != null)
                {
                    ImageDTO image = new()
                    {
                        B64 = obj.B64,
                        Extension = obj.Extension
                    };

                    ImageController ImageController = new();

                    imageurl = await ImageController.SaveImage(image);
                }

                using (NpgsqlCommand cmd = new("INSERT INTO products (uuid, tenantuuid, categoryuuid, subcategoryuuid, name, urlimage, partnumber, barcode, width, height, length, createdat, updatedat) VALUES (@uuid, @tenantuuid, @categoryUuid, @subcategoryUuid, @name, @urlImage, @partnumber, @barcode, @width, @height, @length, @createdAt, @updatedAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("categoryUuid", obj.CategoryUuid == null ? DBNull.Value : obj.CategoryUuid!);
                    cmd.Parameters.AddWithValue("subcategoryUuid", obj.SubCategoryUuid == null ? DBNull.Value : obj.SubCategoryUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name!);
                    cmd.Parameters.AddWithValue("urlImage", obj.B64 == null ? DBNull.Value : imageurl!);
                    cmd.Parameters.AddWithValue("partnumber", obj.PartNumber == null ? DBNull.Value : obj.PartNumber!);
                    cmd.Parameters.AddWithValue("barcode", obj.Barcode == null ? DBNull.Value : obj.Barcode!);
                    cmd.Parameters.AddWithValue("width", obj.Width == null ? DBNull.Value : obj.Width!);
                    cmd.Parameters.AddWithValue("height", obj.Height == null ? DBNull.Value : obj.Height!);
                    cmd.Parameters.AddWithValue("length", obj.Length == null ? DBNull.Value : obj.Length!);
                    cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Produto cadastrado com sucesso");

            }
            else
            {

                // REFACTOR USING STRINGBUILDER

                StringBuilder command = new("UPDATE products SET");

                if (obj.Name != null)
                {
                    command.Append(" name = @name");
                }

                if (obj.CategoryUuid != null)
                {
                    if (obj.Name != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" categoryuuid = @categoryUuid");
                }

                if (obj.SubCategoryUuid != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" subcategoryuuid = @subcategoryUuid");
                }

                if (obj.B64 != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" urlimage = @urlImage");
                }

                if (obj.PartNumber != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.B64 != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" partnumber = @partnumber");
                }

                if (obj.Barcode != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.B64 != null || obj.PartNumber != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" barcode = @barcode");
                }

                if (obj.Width != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.B64 != null || obj.PartNumber != null || obj.Barcode != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" width = @width");
                }

                if (obj.Height != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.B64 != null || obj.PartNumber != null || obj.Barcode != null || obj.Width != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" height = @height");
                }

                if (obj.Length != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.B64 != null || obj.PartNumber != null || obj.Barcode != null || obj.Width != null || obj.Height != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" length = @length");
                }

                command.Append(", updatedat = @updatedAt");
                command.Append(" WHERE uuid = @uuid");

                using (NpgsqlCommand cmd = new(command.ToString(), connection))
                {

                    cmd.Parameters.AddWithValue("uuid", obj.Uuid);

                    if (obj.Name != null)
                    {
                        cmd.Parameters.AddWithValue("name", obj.Name);
                    }

                    if (obj.CategoryUuid != null)
                    {
                        cmd.Parameters.AddWithValue("categoryUuid", obj.CategoryUuid);
                    }

                    if (obj.SubCategoryUuid != null)
                    {
                        cmd.Parameters.AddWithValue("subcategoryUuid", obj.SubCategoryUuid);
                    }

                    if (obj.PartNumber != null)
                    {
                        cmd.Parameters.AddWithValue("partnumber", obj.PartNumber);
                    }

                    if (obj.B64 != null)
                    {
                        ImageDTO image = new()
                        {
                            B64 = obj.B64,
                            Extension = obj.Extension
                        };

                        ImageController ImageController = new();

                        imageurl = await ImageController.SaveImage(image);

                        cmd.Parameters.AddWithValue("urlImage", imageurl);
                    }

                    if (obj.Barcode != null)
                    {
                        cmd.Parameters.AddWithValue("barcode", obj.Barcode);
                    }

                    if (obj.Width != null)
                    {
                        cmd.Parameters.AddWithValue("width", obj.Width);
                    }

                    if (obj.Height != null)
                    {
                        cmd.Parameters.AddWithValue("height", obj.Height);
                    }

                    if (obj.Length != null)
                    {
                        cmd.Parameters.AddWithValue("length", obj.Length);
                    }

                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Produto atualizado com sucesso");
            }

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }
    /// <summary>
    /// Cria ou atualiza o relacionamento de variações de um produto.
    /// </summary>
    /// <param name="obj">Lista de objetos ProductsVariationRelationshipDTO contendo as informações do relacionamento.</param>
    /// <param name="productuuid">UUID do Produto</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/product/relationship/variation/create
    ///     [
    ///       {
    ///         "productUuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "variationUuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Relacionamento criado com sucesso.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/product/relationship/variation/create")]
    public IActionResult RelationshipVariationCreate([FromBody] List<ProductsVariationRelationshipDTO> obj, Guid productuuid)
    {
        bool exists = false;

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand cmd = new("SELECT * FROM productsvariationrelationship WHERE productuuid = @productUuid", connection))
            {

                cmd.Parameters.AddWithValue("productUuid", productuuid);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    exists = true;
                }

                reader.Close();
            }

            if (exists)
            {
                using NpgsqlCommand cmd = new("DELETE FROM productsvariationrelationship WHERE productuuid = @productUuid", connection);

                cmd.Parameters.AddWithValue("productUuid", productuuid);

                cmd.ExecuteNonQuery();
            }

            foreach (var item in obj)
            {
                using NpgsqlCommand cmd = new("INSERT INTO productsvariationrelationship (uuid, productuuid, variationuuid, createdat) VALUES (@uuid, @productUuid, @variationUuid, @createdAt)", connection);

                cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                cmd.Parameters.AddWithValue("productUuid", item.ProductUuid!);
                cmd.Parameters.AddWithValue("variationUuid", item.VariationUuid!);
                cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));

                cmd.ExecuteNonQuery();
            }

            return Ok("Relacionamento criado com sucesso");

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Retorna todas as variações associadas a um produto específico.
    /// </summary>
    /// <param name="productuuid">UUID do produto.</param>
    /// <returns>Retorna uma lista de variações associadas ao produto.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/product/relationship/variation/get?productuuid=123e4567-e89b-12d3-a456-426614174000
    ///     [
    ///       {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "name": "Color",
    ///         "variationKey": "color",
    ///         "variationValue": "Red",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       },
    ///       {
    ///         "uuid": "234e5678-e89b-12d3-a456-426614174000",
    ///         "name": "Size",
    ///         "variationKey": "size",
    ///         "variationValue": "Large",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de variações associadas ao produto.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/product/relationship/variation/get")]
    public IActionResult RelationshipVariationGet(Guid productuuid)
    {
        List<ProductsVariationRelationshipDTO> relationships = [];

        List<Variation> variations = [];

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand command = new("SELECT * FROM productsvariationrelationship WHERE productuuid = @productUuid", connection))
            {

                command.Parameters.AddWithValue("productUuid", productuuid);

                using NpgsqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        relationships.Add(new ProductsVariationRelationshipDTO
                        {
                            ProductUuid = reader.GetGuid(reader.GetOrdinal("productuuid")),
                            SubProductUuid = reader.IsDBNull(reader.GetOrdinal("subproductuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subproductuuid")),
                            VariationUuid = reader.GetGuid(reader.GetOrdinal("variationuuid"))
                        });
                    }

                }

                reader.Close();
            }

            if (relationships.Count > 0)
            {

                for (int i = 0; i < relationships.Count; i++)
                {
                    using (NpgsqlCommand command = new("SELECT * FROM variations WHERE uuid = @variationUuid", connection))
                    {

                        command.Parameters.AddWithValue("variationUuid", relationships[i].VariationUuid!);

                        using NpgsqlDataReader reader = command.ExecuteReader();

                        reader.Read();

                        variations.Add(new Variation
                        {
                            Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            VariationKey = reader.GetString(reader.GetOrdinal("variationkey")),
                            VariationValue = reader.GetString(reader.GetOrdinal("variationvalue")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                        });
                    }
                }
            }

            return Ok(variations);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Cria ou atualiza o relacionamento de classificações de um produto.
    /// </summary>
    /// <param name="obj">Lista de objetos ProductsClassificationRelationshipDTO contendo as informações do relacionamento.</param>
    /// <param name="productuuid">UUID do Produto</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/product/relationship/classification/create
    ///     [
    ///       {
    ///         "productUuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "classificationUuid": "4fa85f64-5717-4562-b3fc-2c963f66afa6"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Relacionamento criado com sucesso.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/product/relationship/classification/create")]
    public IActionResult RelationshipClassificationCreate([FromBody] List<ProductsClassificationRelationshipDTO> obj, Guid productuuid)
    {
        bool exists = false;

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand cmd = new("SELECT * FROM productsclassificationrelationship WHERE productuuid = @productUuid", connection))
            {

                cmd.Parameters.AddWithValue("productUuid", productuuid);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    exists = true;
                }

                reader.Close();
            }

            if (exists)
            {
                using (NpgsqlCommand cmd = new("DELETE FROM productsclassificationrelationship WHERE productuuid = @productUuid", connection))
                {

                    cmd.Parameters.AddWithValue("productUuid", productuuid);

                    cmd.ExecuteNonQuery();
                }
            }

            foreach (var item in obj)
            {
                using (NpgsqlCommand cmd = new("INSERT INTO productsclassificationrelationship (uuid, productuuid, classificationuuid, createdat) VALUES (@uuid, @productUuid, @classificationUuid, @createdAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("productUuid", item.ProductUuid!);
                    cmd.Parameters.AddWithValue("classificationUuid", item.ClassificationUuid!);
                    cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }
            }

            return Ok("Relacionamento criado com sucesso");

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Retorna todas as classificações associadas a um produto específico.
    /// </summary>
    /// <param name="productuuid">UUID do produto.</param>
    /// <returns>Retorna uma lista de classificações associadas ao produto.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/product/relationship/classification/get?productuuid=123e4567-e89b-12d3-a456-426614174000
    ///     [
    ///       {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "name": "Classificação 1",
    ///         "initials": "CL1",
    ///         "description": "Descrição da Classificação 1",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       },
    ///       {
    ///         "uuid": "234e5678-e89b-12d3-a456-426614174000",
    ///         "name": "Classificação 2",
    ///         "initials": "CL2",
    ///         "description": "Descrição da Classificação 2",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de classificações associadas ao produto.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/product/relationship/classification/get")]
    public IActionResult RelationshipClassificationGet(Guid productuuid)
    {
        List<ProductsClassificationRelationshipDTO> relationships = [];

        List<Classification> classifications = [];

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand command = new("SELECT * FROM productsclassificationrelationship WHERE productuuid = @productUuid", connection))
            {

                command.Parameters.AddWithValue("productUuid", productuuid);

                using NpgsqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        relationships.Add(new ProductsClassificationRelationshipDTO
                        {
                            ProductUuid = reader.GetGuid(reader.GetOrdinal("productuuid")),
                            SubProductUuid = reader.IsDBNull(reader.GetOrdinal("subproductuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subproductuuid")),
                            ClassificationUuid = reader.GetGuid(reader.GetOrdinal("classificationuuid"))
                        });
                    }

                }

                reader.Close();
            }

            if (relationships.Count > 0)
            {

                for (int i = 0; i < relationships.Count; i++)
                {
                    using (NpgsqlCommand command = new("SELECT * FROM classifications WHERE uuid = @classificationUuid", connection))
                    {

                        command.Parameters.AddWithValue("classificationUuid", relationships[i].ClassificationUuid!);

                        using NpgsqlDataReader reader = command.ExecuteReader();

                        reader.Read();

                        classifications.Add(new Classification
                        {
                            Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Initials = reader.GetString(reader.GetOrdinal("initials")),
                            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                        });
                    }
                }
            }

            return Ok(classifications);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Cria ou atualiza o relacionamento de subprodutos de um produto.
    /// </summary>
    /// <param name="obj">Lista de objetos ProductsSubProductsRelationshipDTO contendo as informações do relacionamento.</param>
    /// <param name="productuuid">UUID do Produto</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/product/relationship/subproduct/create
    ///     [
    ///       {
    ///         "productUuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "subProductUuid": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "subProductAmount": 5
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Relacionamento criado com sucesso.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/product/relationship/subproduct/create")]
    public IActionResult RelationshipProductCreate([FromBody] List<ProductsSubProductsRelationshipDTO> obj, Guid productuuid)
    {
        bool exists = false;

        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            using (NpgsqlCommand cmd = new("SELECT * FROM productssubproductsrelationship WHERE productuuid = @productUuid", connection))
            {
                cmd.Parameters.AddWithValue("@productUuid", productuuid);

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    exists = true;
                }
            }

            if (exists)
            {
                using (NpgsqlCommand cmd = new("DELETE FROM productssubproductsrelationship WHERE productuuid = @productUuid", connection))
                {
                    cmd.Parameters.AddWithValue("@productUuid", productuuid);
                    cmd.ExecuteNonQuery();
                }
            }

            foreach (var item in obj)
            {
                using NpgsqlCommand cmd = new("INSERT INTO productssubproductsrelationship (uuid, subproductuuid, productuuid, subproductamount, createdat) VALUES (@uuid, @subproductUuid, @productUuid, @subproductamount, @createdAt)", connection);

                cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                cmd.Parameters.AddWithValue("subproductuuid", item.SubProductUuid!);
                cmd.Parameters.AddWithValue("productuuid", productuuid);
                cmd.Parameters.AddWithValue("subproductamount", item.SubProductAmount!);
                cmd.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));

                cmd.ExecuteNonQuery();
            }

            return Ok("Relacionamento criado com sucesso");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }


    /// <summary>
    /// Retorna todos os subprodutos associados a um produto específico.
    /// </summary>
    /// <param name="productuuid">UUID do produto.</param>
    /// <returns>Retorna uma lista de subprodutos associados ao produto.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/product/relationship/subproduct/get?productuuid=123e4567-e89b-12d3-a456-426614174000
    ///     [
    ///       {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "name": "SubProduto 1",
    ///         "description": "Descrição do SubProduto 1",
    ///         "urlImage": "https://example.com/image1.png",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       },
    ///       {
    ///         "uuid": "234e5678-e89b-12d3-a456-426614174000",
    ///         "name": "SubProduto 2",
    ///         "description": "Descrição do SubProduto 2",
    ///         "urlImage": "https://example.com/image2.png",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de subprodutos associados ao produto.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/product/relationship/subproduct/get")]
    public IActionResult RelationshipSubProductGet(Guid productuuid)
    {
        List<ProductsSubProductsRelationshipDTO> relationships = new();
        List<SubProduct> subProducts = new();

        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            using (NpgsqlCommand command = new("SELECT * FROM productssubproductsrelationship WHERE productuuid = @productUuid", connection))
            {
                command.Parameters.AddWithValue("productUuid", productuuid);

                using NpgsqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        relationships.Add(new ProductsSubProductsRelationshipDTO
                        {
                            ProductUuid = reader.GetGuid(reader.GetOrdinal("productuuid")),
                            SubProductUuid = reader.GetGuid(reader.GetOrdinal("subproductuuid")),
                            SubProductAmount = reader.GetDouble(reader.GetOrdinal("subproductamount"))
                        });
                    }
                }
            }

            if (relationships.Count > 0)
            {
                for (int i = 0; i < relationships.Count; i++)
                {
                    using NpgsqlCommand command = new("SELECT * FROM subproducts WHERE uuid = @subproductUuid", connection);
                    command.Parameters.AddWithValue("subproductUuid", relationships[i].SubProductUuid!);

                    using NpgsqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        subProducts.Add(new SubProduct
                        {
                            Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                            UrlImage = reader.IsDBNull(reader.GetOrdinal("urlimage")) ? null : reader.GetString(reader.GetOrdinal("urlimage")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                        });
                    }
                }
            }

            return Ok(subProducts);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Atualiza os SKUs de um produto específico.
    /// </summary>
    /// <param name="skus">Lista de SKUs.</param>
    /// <param name="productuuid">UUID do Produto</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/product/skus/create
    ///     {
    ///       "skus": [
    ///         "SKU123",
    ///         "SKU456",
    ///         "SKU789"
    ///       ],
    ///       "productuuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">SKUs salvos com sucesso.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/product/skus/create")]
    public IActionResult ProductSkusCreate([FromBody] List<string> skus, Guid productuuid)
    {

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand cmd = new("UPDATE products SET skus = @skus WHERE uuid = @productUuid", connection))
            {

                cmd.Parameters.AddWithValue("productUuid", productuuid);
                cmd.Parameters.AddWithValue("skus", skus);

                cmd.ExecuteNonQuery();
            }

            return Ok("Skus salvo com sucesso");

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Retorna os SKUs de um produto específico.
    /// </summary>
    /// <param name="productuuid">UUID do Produto</param>
    /// <returns>Retorna uma lista de SKUs associados ao produto.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/product/skus/get?productuuid=3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     [
    ///       "SKU123",
    ///       "SKU456",
    ///       "SKU789"
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de SKUs associados ao produto.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>    

    [HttpGet]
    [Route("v1/product/skus/get")]
    public IActionResult ProductSkusGet(Guid productuuid)
    {

        List<string> Skus = [];

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand command = new("SELECT skus FROM products WHERE uuid = @productUuid", connection))
            {

                command.Parameters.AddWithValue("productUuid", productuuid);

                using NpgsqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Skus = reader.IsDBNull(reader.GetOrdinal("skus")) == true ? [] : reader.GetFieldValue<List<string>>(reader.GetOrdinal("skus"));
                    }

                }

                reader.Close();
            }

            return Ok(Skus);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }
}

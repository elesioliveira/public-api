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
public class SubProductController : ControllerBase
{
    /// <summary>
    /// Retorna todos os SubProdutos.
    /// </summary>
    /// <returns>Uma lista de todos os SubProdutos.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    [
    ///     {
    ///      "uuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///      "categoryUuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b",
    ///      "subCategoryUuid": "c3f8d87e-9f5e-5d7b-cgd4-63c82d589b0c",
    ///      "name": "SubProduto 1",
    ///      "urlImage": "https://example.com/image.png",
    ///      "description": "Descrição do SubProduto 1",
    ///      "height": 10.5,
    ///      "width": 20.5,
    ///      "length": 30.5,
    ///      "createdAt": "2024-01-01T12:00:00.000000",
    ///      "updatedAt": "2024-01-01T12:00:00.000000"
    ///     },
    ///     {
    ///      "uuid": "d4e8f97e-9f5f-5e7c-dhd5-74e93f67a1b2",
    ///      "categoryUuid": "e5f9g08f-9f6g-6f8d-eie6-85f04g78b2c3",
    ///      "subCategoryUuid": "f6g0h19h-9f7h-7g9e-fjf7-96g15h89c3d4",
    ///      "name": "SubProduto 2",
    ///      "urlImage": "https://example.com/image2.png",
    ///      "description": "Descrição do SubProduto 2",
    ///      "height": 12.5,
    ///      "width": 22.5,
    ///      "length": 32.5,
    ///      "createdAt": "2024-02-01T12:00:00.000000",
    ///      "updatedAt": "2024-02-01T12:00:00.000000"
    ///     }
    ///    ]
    ///
    /// </remarks>
    /// <response code="200">Retorna todos os SubProdutos cadastrados.</response>
    /// <response code="204">Se a tabela de SubProdutos estiver vazia.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/subproduct/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.OpenAsync();





            NpgsqlCommand command = new("SELECT * FROM subproducts WHERE tenantuuid = @tenantuuid", connection);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
            NpgsqlDataReader reader = command.ExecuteReader();
            List<SubProduct> subproducts = [];

            if (!reader.HasRows)
            {
                return NotFound("Nenhum subproduto cadastrado!");
            }

            while (reader.Read())
            {
                subproducts.Add(new SubProduct
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    CategoryUuid = reader.IsDBNull(reader.GetOrdinal("categoryuuid")) ? null : reader.GetGuid(reader.GetOrdinal("categoryuuid")),
                    SubCategoryUuid = reader.IsDBNull(reader.GetOrdinal("subcategoryuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subcategoryuuid")),
                    Name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString(reader.GetOrdinal("name")),
                    UrlImage = reader.IsDBNull(reader.GetOrdinal("urlimage")) ? null : reader.GetString(reader.GetOrdinal("urlimage")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    Height = reader.IsDBNull(reader.GetOrdinal("height")) ? null : reader.GetDouble(reader.GetOrdinal("height")),
                    Width = reader.IsDBNull(reader.GetOrdinal("width")) ? null : reader.GetDouble(reader.GetOrdinal("width")),
                    Length = reader.IsDBNull(reader.GetOrdinal("length")) ? null : reader.GetDouble(reader.GetOrdinal("length")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat")),

                });
            }

            return Ok(subproducts);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna um SubProduto específico.
    /// </summary>
    /// <param name="uuid">UUID do SubProduto</param>
    /// <returns>Retorna um SubProduto</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    {
    ///      "uuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///      "categoryUuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b",
    ///      "subCategoryUuid": "c3f8d87e-9f5e-5d7b-cgd4-63c82d589b0c",
    ///      "name": "SubProduto 1",
    ///      "urlImage": "https://example.com/image.png",
    ///      "description": "Descrição do SubProduto 1",
    ///      "height": 10.5,
    ///      "width": 20.5,
    ///      "length": 30.5,
    ///      "createdAt": "2024-01-01T12:00:00.000000",
    ///      "updatedAt": "2024-01-01T12:00:00.000000"
    ///    }
    ///
    /// </remarks>
    /// <response code="200">Retorna o SubProduto correspondente ao UUID fornecido</response>
    /// <response code="404">Se o SubProduto não for encontrado</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/subproduct/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM subproducts WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            reader.Read();

            SubProduct subProduct = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                CategoryUuid = reader.IsDBNull(reader.GetOrdinal("categoryuuid")) == true ? null : reader.GetGuid(reader.GetOrdinal("categoryuuid")),
                SubCategoryUuid = reader.IsDBNull(reader.GetOrdinal("subcategoryuuid")) == true ? null : reader.GetGuid(reader.GetOrdinal("subcategoryuuid")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                UrlImage = reader.IsDBNull(reader.GetOrdinal("urlimage")) == true ? null : reader.GetString(reader.GetOrdinal("urlimage")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) == true ? null : reader.GetString(reader.GetOrdinal("description")),
                Height = reader.IsDBNull(reader.GetOrdinal("height")) == true ? null : reader.GetDouble(reader.GetOrdinal("height")),
                Width = reader.IsDBNull(reader.GetOrdinal("width")) == true ? null : reader.GetDouble(reader.GetOrdinal("width")),
                Length = reader.IsDBNull(reader.GetOrdinal("length")) == true ? null : reader.GetDouble(reader.GetOrdinal("length")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))

            };

            return Ok(subProduct);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza um SubProduto.
    /// </summary>
    /// <param name="obj">Objeto SubProdutoDTO contendo as informações do SubProduto.</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada para criação:
    ///
    ///     POST /v1/subproduct/create
    ///     {
    ///       "categoryUuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b",
    ///       "subCategoryUuid": "c3f8d87e-9f5e-5d7b-cgd4-63c82d589b0c",
    ///       "name": "Novo SubProduto",
    ///       "urlImage": "https://example.com/image.png",
    ///       "description": "Descrição do Novo SubProduto",
    ///       "height": 10.5,
    ///       "width": 20.5,
    ///       "length": 30.5,
    ///       "b64": "iVBORw0KGgoAAAANSUhEUgAAAAUA...",
    ///       "extension": "png"
    ///     }
    ///
    /// Exemplo de entrada para atualização:
    ///
    ///     POST /v1/subproduct/update
    ///     {
    ///       "uuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///       "categoryUuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b",
    ///       "subCategoryUuid": "c3f8d87e-9f5e-5d7b-cgd4-63c82d589b0c",
    ///       "name": "SubProduto Atualizado",
    ///       "urlImage": "https://example.com/image.png",
    ///       "description": "Descrição do SubProduto Atualizado",
    ///       "height": 12.5,
    ///       "width": 22.5,
    ///       "length": 32.5,
    ///       "b64": "iVBORw0KGgoAAAANSUhEUgAAAAUA...",
    ///       "extension": "png"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma mensagem de sucesso.</response>
    /// <response code="400">Se o SubProduto já estiver cadastrado.</response>
    /// <response code="500">Retorna uma mensagem de erro.</response>

    [HttpPost]
    [Route("v1/subproduct/create")]
    [Route("v1/subproduct/update")]
    public async Task<IActionResult> Upsert([FromBody] SubProductDTO obj, string tenantuuid)
    {
        try
        {

            string? imageurl = null;

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM subproducts WHERE name = @name AND tenantuuid= @tenantuuid", connection))
                {

                    cmd.Parameters.AddWithValue("name", obj.Name!);
                    cmd.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("SubProduto já cadastrado");
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

                using (NpgsqlCommand cmd = new("INSERT INTO subproducts (uuid, tenantuuid, categoryuuid, subcategoryuuid, name, urlimage, description, width, height, length, createdat, updatedat) VALUES (@uuid,@tenantuuid, @categoryuuid, @subcategoryuuid, @name, @urlImage, @description, @width, @height, @length, @createdAt, @updatedAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("categoryUuid", obj.CategoryUuid == null ? DBNull.Value : obj.CategoryUuid!);
                    cmd.Parameters.AddWithValue("subcategoryUuid", obj.SubCategoryUuid == null ? DBNull.Value : obj.SubCategoryUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name!);
                    cmd.Parameters.AddWithValue("urlImage", obj.B64 == null ? DBNull.Value : imageurl!);
                    cmd.Parameters.AddWithValue("description", obj.Description == null ? DBNull.Value : obj.Description!);
                    cmd.Parameters.AddWithValue("width", obj.Width == null ? DBNull.Value : obj.Width!);
                    cmd.Parameters.AddWithValue("height", obj.Height == null ? DBNull.Value : obj.Height!);
                    cmd.Parameters.AddWithValue("length", obj.Length == null ? DBNull.Value : obj.Length!);
                    cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("SubProduto cadastrado com sucesso");

            }
            else
            {

                // REFACTOR USING STRINGBUILDER

                StringBuilder command = new("UPDATE subproducts SET");

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

                if (obj.Description != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" description = @description");
                }

                if (obj.B64 != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.Description != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" urlimage = @urlImage");
                }

                if (obj.Width != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.Description != null || obj.B64 != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" width = @width");
                }

                if (obj.Height != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.Description != null || obj.B64 != null || obj.Width != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" height = @height");
                }

                if (obj.Length != null)
                {
                    if (obj.Name != null || obj.CategoryUuid != null || obj.SubCategoryUuid != null || obj.Description != null || obj.B64 != null || obj.Width != null || obj.Height != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" length = @length");
                }

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

                    if (obj.Description != null)
                    {
                        cmd.Parameters.AddWithValue("description", obj.Description);
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

                return Ok("SubProduto atualizado com sucesso");
            }

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza o relacionamento de variações de um SubProduto.
    /// </summary>
    /// <param name="obj">Lista de objetos ProductsVariationRelationshipDTO contendo as informações do relacionamento.</param>
    /// <param name="subproductuuid">UUID do SubProduto</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/subproduct/relationship/variation/create
    ///     [
    ///       {
    ///         "subProductUuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///         "variationUuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b"
    ///       },
    ///       {
    ///         "subProductUuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///         "variationUuid": "c3f8d87e-9f5e-5d7b-cgd4-63c82d589b0c"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Relacionamento criado com sucesso.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/subproduct/relationship/variation/create")]
    public IActionResult RelationshipVariationCreate([FromBody] List<ProductsVariationRelationshipDTO> obj, Guid subproductuuid)
    {
        bool exists = false;

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand cmd = new("SELECT * FROM productsvariationrelationship WHERE subproductuuid = @subproductUuid", connection))
            {

                cmd.Parameters.AddWithValue("subproductUuid", subproductuuid);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    exists = true;
                }

                reader.Close();
            }

            if (exists)
            {
                using (NpgsqlCommand cmd = new("DELETE FROM productsvariationrelationship WHERE subproductuuid = @subproductUuid", connection))
                {

                    cmd.Parameters.AddWithValue("subproductUuid", subproductuuid);

                    cmd.ExecuteNonQuery();
                }
            }

            foreach (var item in obj)
            {
                using (NpgsqlCommand cmd = new("INSERT INTO productsvariationrelationship (uuid, subproductuuid, variationuuid, createdat) VALUES (@uuid, @subproductUuid, @variationUuid, @createdAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("subproductUuid", item.SubProductUuid!);
                    cmd.Parameters.AddWithValue("variationUuid", item.VariationUuid!);
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
    /// Retorna todas as variações relacionadas a um SubProduto específico.
    /// </summary>
    /// <param name="subproductuuid">UUID do SubProduto</param>
    /// <returns>Retorna uma lista de variações relacionadas.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/SUBproduct/relationship/variation/get?subproductuuid=a3f1c96d-75b4-4b6a-baf3-61b91c478a9a
    ///     [
    ///       {
    ///         "uuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b",
    ///         "name": "Variação 1",
    ///         "variationKey": "cor",
    ///         "variationValue": "azul",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       },
    ///       {
    ///         "uuid": "c3f8d87e-9f5e-5d7b-cgd4-63c82d589b0c",
    ///         "name": "Variação 2",
    ///         "variationKey": "tamanho",
    ///         "variationValue": "M",
    ///         "createdAt": "2024-01-02T12:00:00.000000",
    ///         "updatedAt": "2024-01-02T12:00:00.000000"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de variações relacionadas ao SubProduto.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/SUBproduct/relationship/variation/get")]
    public IActionResult RelationshipVariationGet(Guid subproductuuid)
    {
        List<ProductsVariationRelationshipDTO> relationships = [];

        List<Variation> variations = [];

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand command = new("SELECT * FROM productsvariationrelationship WHERE subproductuuid = @subproductUuid", connection))
            {

                command.Parameters.AddWithValue("subproductUuid", subproductuuid);

                using NpgsqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        relationships.Add(new ProductsVariationRelationshipDTO
                        {
                            ProductUuid = reader.IsDBNull(reader.GetOrdinal("productuuid")) ? null : reader.GetGuid(reader.GetOrdinal("productuuid")),
                            SubProductUuid = reader.GetGuid(reader.GetOrdinal("subproductuuid")),
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
    /// Cria ou atualiza o relacionamento de classificações de um SubProduto.
    /// </summary>
    /// <param name="obj">Lista de objetos ProductsClassificationRelationshipDTO contendo as informações do relacionamento.</param>
    /// <param name="subproductuuid">UUID do SubProduto</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/subproduct/relationship/classification/create
    ///     [
    ///       {
    ///         "subProductUuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "classificationUuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Relacionamento criado com sucesso.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/subproduct/relationship/classification/create")]
    public IActionResult RelationshipClassificationCreate([FromBody] List<ProductsClassificationRelationshipDTO> obj, Guid subproductuuid)
    {
        bool exists = false;

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand cmd = new("SELECT * FROM productsclassificationrelationship WHERE subproductuuid = @subproductUuid", connection))
            {

                cmd.Parameters.AddWithValue("subproductUuid", subproductuuid);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    exists = true;
                }

                reader.Close();
            }

            if (exists)
            {
                using (NpgsqlCommand cmd = new("DELETE FROM productsclassificationrelationship WHERE subproductuuid = @subproductUuid", connection))
                {

                    cmd.Parameters.AddWithValue("subproductUuid", subproductuuid);

                    cmd.ExecuteNonQuery();
                }
            }

            foreach (var item in obj)
            {
                using (NpgsqlCommand cmd = new("INSERT INTO productsclassificationrelationship (uuid, subproductuuid, classificationuuid, createdat) VALUES (@uuid, @subproductUuid, @classificationUuid, @createdAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("subproductUuid", item.SubProductUuid!);
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
    /// Retorna todas as classificações relacionadas a um SubProduto específico.
    /// </summary>
    /// <param name="subproductuuid">UUID do SubProduto</param>
    /// <returns>Retorna uma lista de classificações relacionadas.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/subproduct/relationship/classification/get?subproductuuid=a3f1c96d-75b4-4b6a-baf3-61b91c478a9a
    ///     [
    ///       {
    ///         "uuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b",
    ///         "name": "Classificação 1",
    ///         "initials": "C1",
    ///         "description": "Descrição da Classificação 1",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       },
    ///       {
    ///         "uuid": "c3f8d87e-9f5e-5d7b-cgd4-63c82d589b0c",
    ///         "name": "Classificação 2",
    ///         "initials": "C2",
    ///         "description": "Descrição da Classificação 2",
    ///         "createdAt": "2024-01-02T12:00:00.000000",
    ///         "updatedAt": "2024-01-02T12:00:00.000000"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de classificações relacionadas ao SubProduto.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/subproduct/relationship/classification/get")]
    public IActionResult RelationshipClassificationGet(Guid subproductuuid)
    {
        List<ProductsClassificationRelationshipDTO> relationships = [];

        List<Classification> classifications = [];

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand command = new("SELECT * FROM productsclassificationrelationship WHERE subproductuuid = @subproductUuid", connection))
            {

                command.Parameters.AddWithValue("subproductUuid", subproductuuid);

                using NpgsqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        relationships.Add(new ProductsClassificationRelationshipDTO
                        {
                            ProductUuid = reader.IsDBNull(reader.GetOrdinal("productuuid")) ? null : reader.GetGuid(1),
                            SubProductUuid = reader.GetGuid(reader.GetOrdinal("subproductuuid")),
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
}

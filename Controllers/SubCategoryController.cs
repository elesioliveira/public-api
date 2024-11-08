using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using rmaesolutions.dto;

namespace rmaesolutions.Controllers;

[ApiController]
public class SubCategoryController : ControllerBase
{
    /// <summary>
    /// Retorna todas as SubCategorias.
    /// </summary>
    /// <returns>Retorna uma lista de SubCategorias</returns>
    /// <response code="200">Retorna uma lista de SubCategorias</response>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "categoryUuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "name": "SubCategoria 1",
    ///         "createdAt": "2023-06-01T12:00:00",
    ///         "updatedAt": "2023-06-01T12:00:00"
    ///     },
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "categoryUuid": "123e4567-e89b-12d3-a456-426614174003",
    ///         "name": "SubCategoria 2",
    ///         "createdAt": "2023-06-02T12:00:00",
    ///         "updatedAt": "2023-06-02T12:00:00"
    ///     }
    ///
    /// </remarks>
    /// <response code="204">Retorna 204 se a tabela estiver vazia</response>
    /// <response code="500">Retorna uma mensagem de erro</response>


    [HttpGet]
    [Route("v1/subcategory/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM subcategories WHERE tenantuuid = @tenantuuid", connection);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            NpgsqlDataReader reader = command.ExecuteReader();
            if (!reader.HasRows)
            {
                return NotFound("Nenhuma subCategoria encontrada");
            }

            List<SubCategory> subCategories = new();

            while (reader.Read())
            {
                subCategories.Add(new SubCategory
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                    CategoryUuid = reader.GetGuid(reader.GetOrdinal("categoryuuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }

            return Ok(subCategories);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }


    /// <summary>
    /// Retorna uma SubCategoria baseada no UUID da Categoria.
    /// </summary>
    /// <param name="categoryuuid">UUID da Categoria</param>
    /// <returns>Retorna uma lista de SubCategorias</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "categoryUuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "name": "SubCategoria 1",
    ///         "createdAt": "2023-06-01T12:00:00",
    ///         "updatedAt": "2023-06-01T12:00:00"
    ///     },
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "categoryUuid": "123e4567-e89b-12d3-a456-426614174003",
    ///         "name": "SubCategoria 2",
    ///         "createdAt": "2023-06-02T12:00:00",
    ///         "updatedAt": "2023-06-02T12:00:00"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de SubCategorias</response>
    /// <response code="500">Retorna uma mensagem de erro</response>


    [HttpGet]
    [Route("v1/subcategory/relation/category/get")]
    public IActionResult GetFromCategory(Guid categoryuuid)
    {
        try
        {

            List<SubCategory> subCategories = [];

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM subcategories WHERE categoryuuid = @categoryuuid", connection);

            command.Parameters.AddWithValue("categoryuuid", categoryuuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                subCategories.Add(new SubCategory
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    CategoryUuid = reader.GetGuid(reader.GetOrdinal("categoryuuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }

            return Ok(subCategories);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna uma SubCategoria.
    /// </summary>
    /// <param name="uuid">UUID da SubCategoria</param>
    /// <returns>Retorna uma SubCategoria</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "categoryUuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "name": "SubCategoria Exemplo",
    ///         "createdAt": "2023-06-01T12:00:00",
    ///         "updatedAt": "2023-06-01T12:00:00"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma SubCategoria</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/subcategory/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM subcategories WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            reader.Read();

            SubCategory subcategory = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                CategoryUuid = reader.GetGuid(reader.GetOrdinal("categoryuuid")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
            };

            return Ok(subcategory);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza uma SubCategoria.
    /// </summary>
    /// <param name="obj">Objeto SubCategoria</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    /// <response code="200">Retorna uma mensagem de sucesso</response>
    /// <response code="500">Retorna uma mensagem de erro</response>
    /// <remarks>
    /// Exemplo de requisição:
    ///
    ///     POST /v1/subcategory/create
    ///     {
    ///         "categoryUuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "name": "SubCategoria Exemplo"
    ///     }
    ///
    /// Exemplo de resposta de sucesso:
    ///
    ///     {
    ///         "message": "SubCategoria cadastrada com sucesso"
    ///     }
    ///
    /// </remarks>

    [HttpPost]
    [Route("v1/subcategory/create")]
    [Route("v1/subcategory/update")]
    public IActionResult Upsert([FromBody] SubCategoryDTO obj)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.OpenAsync();

            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM subcategories WHERE name = @name AND categoryuuid = @categoryuuid", connection))
                {

                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("categoryuuid", obj.CategoryUuid!);

                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("SubCategoria já cadastrado");
                    }

                    reader.Close();
                }

                using (NpgsqlCommand cmd = new("INSERT INTO subcategories (uuid,tenantuuid, categoryuuid, name, createdat, updatedat) VALUES (@uuid,@tenantuuid, @categoryuuid, @name, @createdat, @updatedat)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantUuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("categoryUuid", obj.CategoryUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("SubCategoria cadastrada com sucesso");

            }
            else
            {

                string command = "UPDATE subcategories SET";

                if (obj.Name != null)
                {
                    command += " name = @name";
                }

                if (obj.CategoryUuid != null)
                {
                    if (obj.Name != null)
                    {
                        command += ",";
                    }

                    command += "categoryuuid = @categoryUuid";
                }

                command += ", updatedat = @updatedat";
                command += " WHERE uuid = @uuid";

                using (NpgsqlCommand cmd = new(command, connection))
                {

                    cmd.Parameters.AddWithValue("uuid", obj.Uuid);
                    if (obj.Name != null)
                    {
                        cmd.Parameters.AddWithValue("name", obj.Name!);
                    }
                    if (obj.CategoryUuid != null)
                    {
                        cmd.Parameters.AddWithValue("categoryUuid", obj.CategoryUuid!);
                    }

                    cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("SubCategoria atualizada com sucesso");
            }



        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }
}

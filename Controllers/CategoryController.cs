using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using rmaesolutions.dto;

namespace rmaesolutions.Controllers;

[ApiController]
public class CategoryController : ControllerBase
{
    /// <summary>
    /// Retorna todas as Categorias.
    /// </summary>
    /// <returns>Uma lista de todas as categorias.</returns>
    /// <param name="tenantuuid">Uuid do Cliente</param>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    [
    ///     {
    ///      "uuid": "388b0acb-d1c5-48b0-b1e8-f6a655591ea3",
    ///      "name": "CategoryName1",
    ///      "createdAt": "2024-01-01T12:00:00.000000",
    ///      "updatedAt": "2024-01-01T12:00:00.000000"
    ///     },
    ///     {
    ///      "uuid": "2fb54ab6-7182-48ba-ab43-5bf6a34787c2",
    ///      "name": "CategoryName2",
    ///      "createdAt": "2024-01-01T12:00:00.000000",
    ///      "updatedAt": "2024-01-01T12:00:00.000000"
    ///     },
    ///     {
    ///      "uuid": "653c38da-c964-4d5c-b7c0-efd4c2ed678c",
    ///      "name": "CategoryName3",
    ///      "createdAt": "2024-01-01T12:00:00.000000",
    ///      "updatedAt": "2024-01-01T12:00:00.000000"
    ///     }
    ///    ]
    ///
    /// </remarks>
    /// <response code="200">Retorna todas as Categorias cadastradas.</response>
    /// <response code="204">Não há categorias cadastradas.</response>
    /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/category/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM categories WHERE tenantuuid = @tenantuuid", connection);

            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            NpgsqlDataReader reader = command.ExecuteReader();

            List<Category> categories = [];

            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }

            if (categories.Count == 0)
            {
                return NoContent();
            }

            return Ok(categories);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna uma Categoria a partir de um UUID.
    /// </summary>
    /// <param name="uuid">UUID da Categoria</param>
    /// <returns>Um objeto <see cref="Category"/> que corresponde ao UUID fornecido.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    {
    ///        "uuid": "653c38da-c964-4d5c-b7c0-efd4c2ed678c",
    ///        "name": "CategoryName",
    ///        "createdAt": "2024-01-01T12:00:00.000000",
    ///        "updatedAt": "2024-01-01T12:00:00.000000"
    ///    }
    ///
    /// </remarks>
    /// <response code="200">Retorna o objeto Categoria correspondente ao UUID.</response>
    /// <response code="404">Se a Categoria com o UUID fornecido não for encontrada.</response>
    /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/category/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM categories WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                return NotFound("Categoria não encontrada.");
            }

            reader.Read();

            Category category = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
            };

            return Ok(category);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza uma Categoria.
    /// </summary>
    /// <param name="obj">Objeto Categoria</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada para criação:
    ///
    ///    {
    ///        "uuid": null,
    ///        "name": "CategoryName"
    ///    }
    ///
    /// Exemplo de entrada para atualização:
    ///
    ///    {
    ///        "uuid": "653c38da-c964-4d5c-b7c0-efd4c2ed678c",
    ///        "name": "NewCategoryName"
    ///    }
    ///
    /// </remarks>
    /// <response code="200">Categoria criada ou atualizada com sucesso.</response>
    /// <response code="400">Erro na requisição. Verifique os dados fornecidos.</response>
    /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/category/create")]
    [Route("v1/category/update")]
    public IActionResult Upsert([FromBody] CategoryDTO obj, string tenantuuid)
    {
        try
        {
            if (string.IsNullOrEmpty(obj.Name))
            {
                return BadRequest("O nome da categoria não pode ser nulo ou vazio");
            }

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM categories WHERE name = @name AND tenantuuid= @tenantuuid", connection))
                {

                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("Categoria já cadastrada");
                    }

                    reader.Close();
                }

                using (NpgsqlCommand cmd = new("INSERT INTO categories (uuid, tenantuuid, name, createdat, updatedat) VALUES (@uuid, @tenantuuid, @name, @createdat, @updatedat)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Categoria cadastrada com sucesso");

            }
            else
            {

                using (NpgsqlCommand cmd = new("UPDATE categories SET name = @name , updatedat = @updatedat WHERE uuid = @uuid", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", obj.Uuid);
                    cmd.Parameters.AddWithValue("name", obj.Name!);
                    cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Categoria atualizada com sucesso");
            }

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }
}

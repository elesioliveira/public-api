using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using System.Text;
using rmaesolutions.dto;

namespace rmaesolutions.Controllers;

[ApiController]
public class ClassificationController : ControllerBase
{
    /// <summary>
    /// Retorna todas as Classificações.
    /// </summary>
    /// <returns>Uma lista de todas as Classificações</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    [
    ///     {
    ///      "uuid": "62e37d39-5b56-457d-ab79-09ec1c490dd1",
    ///      "name": "Cassificação1",
    ///      "createdAt": "2024-06-25T15:14:58.729747",
    ///      "updatedAt": "2024-06-25T15:14:58.72975"
    ///     },
    ///     {
    ///      "uuid": "22f736b2-876c-4c68-953c-bde97ee4e0a3",
    ///      "name": "Cassificação2",
    ///      "createdAt": "2024-06-25T15:19:34.47457",
    ///      "updatedAt": "2024-06-25T15:19:34.474573"
    ///     },
    ///     {
    ///      "uuid": "653c38da-c964-4d5c-b7c0-efd4c2ed678c",
    ///      "name": "Cassificação3",
    ///      "createdAt": "2024-06-15T17:25:37.994135",
    ///      "updatedAt": "2024-06-25T15:23:44.964655"
    ///     }
    ///    ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de todas as Classificações</response>
    /// <response code="204">Se a tabela Classificação estiver vazia.</response>
    /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/classification/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM classifications WHERE tenantuuid = @tenantuuid", connection);

            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            NpgsqlDataReader reader = command.ExecuteReader();

            List<Classification> classifications = [];

            while (reader.Read())
            {
                classifications.Add(new Classification
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Initials = reader.GetString(reader.GetOrdinal("initials")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }

            if (classifications.Count == 0)
            {
                return NoContent();
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
    /// Retorna uma Classification.
    /// </summary>
    /// <param name="uuid">UUID da Classification</param>
    /// <returns>Retorna uma Classification.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    {
    ///        "uuid": "539351dc-751f-4931-861b-6aa48d5fe3e9",
    ///        "name": "ClassificationName",
    ///        "initials": "Ini",
    ///        "description": "descrição",
    ///        "createdAt": "2024-06-23T12:11:49.746366",
    ///        "updatedAt": "2024-06-23T12:11:49.746374"
    ///    }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma Classification.</response>
    /// <response code="404">Se a Classification não for encontrada.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/classification/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM classifications WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return NotFound("Classification não encontrada.");
            }

            Classification classification = new()
            {
                Uuid = reader.GetGuid(0),
                Name = reader.GetString(1),
                Initials = reader.GetString(2),
                Description = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5)
            };

            return Ok(classification);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza uma Classification.
    /// </summary>
    /// <param name="obj">Objeto Classification</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    /// <remarks>
    /// Exemplo create input:
    ///
    ///    {
    ///    "name": "ClassificationName"
    ///    }
    ///
    /// Exemplo update input:
    ///
    ///    {
    ///    "uuid": "653c38da-c964-4d5c-b7c0-efd4c2ed678c",
    ///    "name": "NewCClassificationName"
    ///    }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma mensagem de sucesso</response>
    /// <response code="400">Erro na requisição. Verifique os dados fornecidos.</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpPost]
    [Route("v1/classification/create")]
    [Route("v1/classification/update")]
    public IActionResult Upsert([FromBody] ClassificationDTO obj, string tenantuuid)
    {
        try
        {
            if (obj.Name == null)
            {
                return BadRequest("O nome da classificação não pode ser nulo.");
            }

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM classifications WHERE name = @name AND tenantuuid= @tenantuuid", connection))
                {
                    cmd.Parameters.AddWithValue("name", obj.Name.ToLower());
                    cmd.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("Classificação já cadastrado");
                    }

                    reader.Close();
                }

                using (NpgsqlCommand cmd = new("INSERT INTO classifications (uuid, tenantuuid, name, initials, description, createdat, updatedat) VALUES (@uuid, @tenantuuid, @name, @initials, @description, @createdAt, @updatedAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name);
                    cmd.Parameters.AddWithValue("initials", obj.Initials!);
                    cmd.Parameters.AddWithValue("description", obj.Description!);
                    cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Classificação cadastrado com sucesso");

            }
            else
            {

                using (NpgsqlCommand checkCmd = new("SELECT 1 FROM classifications WHERE uuid = @uuid", connection))
                {
                    checkCmd.Parameters.AddWithValue("uuid", obj.Uuid);

                    using NpgsqlDataReader reader = checkCmd.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        return NotFound("Classificação não encontrada.");
                    }

                    reader.Close();
                }

                // REFACTOR USING STRINGBUILDER

                StringBuilder command = new("UPDATE classifications SET");

                if (obj.Name != null)
                {
                    command.Append(" name = @name");
                }

                if (obj.Initials != null)
                {
                    if (obj.Name != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" initials = @initials");
                }

                if (obj.Description != null)
                {
                    if (obj.Name != null || obj.Initials != null)
                    {
                        command.Append(',');
                    }
                    command.Append(" description = @description");
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

                    if (obj.Initials != null)
                    {
                        cmd.Parameters.AddWithValue("initials", obj.Initials);
                    }

                    if (obj.Description != null)
                    {
                        cmd.Parameters.AddWithValue("description", obj.Description);
                    }

                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Classificação atualizado com sucesso");
            }

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }
}
